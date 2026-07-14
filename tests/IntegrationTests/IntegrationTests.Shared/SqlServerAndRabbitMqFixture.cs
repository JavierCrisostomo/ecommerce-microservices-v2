using Testcontainers.MsSql;
using Testcontainers.RabbitMq;
using Xunit;

namespace IntegrationTests.Shared;

// Para los servicios que además publican/consumen eventos: un SQL Server y un
// RabbitMQ reales por clase de test.
public class SqlServerAndRabbitMqFixture : IAsyncLifetime
{
    private const string SqlPassword = "Testcontainers_2026!";

    private readonly MsSqlContainer _sqlContainer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
        .WithPassword(SqlPassword)
        .Build();

    private const string RabbitMqUsername = "guest";
    private const string RabbitMqPassword = "guest";

    private readonly RabbitMqContainer _rabbitMqContainer = new RabbitMqBuilder("rabbitmq:3-management")
        .WithUsername(RabbitMqUsername)
        .WithPassword(RabbitMqPassword)
        .Build();

    public string RabbitMqHost => _rabbitMqContainer.Hostname;
    public int RabbitMqPort => _rabbitMqContainer.GetMappedPublicPort(5672);
    public string RabbitMqUsernameValue => RabbitMqUsername;
    public string RabbitMqPasswordValue => RabbitMqPassword;

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_sqlContainer.StartAsync(), _rabbitMqContainer.StartAsync());
    }

    public async Task DisposeAsync()
    {
        await Task.WhenAll(_sqlContainer.DisposeAsync().AsTask(), _rabbitMqContainer.DisposeAsync().AsTask());
    }

    public string GetSqlConnectionString(string database)
        => $"Server={_sqlContainer.Hostname},{_sqlContainer.GetMappedPublicPort(1433)};Database={database};User Id=sa;Password={SqlPassword};TrustServerCertificate=True";
}
