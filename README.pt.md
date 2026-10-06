<div align="center">

<img src=".github/logo.svg" alt="Logo do back-template-dotnet" width="120" height="120">

# back-template-dotnet

**Um starter de Web API ASP.NET Core com login, segurança e um guard automático de queries N+1.**<br>
C#/.NET 10, EF Core e Postgres. Clona e já sai construindo a API de verdade.

[![CI](https://github.com/obrenoalvim/back-template-dotnet/actions/workflows/ci.yml/badge.svg)](https://github.com/obrenoalvim/back-template-dotnet/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![GitHub stars](https://img.shields.io/github/stars/obrenoalvim/back-template-dotnet?style=flat&logo=github&color=8b6cff)](https://github.com/obrenoalvim/back-template-dotnet/stargazers)
[![.NET 10](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white)](#o-que-já-vem-pronto)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-4169E1?logo=postgresql&logoColor=white)](#rodando-local)

[English](README.md) · **Português** · [Español](README.es.md)

[O que vem pronto](#o-que-já-vem-pronto) · [Rodando local](#rodando-local) · [Testes](#testes) · [Configuração](#configuração) · [Família de templates](#a-família-de-templates) · [Perguntas frequentes](#perguntas-frequentes)

</div>

---

Backend starter em C#/.NET 10: login, segurança e testes prontos pra começar a construir a API de verdade.

## O que já vem pronto

- ASP.NET Core Web API + EF Core 10 (Postgres via Npgsql), padrão repository
- Autenticação JWT (registro/login, hash de senha com BCrypt)
- **Guarda automatizado contra queries N+1** rodando como teste normal no CI (ver abaixo)
- Rate limiting nos endpoints de auth (10 req/min por IP, contra brute force)
- Health check real (`/health` testa a conexão com o Postgres, não só "app de pé")
- Middleware global de exceção (devolve `ProblemDetails`, nunca stack trace cru)
- Documentação interativa da API via [Scalar](https://scalar.com) em `/scalar/v1` (ambiente Development)
- Docker + docker-compose (API + Postgres), imagem roda como usuário não-root (`app`, uid 1654)
- CI no GitHub Actions: build, test (unitário + integração via `WebApplicationFactory`), build da imagem Docker em toda alteração

## Rodando local

```bash
docker compose up -d
curl http://localhost:8081/health
```

A API aplica as migrations do EF Core sozinha ao subir (`Database.Migrate()` no `Program.cs`). Não precisa rodar nada manual.

Quer trocar a senha do Postgres ou a chave de assinatura JWT sem editar `docker-compose.yml`? Copie `.env.example` pra `.env` e ajuste. Se `.env` não existir, os defaults de dev de sempre continuam valendo.

**Tem Postgres instalado nativamente na máquina (não em Docker)?** Ele pode já estar ocupando a porta 5432 no host, e o Docker Desktop deixa os dois escutando ao mesmo tempo sem erro. A conexão então cai ora num, ora no outro, e você vê `password authentication failed` mesmo com a senha certa. Se acontecer, pare o serviço Postgres local ou troque o mapeamento de porta em `docker-compose.yml` (`"5433:5432"`, por exemplo) antes de depurar qualquer outra coisa.

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

## Desenvolvendo sem Docker

Precisa do [.NET 10 SDK](https://dotnet.microsoft.com/download) e um Postgres local (ou `docker compose up -d postgres`).

```bash
dotnet restore
dotnet build
dotnet run --project src/BackTemplate.Api
```

## Testes

```bash
dotnet test
```

Os testes sobem um Postgres real via [Testcontainers](https://testcontainers.com/). Precisa do Docker rodando na máquina (ou no runner de CI, que já vem com Docker). De propósito não usa o provider `InMemory` do EF Core: `InMemory` não gera SQL de verdade, então não serve pra contar query.

Duas camadas de teste:

- **Unitário** (`Auth/`, `QueryCount/`): chama repository/service direto, contra o `AppDbContext`.
- **Integração** (`Integration/`): sobe a API inteira via `WebApplicationFactory<Program>`. Roteamento, model binding, autenticação e rate limiting reais, batendo por HTTP.

### Pegadinha de configuração que os testes de integração pegaram

`Program.cs` usa top-level statements. Se você ler `builder.Configuration.GetConnectionString(...)` numa variável local **antes** de `builder.Build()` e capturar essa variável num closure (`options.UseNpgsql(connectionString)`), o `WebApplicationFactory` dos testes de integração não consegue sobrescrever esse valor. A leitura já aconteceu contra o `appsettings.json` de verdade antes do override de teste ser aplicado. Por isso `ConnectionStrings`, `Jwt` e o health check do Postgres neste projeto são todos resolvidos **dentro** dos delegates de configuração (via `IConfiguration`/`IOptions` injetados), nunca numa variável capturada cedo demais. Vale a pena manter esse padrão ao adicionar novas configurações.

### O guarda de N+1

`Diagnostics/QueryCountInterceptor.cs` conta quantos comandos SQL um `DbContext` executa. O teste `BookRepositoryQueryCountTests` chama `BookRepository.GetAllWithAuthorsAsync()` e garante que isso roda **exatamente 1 query**, não importa quantos livros existam.

Se no futuro alguém remover o `.Include(b => b.Author)` do repository (ou trocar por acesso lazy dentro de um loop), esse teste fica vermelho no CI antes de virar um problema de performance em produção. Sem precisar de profiler pago ou ferramenta externa.

Pra usar o mesmo padrão em outros repositories do seu projeto:

```csharp
var interceptor = new QueryCountInterceptor();
await using var context = CreateContext(interceptor); // registra o interceptor no DbContextOptionsBuilder

interceptor.Reset();
var result = await meuRepository.MetodoQueDeviaSerUmaQueryOnly();

Assert.Equal(1, interceptor.CommandCount);
```

## Adicionando uma migration

```bash
dotnet tool restore
dotnet ef migrations add NomeDaMigration \
  --project src/BackTemplate.Api/BackTemplate.Api.csproj \
  --startup-project src/BackTemplate.Api/BackTemplate.Api.csproj \
  -o Data/Migrations
```

## Configuração

Variáveis de ambiente (sobrescrevem `appsettings.json`, sintaxe `Section__Key`):

| Variável | Descrição |
|---|---|
| `ConnectionStrings__Default` | connection string do Postgres |
| `Jwt__Issuer` / `Jwt__Audience` | issuer/audience do token |
| `Jwt__SigningKey` | chave de assinatura HMAC: **troque o valor default antes de ir pra produção** |
| `Jwt__ExpiryMinutes` | validade do token em minutos |

## Estrutura

```
src/BackTemplate.Api/
  Auth/             registro, login, geração de JWT
  Controllers/      endpoints HTTP
  Data/             DbContext, entidades, migrations
  Diagnostics/       QueryCountInterceptor (guarda de N+1)
  Middleware/        tratamento global de exceção
  RateLimiting/      nomes de política de rate limit
  Repositories/     acesso a dados

tests/BackTemplate.Tests/
  Auth/             testes de registro/login (chamando o service direto)
  Integration/       testes batendo na API real via WebApplicationFactory
  QueryCount/       teste do guarda de N+1
  TestFixtures/      fixture do Postgres via Testcontainers
```

## Fora de escopo (por enquanto)

Front-end, deploy automatizado em nuvem, publish de imagem Docker em registry.

## Aviso conhecido

`dotnet build`/`dotnet test` mostra um warning `NU1903` sobre uma vulnerabilidade no pacote `SSH.NET`. É dependência transitiva do Testcontainers (usada só nos testes, nunca publicada na imagem de produção). Sem risco pra API rodando.

---

## Perguntas frequentes

**Qual banco de dados ele usa?**
Postgres via Npgsql e EF Core 10. O `docker compose up -d` sobe a API e o Postgres juntos, e a API aplica as migrations na inicialização.

**O que é o guarda de N+1?**
Um teste que conta os comandos SQL que um `DbContext` executa e verifica que `GetAllWithAuthorsAsync()` roda exatamente uma query. Se alguém remover o `.Include`, o teste falha no CI. Veja [O guarda de N+1](#o-guarda-de-n1).

**Por que Testcontainers em vez do provider InMemory do EF Core?**
O `InMemory` não gera SQL de verdade, então não dá pra contar queries.

**Está pronto para produção?**
Ele entrega auth, rate limiting, um health check de verdade e uma imagem Docker sem root. Antes de fazer deploy, troque o `Jwt__SigningKey`. Front-end, deploy automático em cloud e publicar a imagem num registry estão fora de escopo.

## A família de templates

Mesma ideia, stack diferente. Clona um e já sai construindo.

| Camada | Starter |
|---|---|
| Backend | [Spring Boot](https://github.com/obrenoalvim/back-template-spring) · [Go](https://github.com/obrenoalvim/back-template-go) · [FastAPI](https://github.com/obrenoalvim/back-template-fastapi) · [NestJS](https://github.com/obrenoalvim/back-template-nest) · [Laravel](https://github.com/obrenoalvim/back-template-laravel) · **ASP.NET Core (este repo)** |
| Frontend | [Angular](https://github.com/obrenoalvim/front-template-angular) · [React](https://github.com/obrenoalvim/front-template-react) · [SvelteKit](https://github.com/obrenoalvim/front-template-sveltekit) · [Vue](https://github.com/obrenoalvim/front-template-vue) |
| Full-stack | [Next.js](https://github.com/obrenoalvim/next-template) |

## Licença

[MIT](LICENSE)

---

<div align="center">

Se isso te poupou um dia de setup, uma ⭐ ajuda outras pessoas desenvolvedoras a encontrá-lo.

<sub>**Tópicos:** aspnetcore · dotnet · csharp · web-api · entity-framework-core · jwt-authentication · postgresql · docker · testcontainers · boilerplate · starter-kit · backend-template</sub>

</div>
