using ECommerce.Observability;

var builder = WebApplication.CreateBuilder(args);

builder.AddOpenTelemetryTracing("gateway-api", includeEfCore: false, includeMassTransit: false);

builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.MapGet("/", () => Results.Ok(new { service = "API Gateway", status = "up" }));
app.MapReverseProxy();

app.Run();
