using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Payments.IntegrationTests;

public class PaymentsApiFactory(string sqlConnectionString, string rabbitMqHost, int rabbitMqPort) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PaymentsDb"] = sqlConnectionString,
                ["RabbitMq:Host"] = rabbitMqHost,
                ["RabbitMq:Port"] = rabbitMqPort.ToString()
            });
        });
    }
}
