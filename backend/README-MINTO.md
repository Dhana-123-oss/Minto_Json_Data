# Minto API

The Minto API is a modular ASP.NET Core application. Feature slices are grouped by business boundary: `Identity`, `Commerce`, `Operations`, `Data`, `Domain`, `Features`, and `Infrastructure`.

```powershell
dotnet build src/Minto.Api/Minto.Api.csproj
dotnet test tests/Minto.Api.Tests/Minto.Api.Tests.csproj
```

Development uses EF Core InMemory. Production can use PostgreSQL, RabbitMQ outbox publishing, and Keycloak JWT validation.
