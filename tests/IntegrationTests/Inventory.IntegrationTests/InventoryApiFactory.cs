using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Inventory.IntegrationTests;

public class InventoryApiFactory(string sqlConnectionString, string rabbitMqHost, int rabbitMqPort) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:InventoryDb"] = sqlConnectionString,
                ["RabbitMq:Host"] = rabbitMqHost,
                ["RabbitMq:Port"] = rabbitMqPort.ToString()
            });
        });
    }
}
