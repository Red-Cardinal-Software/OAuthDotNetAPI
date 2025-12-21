using Testcontainers.MsSql;

namespace WebApi.Integration.Tests.Fixtures;

/// <summary>
/// xUnit fixture that manages the SQL Server test container lifecycle.
/// The container is started once and shared across all tests in the collection.
/// </summary>
public class SqlServerContainerFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}