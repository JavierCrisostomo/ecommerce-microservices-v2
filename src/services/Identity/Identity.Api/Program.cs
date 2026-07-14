using System.Text;
using FluentValidation;
using Identity.Api.Contracts;
using Identity.Application;
using Identity.Application.Exceptions;
using Identity.Application.Users.Commands.Login;
using Identity.Application.Users.Commands.RegisterUser;
using Identity.Infrastructure;
using Identity.Infrastructure.Persistence;
using Identity.Infrastructure.Security;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["Secret"]!))
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

// Aplica las migraciones al arrancar: evita tener que correr `dotnet ef database
// update` a mano contra cada contenedor. Reintenta porque SQL Server puede tardar
// unos segundos más en aceptar conexiones aunque el healthcheck ya haya pasado.
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
    for (var attempt = 1; ; attempt++)
    {
        try
        {
            dbContext.Database.Migrate();
            break;
        }
        catch when (attempt < 10)
        {
            Thread.Sleep(TimeSpan.FromSeconds(3));
        }
    }
}

if (app.Environment.IsDevelopment() || app.Environment.EnvironmentName == "Docker")
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var feature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
        var exception = feature?.Error;

        var (statusCode, title) = exception switch
        {
            ValidationException validationException => (StatusCodes.Status400BadRequest, string.Join("; ", validationException.Errors.Select(e => e.ErrorMessage))),
            EmailAlreadyInUseException emailEx => (StatusCodes.Status409Conflict, emailEx.Message),
            InvalidCredentialsException credEx => (StatusCodes.Status401Unauthorized, credEx.Message),
            _ => (StatusCodes.Status500InternalServerError, "Ocurrió un error inesperado.")
        };

        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(new { title });
    });
});

var auth = app.MapGroup("/api/auth");

auth.MapPost("/register", async (RegisterRequest request, ISender sender, CancellationToken cancellationToken) =>
{
    var result = await sender.Send(new RegisterUserCommand(request.Email, request.Password), cancellationToken);
    return Results.Created($"/api/auth/users/{result.UserId}", result);
});

auth.MapPost("/login", async (LoginRequest request, ISender sender, CancellationToken cancellationToken) =>
{
    var result = await sender.Send(new LoginCommand(request.Email, request.Password), cancellationToken);
    return Results.Ok(result);
});

app.MapGet("/api/auth/me", (System.Security.Claims.ClaimsPrincipal user) =>
    Results.Ok(new
    {
        UserId = user.FindFirst("sub")?.Value,
        Email = user.FindFirst("email")?.Value,
        Role = user.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value
    })).RequireAuthorization();

app.Run();

public partial class Program;
