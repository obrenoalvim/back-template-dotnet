FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY BackTemplateDotnet.slnx ./
COPY src/BackTemplate.Api/BackTemplate.Api.csproj src/BackTemplate.Api/
COPY tests/BackTemplate.Tests/BackTemplate.Tests.csproj tests/BackTemplate.Tests/
RUN dotnet restore src/BackTemplate.Api/BackTemplate.Api.csproj

COPY src/BackTemplate.Api/ src/BackTemplate.Api/
RUN dotnet publish src/BackTemplate.Api/BackTemplate.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "BackTemplate.Api.dll"]
