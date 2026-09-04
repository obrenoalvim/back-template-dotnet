using System.Text;
using System.Threading.RateLimiting;
using BackTemplate.Api.Auth;
using BackTemplate.Api.Data;
using BackTemplate.Api.Middleware;
using BackTemplate.Api.RateLimiting;
using BackTemplate.Api.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// A connection string e as opções de Jwt são lidas de dentro dos delegates de
// configuração (via IConfiguration/IOptions resolvidos pelo DI), não numa
// variável local capturada aqui em cima. WebApplicationFactory (testes de
// integração) só injeta os valores de teste depois que esse arquivo já rodou
// até aqui - uma leitura eager de builder.Configuration nesse ponto captura
// sempre o appsettings.json de produção, ignorando qualquer override de teste.
static string GetConnectionString(IConfiguration configuration) =>
    configuration.GetConnectionString("Default")
        ?? throw new InvalidOperationException("Missing ConnectionStrings:Default");

builder.Services.AddDbContext<AppDbContext>((sp, options) =>
    options.UseNpgsql(GetConnectionString(sp.GetRequiredService<IConfiguration>())));

builder.Services.AddHealthChecks()
    .AddNpgSql(sp => GetConnectionString(sp.GetRequiredService<IConfiguration>()), name: "postgres");

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();

builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtOptions>>((bearerOptions, jwtOptionsAccessor) =>
    {
        var jwtOptions = jwtOptionsAccessor.Value;
        bearerOptions.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
        };
    });

builder.Services.AddAuthorization();

// Trava tentativas de login/registro: 10 requisições por minuto por IP.
// Objetivo é dificultar brute force, não é rate limit de produto (ajuste pro seu caso).
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy(RateLimiterPolicies.Auth, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
            }));
});

builder.Services.AddScoped<IBookRepository, BookRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

var app = builder.Build();

if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseRateLimiter();

app.MapControllers();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new { name = e.Key, status = e.Value.Status.ToString() }),
        });
    },
}).AllowAnonymous();

app.Run();

public partial class Program;
