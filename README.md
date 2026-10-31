# Minto workspace

Minto is a grocery ordering and last-mile delivery MVP. Customers shop from a local catalogue; pickers prepare orders; riders verify pickup and complete delivery; admins monitor operations.

Start with the [Minto documentation map](docs/minto-map.md) for the product overview, architecture, local setup, API contract, data model, workflows, and development guides.

This is the Minto workspace. It follows the multi-repository layout requested for the project:

| Folder | Responsibility |
| --- | --- |
| `documents` | Minto product, architecture, API, operations, and migration documentation |
| `backend` | ASP.NET Core API, domain workflow, persistence, tests, and infrastructure |
| `frontend` | React/Vite customer, picker, rider, and admin application |

## Run locally

```powershell
docker compose -f backend/compose/docker-compose.yml up -d postgres rabbitmq valkey keycloak
dotnet run --project backend/src/Minto.Api --urls http://localhost:5080
cd frontend/apps/minto; npm install; npm run dev
```

The API uses EF Core InMemory with seeded data in Development. Open `http://localhost:5173`; Swagger is at `http://localhost:5080/swagger`.

Read [documents/docs/source/minto/start-here.md](documents/docs/source/minto/start-here.md) before changing code.
