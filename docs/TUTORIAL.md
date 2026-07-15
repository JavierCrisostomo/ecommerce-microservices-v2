# Tutorial: construyendo una plataforma de e-commerce con microservicios en .NET

Esta guía recorre paso a paso cómo se construyó este repositorio: una plataforma de
e-commerce armada como microservicios independientes, cada uno con Clean Architecture y
CQRS por dentro, persistiendo en SQL Server y coordinados por una saga de checkout sobre
RabbitMQ. El objetivo no es solo mostrar código que funciona, sino explicar el porqué de
cada decisión — por qué 7 servicios y no 3, por qué un read model separado en algunos
casos y no en otros, y qué problemas reales aparecieron en el camino (y cómo se
resolvieron).

## Requisitos previos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker](https://www.docker.com/) (para SQL Server, RabbitMQ, y los tests de integración)
- Un cliente HTTP para probar endpoints (`curl`, Postman, o el archivo `.http` que trae
  cada proyecto)

## Índice

1. [Arquitectura general](#1-arquitectura-general)
2. [Base de la solución y el paquete de contratos compartidos](#2-base-de-la-solución-y-el-paquete-de-contratos-compartidos)
3. [Identity Service: la plantilla de Clean Architecture](#3-identity-service-la-plantilla-de-clean-architecture)
4. [Catalog y Orders: CQRS con read model separado](#4-catalog-y-orders-cqrs-con-read-model-separado)
5. [Inventory, Payments y la saga de checkout](#5-inventory-payments-y-la-saga-de-checkout)
6. [Notifications y el API Gateway](#6-notifications-y-el-api-gateway)
7. [Dockerizando todo el stack](#7-dockerizando-todo-el-stack)
8. [Pruebas unitarias](#8-pruebas-unitarias)
9. [Pruebas de integración con Testcontainers](#9-pruebas-de-integración-con-testcontainers)
10. [Outbox pattern: escritura y publicación atómicas](#10-outbox-pattern-escritura-y-publicación-atómicas)
11. [Resiliencia: retry y circuit breaker](#11-resiliencia-retry-y-circuit-breaker)
12. [Observabilidad: OpenTelemetry y Jaeger](#12-observabilidad-opentelemetry-y-jaeger)
13. [Cómo seguir desde acá](#13-cómo-seguir-desde-acá)

---

## 1. Arquitectura general

Antes de escribir código, conviene fijar el diseño en un documento: qué servicios existen,
qué base de datos tiene cada uno, cómo se comunican entre sí, y qué patrón de capas
comparten. Las decisiones de este proyecto:

- **7 servicios**, cada uno dueño exclusivo de su base de datos (nadie más la consulta
  directamente): Identity, Catalog, Orders, Inventory, Payments, Notifications y un API
  Gateway.
- **Comunicación síncrona** (REST vía Gateway) para lo que el cliente espera en la misma
  respuesta; **asíncrona** (eventos sobre RabbitMQ) para todo lo que se resuelve después.
- **CQRS** dentro de cada servicio: comandos y queries por caminos separados, con un read
  model realmente distinto (tabla propia) en los dos servicios de mayor carga de lectura
  (Catalog y Orders).
- **Saga coreografiada** para el checkout: sin un orquestador central, cada servicio
  reacciona al evento anterior.

El orden de construcción importa: Identity primero, porque todo lo demás depende de tener
un token que validar. Después Catalog y Orders (el primer par con CQRS completo). Recién
ahí Inventory, Payments y la mensajería, porque la saga necesita que Orders ya sepa crear
pedidos. Notifications y el Gateway cierran el flujo. Docker, tests unitarios y tests de
integración quedan al final, sobre una base ya funcionando.

## 2. Base de la solución y el paquete de contratos compartidos

Se arranca con una solución vacía y una carpeta para el único código que **sí** se
comparte entre servicios: los eventos de integración.

```bash
mkdir ECommerce.Microservices && cd ECommerce.Microservices
git init
dotnet new sln -n ECommerce.Microservices --format sln

mkdir -p src/shared
cd src/shared
dotnet new classlib -n ECommerce.Contracts -f net8.0
cd ../..
dotnet sln add src/shared/ECommerce.Contracts/ECommerce.Contracts.csproj
```

`ECommerce.Contracts` solo contiene records de eventos, sin ninguna lógica:

```csharp
// src/shared/ECommerce.Contracts/IntegrationEvents/OrderEvents.cs
public record OrderLine(Guid ProductId, int Quantity, decimal UnitPrice);

public record OrderCreated(
    Guid EventId,
    DateTimeOffset OccurredOn,
    Guid OrderId,
    Guid CustomerId,
    IReadOnlyCollection<OrderLine> Lines,
    decimal TotalAmount) : IIntegrationEvent;
```

Es deliberado que sea lo único compartido: cualquier otra cosa (interfaces de Application,
helpers de Infrastructure) se duplica servicio por servicio en vez de vivir en un paquete
común. Compartir código de capas internas entre microservicios recrea el acoplamiento que
los microservicios están pensados para evitar — la única excepción razonable son los
contratos de los eventos, porque *alguien* tiene que definir su forma para que emisor y
consumidor se entiendan.

## 3. Identity Service: la plantilla de Clean Architecture

Identity es el primer servicio y el que define la plantilla de 4 proyectos que se repite
en Catalog, Orders, Inventory, Payments y Notifications:

```bash
cd src/services
mkdir Identity && cd Identity

dotnet new classlib -n Identity.Domain -f net8.0
dotnet new classlib -n Identity.Application -f net8.0
dotnet new classlib -n Identity.Infrastructure -f net8.0
dotnet new webapi -n Identity.Api -f net8.0 --use-controllers false
```

Las referencias entre proyectos siguen la regla de dependencia de Clean Architecture: las
flechas apuntan hacia el Domain.

```bash
dotnet add Identity.Application reference Identity.Domain
dotnet add Identity.Infrastructure reference Identity.Application
dotnet add Identity.Api reference Identity.Application
dotnet add Identity.Api reference Identity.Infrastructure
```

**Domain** no tiene ninguna dependencia de infraestructura. Una entidad se crea solo a
través de un factory method que protege sus invariantes — nunca con un constructor público
ni con setters libres:

```csharp
public class User
{
    public Guid Id { get; private set; }
    public string Email { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
    public string Role { get; private set; } = "Customer";

    private User() { }

    public static User Create(string email, string passwordHash, string role = "Customer")
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("El email es obligatorio.", nameof(email));

        return new User
        {
            Id = Guid.NewGuid(),
            Email = email.Trim().ToLowerInvariant(),
            PasswordHash = passwordHash,
            Role = role,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}
```

**Application** define comandos con MediatR y sus validadores con FluentValidation. El
patrón se repite para cada comando: un record que implementa `IRequest<TResult>`, un
validador, y un handler que orquesta el repositorio y el `IUnitOfWork`:

```csharp
public record RegisterUserCommand(string Email, string Password) : IRequest<RegisterUserResult>;

public class RegisterUserCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IUnitOfWork unitOfWork) : IRequestHandler<RegisterUserCommand, RegisterUserResult>
{
    public async Task<RegisterUserResult> Handle(RegisterUserCommand request, CancellationToken ct)
    {
        var existing = await userRepository.GetByEmailAsync(request.Email, ct);
        if (existing is not null)
            throw new EmailAlreadyInUseException(request.Email);

        var user = User.Create(request.Email, passwordHasher.Hash(request.Password));
        await userRepository.AddAsync(user, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return new RegisterUserResult(user.Id, user.Email);
    }
}
```

Un `ValidationBehavior<TRequest,TResponse>` — un pipeline behavior de MediatR — corre todos
los validadores registrados antes de que el handler se ejecute, así ningún handler necesita
validar manualmente sus parámetros:

```csharp
public class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var failures = (await Task.WhenAll(validators.Select(v => v.ValidateAsync(request, ct))))
            .SelectMany(r => r.Errors).ToList();

        if (failures.Count != 0)
            throw new ValidationException(failures);

        return await next();
    }
}
```

**Infrastructure** implementa esas interfaces con EF Core y SQL Server, y agrega la
generación de JWT:

```csharp
services.AddDbContext<IdentityDbContext>(options =>
    options.UseSqlServer(configuration.GetConnectionString("IdentityDb")));

services.AddScoped<IUserRepository, UserRepository>();
services.AddScoped<IUnitOfWork, UnitOfWork>();
services.AddSingleton<IPasswordHasher, PasswordHasher>();
services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
```

Un detalle que rompe endpoints protegidos si se pasa por alto: ASP.NET Core, por defecto,
remapea los claims estándar del JWT (`sub`, `email`) a URIs largas al validarlos. Sin
desactivar eso, un endpoint como `GET /api/auth/me` recibe un token válido pero
`ClaimsPrincipal.FindFirst("sub")` devuelve `null`:

```csharp
.AddJwtBearer(options =>
{
    options.MapInboundClaims = false; // conserva los nombres cortos de los claims
    options.TokenValidationParameters = new TokenValidationParameters { /* ... */ };
});
```

Con Identity funcionando (registro, login, endpoint protegido), se genera la migración
inicial y se prueba contra una base real antes de seguir:

```bash
dotnet ef migrations add InitialCreate \
  --project src/services/Identity/Identity.Infrastructure \
  --startup-project src/services/Identity/Identity.Api \
  --output-dir Persistence/Migrations

dotnet ef database update \
  --project src/services/Identity/Identity.Infrastructure \
  --startup-project src/services/Identity/Identity.Api
```

## 4. Catalog y Orders: CQRS con read model separado

Con la plantilla de 4 capas ya probada, Catalog y Orders se arman igual, pero acá el CQRS
se vuelve más interesante: en vez de que las queries lean directo de la tabla de escritura,
tienen su **propio modelo de lectura** desnormalizado, en su propia tabla.

En Catalog: `Products` es la tabla normalizada donde escribe el comando
`CreateProductCommand`; `ProductReadModels` es una proyección optimizada para las queries
de listado/búsqueda. Como todavía no hay bus de eventos en esta etapa del proyecto, la
proyección se actualiza de forma síncrona, en el mismo handler que confirma la escritura:

```csharp
public async Task<CreateProductResult> Handle(CreateProductCommand request, CancellationToken ct)
{
    var product = Product.Create(request.Sku, request.Name, request.Description, request.Category, request.Price);

    await productRepository.AddAsync(product, ct);
    await unitOfWork.SaveChangesAsync(ct);

    // Sin bus de eventos todavía: la proyección se actualiza justo después de confirmar
    // el write model. Cuando llegue RabbitMQ, este paso pasa a ser un consumer async.
    await productReadStore.UpsertAsync(new ProductSummary(product.Id, product.Sku, /* ... */), ct);

    return new CreateProductResult(product.Id, product.Sku);
}
```

En Orders aparece el primer punto delicado de mapeo con EF Core: el aggregate `Order`
expone sus líneas como `IReadOnlyCollection<OrderLine>`, respaldadas por un
`List<OrderLine>` privado — no un `List<T>` público que EF Core pueda mutar directamente.
Para que EF Core pueda materializar esa colección igual, hay que decirle explícitamente que
use el campo privado en vez de la propiedad pública:

```csharp
builder.OwnsMany(o => o.Lines, lines =>
{
    lines.ToTable("OrderLines");
    lines.WithOwner().HasForeignKey("OrderId");
    lines.HasKey(l => l.Id);
    lines.Ignore(l => l.LineTotal); // propiedad calculada, no una columna
});

builder.Navigation(o => o.Lines).Metadata.SetPropertyAccessMode(PropertyAccessMode.Field);
```

Sin esa última línea, EF Core intenta usar el setter público de `Lines` (que no existe) y
falla al construir el modelo.

## 5. Inventory, Payments y la saga de checkout

Acá entra RabbitMQ. Antes de escribir el primer consumer, conviene tener el broker
corriendo:

```bash
docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management
```

Y agregar MassTransit a los servicios que van a publicar o consumir eventos:

```bash
dotnet add Orders.Infrastructure package MassTransit -v 8.5.10
dotnet add Orders.Infrastructure package MassTransit.RabbitMQ -v 8.5.10
```

**La versión importa**: MassTransit 9.x exige una licencia comercial y falla al arrancar
sin una. 8.5.10 es la última versión completamente libre.

### El flujo de eventos

```
Orders (POST /api/orders) --OrderCreated--> Inventory
Inventory --StockReserved(con el monto total) o StockRejected--> Payments / Orders
Payments --PaymentCompleted o PaymentFailed--> Orders
Orders --OrderConfirmed o OrderCancelled--> Notifications
```

Un detalle de diseño no obvio: `StockReserved` lleva el `TotalAmount` del pedido, copiado
del `OrderCreated` original por Inventory. La alternativa —que Payments escuche
`OrderCreated` directamente para enterarse del monto— crearía una carrera real: si
`StockReserved` le llega a Payments antes de que termine de procesar `OrderCreated`, no
tendría el monto todavía. Haciendo que Inventory reenvíe el dato, Payments solo necesita
escuchar un evento.

Inventory reserva stock todo-o-nada: primero verifica que **todas** las líneas del pedido
tengan stock suficiente, y recién ahí muta cualquier cosa:

```csharp
var canReserveAll = request.Lines.All(line =>
    itemsByProduct.TryGetValue(line.ProductId, out var item) && item.AvailableQuantity >= line.Quantity);

if (!canReserveAll)
{
    await eventPublisher.PublishAsync(new StockRejected(/* ... */), ct);
    return; // nada se reserva si una sola línea no alcanza
}

foreach (var line in request.Lines)
    itemsByProduct[line.ProductId].TryReserve(line.Quantity);
```

Payments simula una pasarela real (Stripe en modo test, por ejemplo), rechazando montos
grandes para poder probar el camino de falla sin depender de un proveedor externo:

```csharp
public class SimulatedPaymentGateway : IPaymentGateway
{
    private const decimal RejectionThreshold = 1000m;

    public PaymentGatewayResult Charge(Guid orderId, decimal amount) =>
        amount > RejectionThreshold
            ? new PaymentGatewayResult(false, $"Pago rechazado: el monto {amount:N2} supera el límite permitido.")
            : new PaymentGatewayResult(true, null);
}
```

Si el pago falla, Inventory también escucha `PaymentFailed` y libera la reserva — es la
compensación de la saga. Como los mensajes pueden entregarse más de una vez (entrega "al
menos una vez"), tanto `Order.Confirm()`/`Order.Cancel()` como `OrderReservation.Release()`
son idempotentes: si ya están en el estado destino, no hacen nada.

### El bug de las colas duplicadas

Un problema real que aparece apenas hay más de un servicio consumiendo eventos: MassTransit
deriva el nombre de la cola de RabbitMQ del **nombre de la clase del consumer**, no del
ensamblado. Si Orders e Inventory tienen cada uno una clase `PaymentFailedConsumer`, ambos
terminan escuchando la *misma* cola en vez de tener cada uno la suya — y entonces solo uno
de los dos procesa cada mensaje, al azar. La solución es nombrar cada cola explícitamente,
prefijada por servicio:

```csharp
cfg.ReceiveEndpoint("orders-payment-failed", e => e.ConfigureConsumer<PaymentFailedConsumer>(context));
cfg.ReceiveEndpoint("inventory-payment-failed", e => e.ConfigureConsumer<PaymentFailedConsumer>(context));
```

## 6. Notifications y el API Gateway

Notifications no tiene estado de negocio propio — su tabla es apenas una bitácora de lo que
"envió" (simulado: un log más un registro persistido), útil para verificar la saga
end-to-end. Consume `OrderConfirmed` y `OrderCancelled`. Para que sepa a quién avisar, esos
dos eventos necesitan llevar el `CustomerId` — un ajuste al contrato compartido que hubo
que hacer retroactivamente cuando este servicio se sumó.

El Gateway es el único de los 7 servicios sin las 4 capas: es solo configuración de YARP
enrutando por prefijo de path hacia los demás servicios.

```bash
dotnet new web -n Gateway.Api -f net8.0
dotnet add Gateway.Api package Yarp.ReverseProxy
```

```json
{
  "ReverseProxy": {
    "Routes": {
      "orders-route": { "ClusterId": "orders-cluster", "Match": { "Path": "/api/orders/{**catch-all}" } }
    },
    "Clusters": {
      "orders-cluster": { "Destinations": { "destination1": { "Address": "http://localhost:5089" } } }
    }
  }
}
```

Con los 7 servicios arriba, el flujo completo se prueba pasando únicamente por el Gateway:
registrarse, loguearse, crear un producto, darle stock, crear un pedido, y verificar que la
saga lo termina en `Confirmed` o `Cancelled` según corresponda.

## 7. Dockerizando todo el stack

Cada servicio recibe un Dockerfile multi-stage (SDK para compilar, ASP.NET runtime para
correr):

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish "src/services/Orders/Orders.Api/Orders.Api.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Orders.Api.dll"]
```

Para que `docker compose up` no requiera correr migraciones a mano contra cada contenedor,
cada `Program.cs` las aplica al arrancar, con reintentos (SQL Server puede tardar unos
segundos más en aceptar conexiones aunque el healthcheck ya haya pasado):

```csharp
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
    for (var attempt = 1; ; attempt++)
    {
        try { dbContext.Database.Migrate(); break; }
        catch when (attempt < 10) { Thread.Sleep(TimeSpan.FromSeconds(3)); }
    }
}
```

Las cadenas de conexión cambian entre correr localmente y correr en Docker (`localhost` vs.
el nombre del servicio en Compose, como `sqlserver`). En vez de variables de entorno
sueltas, cada servicio tiene un `appsettings.Docker.json` que se activa poniendo
`ASPNETCORE_ENVIRONMENT=Docker` en el `docker-compose.yml` — el patrón estándar de
configuración por entorno de ASP.NET Core, aplicado a un entorno nuevo en vez de solo
`Development`/`Production`.

```yaml
services:
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    healthcheck:
      test: ["CMD-SHELL", "/opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P \"...\" -Q 'SELECT 1'"]

  orders-api:
    build:
      context: .
      dockerfile: src/services/Orders/Orders.Api/Dockerfile
    environment:
      ASPNETCORE_ENVIRONMENT: Docker
    depends_on:
      sqlserver: { condition: service_healthy }
      rabbitmq: { condition: service_healthy }
```

## 8. Pruebas unitarias

Con los 7 servicios funcionando, toca cubrir la lógica con tests. Un proyecto xUnit por
servicio, con Moq para los repositorios/publishers y FluentAssertions para las
aserciones — sin tocar base de datos ni RabbitMQ real:

```bash
dotnet new xunit -n Orders.Tests -f net8.0
dotnet add Orders.Tests reference src/services/Orders/Orders.Domain src/services/Orders/Orders.Application
dotnet add Orders.Tests package Moq
dotnet add Orders.Tests package FluentAssertions
```

Los tests de mayor valor son los que ejercitan las reglas de negocio que viven en el
Domain — transiciones de estado, idempotencia — y los handlers de Application con sus
dependencias mockeadas:

```csharp
[Fact]
public void Confirm_WhenAlreadyConfirmed_IsIdempotent()
{
    var order = Order.Create(Guid.NewGuid(), OneLine());
    order.Confirm();

    var act = () => order.Confirm();

    act.Should().NotThrow();
    order.Status.Should().Be(OrderStatus.Confirmed);
}
```

## 9. Pruebas de integración con Testcontainers

Los mocks no detectan errores de mapeo de EF Core ni de configuración real de MassTransit —
para eso hacen falta pruebas contra infraestructura real. Testcontainers levanta SQL Server
y RabbitMQ reales en contenedores efímeros, uno por clase de test:

```bash
dotnet add tests/IntegrationTests.Shared package Testcontainers.MsSql
dotnet add tests/IntegrationTests.Shared package Testcontainers.RabbitMq
dotnet add tests/IntegrationTests.Shared package Microsoft.AspNetCore.Mvc.Testing
```

```csharp
public class SqlServerFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
        .WithPassword("...")
        .Build();

    public Task InitializeAsync() => _container.StartAsync();
    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    public string GetConnectionString(string database) =>
        $"Server={_container.Hostname},{_container.GetMappedPublicPort(1433)};Database={database};...";
}
```

Cada servicio tiene su propia `WebApplicationFactory<Program>` que reemplaza la cadena de
conexión y el host de RabbitMQ por los del contenedor de test:

```csharp
public class OrdersApiFactory(string sqlConnectionString, string rabbitMqHost, int rabbitMqPort)
    : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:OrdersDb"] = sqlConnectionString,
                ["RabbitMq:Host"] = rabbitMqHost,
                ["RabbitMq:Port"] = rabbitMqPort.ToString()
            });
        });
    }
}
```

Dos problemas reales aparecieron al conectar esto contra un RabbitMQ de Testcontainers en
vez del de desarrollo:

- **Testcontainers asigna un puerto aleatorio** al mapear el 5672 del contenedor. El código
  de producción, hasta ese momento, asumía siempre el puerto por defecto — hubo que agregar
  soporte para un puerto configurable en la configuración de MassTransit de los 4 servicios
  que publican o consumen eventos.
- **Sin fijar usuario/contraseña explícitos**, el contenedor de RabbitMQ de Testcontainers
  puede generar credenciales que no coinciden con las que el servicio bajo prueba espera
  (`guest`/`guest`), y la conexión falla con `ACCESS_REFUSED`. Fijarlas explícitamente en el
  fixture lo resuelve.

El test más completo del repositorio levanta los 4 servicios de la saga
(Orders/Inventory/Payments/Notifications) juntos, contra un único SQL Server y un único
RabbitMQ de Testcontainers, y recorre los 3 caminos posibles sin mockear nada:

```csharp
[Fact]
public async Task HappyPath_OrderIsConfirmedPaymentCompletesAndNotificationIsSent()
{
    await SeedStockAsync(productId, 10);

    var created = await CreateOrderAsync(customerId, productId, quantity: 3);
    var order = await PollOrderUntilAsync(created.OrderId, "Confirmed", "Cancelled");

    order.Status.Should().Be("Confirmed");
    // ... verifica pago Completed, stock descontado, y notificación registrada
}
```

Para que ~7 proyectos de integración no saturen Docker corriendo en paralelo (cada uno
levanta sus propios contenedores), un `tests/IntegrationTests/.runsettings` fuerza
ejecución secuencial:

```bash
dotnet test --settings tests/IntegrationTests/.runsettings
```

## 10. Outbox pattern: escritura y publicación atómicas

Hasta acá, cada handler que publica un evento hace dos pasos separados: confirma la
escritura en base de datos (`unitOfWork.SaveChangesAsync()`) y recién después publica el
evento (`eventPublisher.PublishAsync(...)`, que en definitiva llama a
`IPublishEndpoint.Publish`). Si el proceso se cae justo entre medio, el evento se pierde (o
se duplica, según el punto exacto de la caída) — rompiendo la consistencia de la saga.

MassTransit trae soporte nativo para el Outbox pattern sobre EF Core, y hay que elegir entre
dos variantes según **quién dispara la publicación**:

- **Bus Outbox** (`o.UseBusOutbox()`): para publicaciones que salen de *fuera* de un
  consumer — el único caso acá es `CreateOrder`, disparado directo desde el endpoint HTTP de
  Orders.
- **Outbox transaccional de consumer** (`cfg.UseEntityFrameworkOutbox<TDbContext>(context)`
  aplicado a cada `ReceiveEndpoint`): envuelve el procesamiento del consumer en una
  transacción, guarda el evento saliente junto con el `SaveChanges` del handler, y agrega
  deduplicación de entrada (tabla `InboxState`). Aplica a todo lo demás — `ConfirmOrder`,
  `CancelOrder`, `ReserveStock`, `ProcessPayment` — porque todos esos handlers se disparan
  desde un consumer, no desde HTTP.

```bash
dotnet add Orders.Infrastructure package MassTransit.EntityFrameworkCore -v 8.4.1
```

En el `DbContext` hace falta registrar las tres entidades del outbox:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrdersDbContext).Assembly);
    modelBuilder.AddInboxStateEntity();
    modelBuilder.AddOutboxMessageEntity();
    modelBuilder.AddOutboxStateEntity();
}
```

Y en la configuración de MassTransit, Orders necesita ambas variantes (tiene un publish
HTTP-triggered y publishes consumer-triggered), mientras que Inventory y Payments solo
necesitan la del consumer:

```csharp
x.AddEntityFrameworkOutbox<OrdersDbContext>(o =>
{
    o.UseSqlServer();
    o.UseBusOutbox();
});

x.UsingRabbitMq((context, cfg) =>
{
    cfg.Host(/* ... */);

    cfg.ReceiveEndpoint("orders-payment-completed", e =>
    {
        e.UseEntityFrameworkOutbox<OrdersDbContext>(context);
        e.ConfigureConsumer<PaymentCompletedConsumer>(context);
    });
});
```

El detalle que rompe todo si se pasa por alto: el Bus Outbox bufferea la publicación en el
`DbContext` ambiente, pero recién la vuelca a la tabla `OutboxMessage` en el próximo
`SaveChangesAsync`. Eso obliga a **publicar antes de guardar**, no después:

```csharp
await orderRepository.AddAsync(order, cancellationToken);

// El evento se publica antes de SaveChanges para que el outbox lo bufferee
// y lo persista de forma atómica junto con el write model.
await eventPublisher.PublishAsync(new OrderCreatedEvent(/* ... */), cancellationToken);
await unitOfWork.SaveChangesAsync(cancellationToken);
```

Con el modelo cambiado, hace falta una migración nueva por servicio (crea las tablas
`InboxState`, `OutboxMessage` y `OutboxState`):

```bash
dotnet ef migrations add AddOutbox \
  --project src/services/Orders/Orders.Infrastructure \
  --startup-project src/services/Orders/Orders.Api
```

## 11. Resiliencia: retry y circuit breaker

Acá aparece una premisa que no encaja con el código tal como está: "agregar circuit breaker
en las llamadas HTTP entre servicios" no aplica directamente, porque **no hay HTTP síncrono
entre microservicios** — todo el tráfico entre Orders/Inventory/Payments/Notifications es
mensajería. Y la pasarela de pago simulada tampoco hacía ninguna llamada de red: era un
método síncrono que solo comparaba el monto contra un umbral. Antes de poder agregar
resiliencia hay que decidir qué significa "resiliencia" en cada uno de esos dos casos.

**Entre servicios**, el equivalente real de retry/circuit breaker es el que trae
MassTransit para sus consumers, agregado a nivel de bus (antes de los `ReceiveEndpoint`, así
envuelve todo el pipeline de cada consumer, outbox incluido):

```csharp
cfg.UseMessageRetry(r => r.Exponential(
    retryLimit: 3,
    minInterval: TimeSpan.FromMilliseconds(200),
    maxInterval: TimeSpan.FromSeconds(5),
    intervalDelta: TimeSpan.FromMilliseconds(200)));

cfg.UseCircuitBreaker(cb =>
{
    cb.TrackingPeriod = TimeSpan.FromMinutes(1);
    cb.TripThreshold = 15;
    cb.ActiveThreshold = 10;
    cb.ResetInterval = TimeSpan.FromMinutes(5);
});
```

Tras agotar los reintentos, MassTransit mueve el mensaje a su cola `_error` — comportamiento
estándar, sin configuración adicional. No hay riesgo nuevo de duplicados: los handlers ya
eran idempotentes desde la sección 5, y el `InboxState` del outbox suma deduplicación extra.

**Hacia la pasarela de pago**, en cambio, sí tiene sentido un `HttpClient` real con Polly —
pero primero hay que convertir la pasarela simulada en una llamada HTTP de verdad. Se agrega
un endpoint interno en `Payments.Api` que simula latencia y una tasa de falla transitoria, y
`IPaymentGateway` pasa a ser async:

```csharp
gateway.MapPost("/charge", async (GatewayChargeRequest request) =>
{
    await Task.Delay(Random.Shared.Next(50, 250));       // latencia simulada
    if (Random.Shared.NextDouble() < 0.2)                // ~20% falla transitoria
        return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);

    var result = SimulatedGatewayBackend.Charge(request.OrderId, request.Amount);
    return Results.Ok(new GatewayChargeResponse(result.Success, result.FailureReason));
});
```

El cliente que lo llama se registra con `AddStandardResilienceHandler()` — la API estándar
de .NET 8 para esto (paquete `Microsoft.Extensions.Http.Resilience`, Polly v8 por debajo),
que agrega retry con backoff exponencial, circuit breaker y timeout sin tener que componer
la pipeline a mano:

```bash
dotnet add Payments.Infrastructure package Microsoft.Extensions.Http.Resilience
```

```csharp
services.AddHttpClient<IPaymentGateway, HttpPaymentGateway>(client =>
{
    client.BaseAddress = new Uri(configuration["PaymentGateway:BaseUrl"]!);
})
.AddStandardResilienceHandler();
```

El punto que hace que esto sea correcto y no solo "reintentar todo": `HttpPaymentGateway`
llama a `response.EnsureSuccessStatusCode()` antes de leer el resultado. Un 503 simulado
(falla transitoria) lanza `HttpRequestException`, que Polly reintenta; un 200 OK con
`success:false` (el pago se rechazó porque el monto supera el umbral) **no** se reintenta —
es una decisión de negocio legítima, no una falla.

## 12. Observabilidad: OpenTelemetry y Jaeger

Con outbox y resiliencia en su lugar, falta poder *ver* el recorrido de un pedido a través
de los 5 servicios que toca la saga (Gateway, Orders, Inventory, Payments, Notifications).
Como el cableado es casi idéntico en los cinco (con dos variantes: con/sin EF Core, con/sin
MassTransit), se centraliza en un proyecto compartido nuevo — mismo criterio que
`ECommerce.Contracts` en la sección 2 — en vez de repetir el boilerplate en cada `Program.cs`:

```bash
cd src/shared
dotnet new classlib -n ECommerce.Observability -f net8.0
cd ../..
dotnet sln add src/shared/ECommerce.Observability/ECommerce.Observability.csproj
```

```csharp
public static class ObservabilityExtensions
{
    public static WebApplicationBuilder AddOpenTelemetryTracing(
        this WebApplicationBuilder builder,
        string serviceName,
        bool includeEfCore = true,
        bool includeMassTransit = true)
    {
        var otlpEndpoint = builder.Configuration["OpenTelemetry:OtlpEndpoint"] ?? "http://localhost:4317";

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(serviceName))
            .WithTracing(tracing =>
            {
                tracing.AddAspNetCoreInstrumentation();
                tracing.AddHttpClientInstrumentation();
                if (includeEfCore) tracing.AddEntityFrameworkCoreInstrumentation();
                if (includeMassTransit) tracing.AddSource("MassTransit");
                tracing.AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint));
            });

        return builder;
    }
}
```

Cada `Program.cs` queda en una sola línea, justo después de crear el `builder`:

```csharp
builder.AddOpenTelemetryTracing("orders-api");
```

(Gateway, sin EF Core ni MassTransit propios, la llama con
`includeEfCore: false, includeMassTransit: false` — solo instrumenta ASP.NET Core y el
`HttpClient` que usa YARP para reenviar hacia los demás servicios.)

El punto que hace que esto conecte los 5 servicios en un único trace, sin escribir código de
correlación a mano: MassTransit 8.x ya trae su propio `ActivitySource` (literalmente
`"MassTransit"`) que propaga el contexto de traza W3C (`traceparent`) por los headers del
mensaje — alcanza con registrarlo (`tracing.AddSource("MassTransit")`). Lo mismo pasa con el
salto Gateway→Orders vía YARP: la propagación W3C es una capacidad nativa de
`HttpClient`/`SocketsHttpHandler` desde .NET 5+, automática en cuanto hay un `Activity`
ambiente.

Por último, Jaeger se suma a `docker-compose.yml` como un contenedor más (acepta OTLP
nativamente, sin necesitar un collector intermedio):

```yaml
jaeger:
  image: jaegertracing/all-in-one:1.76.0
  ports:
    - "16686:16686" # UI
    - "4317:4317"   # OTLP gRPC
    - "4318:4318"   # OTLP HTTP
  environment:
    COLLECTOR_OTLP_ENABLED: "true"
```

Con el stack arriba, un pedido creado a través del Gateway (`http://localhost:5080`) se
puede buscar en `http://localhost:16686`: un único trace conecta la entrada HTTP en el
Gateway, cada publish/consume de RabbitMQ (incluido el outbox) y la llamada HTTP a la
pasarela de pago — con el reintento de Polly visible como un span propio cuando la pasarela
simulada devuelve una falla transitoria.

## 13. Cómo seguir desde acá

Ideas para extender el proyecto en la misma línea:

- **Logs estructurados correlacionados**: Seq (o similar), con el trace ID de OpenTelemetry
  como campo común, para saltar de una traza en Jaeger a sus logs exactos y viceversa.
- **Sampling de trazas**: hoy se exporta el 100% del tráfico, razonable para una demo pero
  no para producción — vale la pena introducir tail-based sampling antes de llevar esto más
  lejos.
- **Chaos testing del circuit breaker**: un test que fuerce una tasa de falla del 100% en la
  pasarela simulada y verifique que el circuito de Polly efectivamente abre y deja de
  golpearla, en vez de solo confiar en la configuración.
- **Kubernetes**: el `docker-compose.yml` actual es el punto de partida natural para migrar
  a manifiestos de Helm cuando el proyecto necesite escalar más allá de una sola máquina.
