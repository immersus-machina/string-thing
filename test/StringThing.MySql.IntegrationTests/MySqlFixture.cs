using Testcontainers.MySql;
using Xunit;

namespace StringThing.MySql.IntegrationTests;

public class MySqlFixture : IAsyncLifetime
{
    private readonly MySqlContainer _container = new MySqlBuilder("mysql:8.0")
        .Build();

    public string ConnectionString { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();
        ConnectionString = _container.GetConnectionString();
    }

    public async ValueTask DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}
