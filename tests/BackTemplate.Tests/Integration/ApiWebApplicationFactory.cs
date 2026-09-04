using BackTemplate.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BackTemplate.Tests.Integration;

/// <summary>
/// Sobe a API inteira (pipeline HTTP real, DI real) contra o Postgres do
/// PostgresFixture. Ambiente "Testing" pra Program.cs pular a migration
/// automática - o schema é criado aqui via EnsureCreated, igual ao resto
/// dos testes.
/// </summary>
public class ApiWebApplicationFactory(string connectionString) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = connectionString,
                ["Jwt:Issuer"] = "test-issuer",
                ["Jwt:Audience"] = "test-audience",
                ["Jwt:SigningKey"] = "integration-test-signing-key-needs-32-bytes",
                ["Jwt:ExpiryMinutes"] = "60",
            });
        });
    }

    public async Task EnsureDatabaseCreatedAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();
    }
}
