<div align="center">

<img src=".github/logo.svg" alt="back-template-dotnet logo" width="120" height="120">

# back-template-dotnet

**An ASP.NET Core Web API starter with login, security and an automated N+1 query guard.**<br>
C#/.NET 10, EF Core and Postgres. Clone it and start building the real API.

[![CI](https://github.com/obrenoalvim/back-template-dotnet/actions/workflows/ci.yml/badge.svg)](https://github.com/obrenoalvim/back-template-dotnet/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![GitHub stars](https://img.shields.io/github/stars/obrenoalvim/back-template-dotnet?style=flat&logo=github&color=8b6cff)](https://github.com/obrenoalvim/back-template-dotnet/stargazers)
[![.NET 10](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white)](#whats-already-in)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-4169E1?logo=postgresql&logoColor=white)](#running-locally)

**English** · [Português](README.pt.md) · [Español](README.es.md)

[What's in](#whats-already-in) · [Running locally](#running-locally) · [Tests](#tests) · [Configuration](#configuration) · [Template family](#the-template-family) · [FAQ](#faq)

</div>

---

C#/.NET 10 backend starter: login, security and tests ready to go, so you start building the real API right away.

## What's already in

- ASP.NET Core Web API + EF Core 10 (Postgres via Npgsql), repository pattern
- JWT auth (register/login, password hashing with BCrypt)
- **Automated N+1 query guard** running as a regular test in CI (see below)
- Rate limiting on auth endpoints (10 req/min per IP, against brute force)
- Real health check (`/health` tests the actual Postgres connection, not just "app is up")
- Global exception middleware (returns `ProblemDetails`, never a raw stack trace)
- Interactive API docs via [Scalar](https://scalar.com) at `/scalar/v1` (Development environment)
- Docker + docker-compose (API + Postgres), image runs as a non-root user (`app`, uid 1654)
- GitHub Actions CI: build, test (unit + integration via `WebApplicationFactory`), Docker image build on every change

## Running locally

```bash
docker compose up -d
curl http://localhost:8081/health
```

The API applies EF Core migrations by itself on startup (`Database.Migrate()` in `Program.cs`). Nothing to run manually.

Want to change the Postgres password or the JWT signing key without editing `docker-compose.yml`? Copy `.env.example` to `.env` and adjust. If `.env` doesn't exist, the usual dev defaults still apply.

**Got Postgres installed natively on the machine (not in Docker)?** It may already be sitting on host port 5432, and Docker Desktop lets both listen at once with no error. The connection then lands on one or the other at random, and you see `password authentication failed` even with the right password. If that happens, stop the local Postgres service or remap the port in `docker-compose.yml` (e.g. `"5433:5432"`) before debugging anything else.

Endpoints:

```bash
curl -X POST http://localhost:8081/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"you@example.com","password":"correct-horse-battery-staple"}'

curl -X POST http://localhost:8081/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"you@example.com","password":"correct-horse-battery-staple"}'

curl http://localhost:8081/api/books -H "Authorization: Bearer <token>"
```

## Developing without Docker

You need the [.NET 10 SDK](https://dotnet.microsoft.com/download) and a local Postgres (or `docker compose up -d postgres`).

```bash
dotnet restore
dotnet build
dotnet run --project src/BackTemplate.Api
```

## Tests

```bash
dotnet test
```

Tests spin up a real Postgres via [Testcontainers](https://testcontainers.com/). Docker must be running on the machine (or on the CI runner, which already ships with Docker). Deliberately not using EF Core's `InMemory` provider: `InMemory` doesn't generate real SQL, so it can't be used to count queries.

Two test layers:

- **Unit** (`Auth/`, `QueryCount/`): calls the repository/service directly, against `AppDbContext`.
- **Integration** (`Integration/`): boots the whole API via `WebApplicationFactory<Program>`. Real routing, model binding, authentication and rate limiting, hit over HTTP.

### A config gotcha the integration tests caught

`Program.cs` uses top-level statements. If you read `builder.Configuration.GetConnectionString(...)` into a local variable **before** `builder.Build()` and capture that variable in a closure (`options.UseNpgsql(connectionString)`), the integration tests' `WebApplicationFactory` can't override that value. The read already happened against the real `appsettings.json` before the test override was applied. That's why `ConnectionStrings`, `Jwt` and the Postgres health check in this project are all resolved **inside** the configuration delegates (via injected `IConfiguration`/`IOptions`), never from a variable captured too early. Worth keeping that pattern when adding new configuration.

### The N+1 guard

`Diagnostics/QueryCountInterceptor.cs` counts how many SQL commands a `DbContext` runs. The `BookRepositoryQueryCountTests` test calls `BookRepository.GetAllWithAuthorsAsync()` and asserts it runs **exactly 1 query**, no matter how many books exist.

If someone later removes `.Include(b => b.Author)` from the repository (or swaps it for lazy access inside a loop), this test goes red in CI before it becomes a production performance problem. No paid profiler or external tool needed.

To use the same pattern on other repositories in your project:

```csharp
var interceptor = new QueryCountInterceptor();
await using var context = CreateContext(interceptor); // registers the interceptor on DbContextOptionsBuilder

interceptor.Reset();
var result = await myRepository.MethodThatShouldBeOneQuery();

Assert.Equal(1, interceptor.CommandCount);
```

## Adding a migration

```bash
dotnet tool restore
dotnet ef migrations add MigrationName \
  --project src/BackTemplate.Api/BackTemplate.Api.csproj \
  --startup-project src/BackTemplate.Api/BackTemplate.Api.csproj \
  -o Data/Migrations
```

## Configuration

Environment variables (override `appsettings.json`, `Section__Key` syntax):

| Variable | Description |
|---|---|
| `ConnectionStrings__Default` | Postgres connection string |
| `Jwt__Issuer` / `Jwt__Audience` | token issuer/audience |
| `Jwt__SigningKey` | HMAC signing key: **change the default value before going to production** |
| `Jwt__ExpiryMinutes` | token validity in minutes |

## Structure

```
src/BackTemplate.Api/
  Auth/             register, login, JWT generation
  Controllers/      HTTP endpoints
  Data/             DbContext, entities, migrations
  Diagnostics/       QueryCountInterceptor (N+1 guard)
  Middleware/        global exception handling
  RateLimiting/      rate limit policy names
  Repositories/     data access

tests/BackTemplate.Tests/
  Auth/             register/login tests (calling the service directly)
  Integration/       tests hitting the real API via WebApplicationFactory
  QueryCount/       N+1 guard test
  TestFixtures/      Postgres fixture via Testcontainers
```

## Out of scope (for now)

Front-end, automated cloud deploy, publishing the Docker image to a registry.

## Known warning

`dotnet build`/`dotnet test` shows an `NU1903` warning about a vulnerability in the `SSH.NET` package. It's a transitive dependency of Testcontainers (test-only, never published in the production image). No risk to the running API.

---

## FAQ

**Which database does it use?**
Postgres through Npgsql and EF Core 10. `docker compose up -d` starts the API and Postgres together, and the API applies migrations on startup.

**What is the N+1 guard?**
A test that counts the SQL commands a `DbContext` runs and asserts that `GetAllWithAuthorsAsync()` runs exactly one query. If someone removes the `.Include`, the test fails in CI. See [The N+1 guard](#the-n1-guard).

**Why Testcontainers instead of EF Core's InMemory provider?**
`InMemory` does not generate real SQL, so it cannot count queries.

**Is it ready for production?**
It gives you auth, rate limiting, a real health check and a non-root Docker image. Before you deploy, change `Jwt__SigningKey`. Front-end, automated cloud deploy and publishing the image to a registry are out of scope.

## The template family

Same idea, different stack. Clone one and start building.

| Layer | Starter |
|---|---|
| Backend | [Spring Boot](https://github.com/obrenoalvim/back-template-spring) · [Go](https://github.com/obrenoalvim/back-template-go) · [FastAPI](https://github.com/obrenoalvim/back-template-fastapi) · [NestJS](https://github.com/obrenoalvim/back-template-nest) · [Laravel](https://github.com/obrenoalvim/back-template-laravel) · **ASP.NET Core (this repo)** |
| Frontend | [Angular](https://github.com/obrenoalvim/front-template-angular) · [React](https://github.com/obrenoalvim/front-template-react) · [SvelteKit](https://github.com/obrenoalvim/front-template-sveltekit) · [Vue](https://github.com/obrenoalvim/front-template-vue) |
| Full-stack | [Next.js](https://github.com/obrenoalvim/next-template) |

## License

[MIT](LICENSE)

---

<div align="center">

If this saved you a day of setup, a ⭐ helps other developers find it.

<sub>**Topics:** aspnetcore · dotnet · csharp · web-api · entity-framework-core · jwt-authentication · postgresql · docker · testcontainers · boilerplate · starter-kit · backend-template</sub>

</div>
