using Testcontainers.MsSql;
using Xunit;

namespace IntegrationTests.Shared;

// Un contenedor de SQL Server real por clase de test (vía IClassFixture),
// para no compartir estado entre clases y poder correr en paralelo.
public class SqlServerFixture : IAsyncLifetime
{
    private const string Password = "Testcontainers_2026!";

    private readonly MsSqlContainer _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
        .WithPassword(Password)
        .Build();

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    public string GetConnectionString(string database)
        => $"Server={_container.Hostname},{_container.GetMappedPublicPort(1433)};Database={database};User Id=sa;Password={Password};TrustServerCertificate=True";
}
