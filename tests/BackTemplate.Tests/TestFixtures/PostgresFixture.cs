using BackTemplate.Api.Data;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace BackTemplate.Tests.TestFixtures;

/// <summary>
/// Sobe um Postgres real via Testcontainers pros testes. De propósito não usa o
/// provider InMemory do EF Core: InMemory não gera SQL de verdade, então não dá
/// pra contar query nele - o guarda de N+1 depende de um banco real.
/// </summary>
public class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("backtemplate_test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync();
    }

    public AppDbContext CreateContext(BackTemplate.Api.Diagnostics.QueryCountInterceptor? interceptor = null)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(ConnectionString);

        if (interceptor is not null)
        {
            optionsBuilder.AddInterceptors(interceptor);
        }

        return new AppDbContext(optionsBuilder.Options);
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}
