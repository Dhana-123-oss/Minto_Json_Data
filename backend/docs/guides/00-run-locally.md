# Run the Minto backend locally

## Prerequisites

- .NET 10 SDK
- Docker Desktop only when using Compose infrastructure
- PowerShell or another shell

## Start the API

From `MintoNew`:

```powershell
dotnet run --project backend/src/Minto.Api --urls http://localhost:5080
```

Development uses EF Core InMemory and seeds products, inventory, and development users automatically. Open Swagger at <http://localhost:5080/swagger> and health at <http://localhost:5080/health>.

## Start optional infrastructure

```powershell
docker compose -f backend/compose/docker-compose.yml up -d postgres rabbitmq valkey keycloak
```

RabbitMQ and Keycloak become active integrations only when their configuration values are provided. Starting the containers alone does not switch the API away from its development adapter.

## Validate the backend

```powershell
dotnet build backend/src/Minto.Api/Minto.Api.csproj
dotnet test backend/tests/Minto.Api.Tests/Minto.Api.Tests.csproj
```

## Troubleshooting

| Symptom | Check |
|---|---|
| API starts but no products appear | Confirm the Development environment and seeded startup completed. |
| `401 Unauthorized` | Log in through `POST /api/v1/identity/login` and send the returned bearer token. |
| Transition rejected | The requested status is not the next valid state, or the role/pickup code is invalid. |
| PostgreSQL connection failure | Development should use InMemory; outside Development verify `ConnectionStrings:Minto`. |
