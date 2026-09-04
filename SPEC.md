# back-template-dotnet — spec

Backend starter template em C#/.NET, mesmo papel que back-template-spring, back-template-go, back-template-fastapi, back-template-nest e back-template-laravel: clonar e já ter login, segurança e testes prontos pra começar a construir a API de verdade.

## Objetivo

Preencher a lacuna .NET na coleção de back-templates, com um diferencial: guarda-corpo automatizado contra queries N+1 no EF Core, rodando como teste normal no CI — sem ferramenta externa, sem profiler pago.

## Stack

- .NET 10, ASP.NET Core Web API
- EF Core 10 + Npgsql (PostgreSQL)
- Autenticação JWT (registro/login, hash de senha com BCrypt.Net-Next)
- xUnit + Testcontainers.PostgreSql (banco real nos testes, não InMemory — InMemory não gera SQL real, não serve pra contar query)
- Docker + docker-compose (API + Postgres)
- GitHub Actions CI (restore, build, test em toda alteração)

## Domínio de exemplo

Author / Book (clássico pra demonstrar relação 1-N) + User (autenticação).

## Guarda N+1 — como funciona

1. `QueryCountInterceptor : DbCommandInterceptor` conta comandos SQL executados por um DbContext durante um bloco de teste.
2. Repository correto usa `.Include()` pra trazer Book+Author numa query só.
3. Teste chama o repository, assert `interceptor.CommandCount == 1`.
4. Se alguém no futuro remover o `.Include()` (ou trocar por lazy loading), a contagem sobe pra 1+N e o teste quebra no CI — é o alerta.

## Estrutura

```
back-template-dotnet/
  src/BackTemplate.Api/          (Program.cs, Controllers, Data, Repositories, Services, Diagnostics)
  tests/BackTemplate.Tests/      (fixtures Testcontainers, testes de auth, teste de contagem de query)
  .github/workflows/ci.yml
  Dockerfile
  docker-compose.yml
  README.md
```

## Fora de escopo (por enquanto)

Front-end, deploy automatizado em nuvem, publish de imagem Docker em registry (exige credencial que não tenho).

## Autoria

Repo público em github.com/obrenoalvim, commits só no nome do usuário, sem trailer de coautoria.
