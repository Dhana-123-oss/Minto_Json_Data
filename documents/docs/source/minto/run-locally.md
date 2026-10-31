# Run Minto locally

Requirements: .NET 10 SDK, Node 20+, and Docker Desktop for infrastructure.

```powershell
dotnet run --project backend/src/Minto.Api --urls http://localhost:5080
cd frontend/apps/minto
npm install
npm run dev
```

Development uses EF Core InMemory and seeded users. Run infrastructure with `docker compose -f backend/compose/docker-compose.yml up -d postgres rabbitmq valkey keycloak` when needed.

Verify with `dotnet build backend/src/Minto.Api/Minto.Api.csproj`, `dotnet test backend/tests/Minto.Api.Tests/Minto.Api.Tests.csproj`, and `npm run build` in `frontend/apps/minto`.
