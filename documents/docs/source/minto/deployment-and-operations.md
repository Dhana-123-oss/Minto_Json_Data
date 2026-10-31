# Deployment and operations

## Local infrastructure

The Compose file provisions PostgreSQL, RabbitMQ, Valkey, and Keycloak. Development does not require all services: the API can run with EF Core InMemory and the built-in development login.

```powershell
docker compose -f backend/compose/docker-compose.yml up -d postgres rabbitmq valkey keycloak
```

## Configuration switches

| Setting | Effect |
|---|---|
| `ConnectionStrings:Minto` | Selects PostgreSQL outside Development |
| `Jwt:SigningKey` | Signs development JWTs when Keycloak is not configured |
| `Keycloak:Authority` / `Keycloak:Audience` | Enables Keycloak JWT validation |
| `RabbitMq:Host` | Enables the hosted outbox publisher |
| `VITE_API_URL` | Sets the frontend API base URL |

## Health and diagnostics

- Health endpoint: `/health`
- API documentation: `/swagger` in Development
- Real-time tracking endpoint: `/hubs/tracking`
- Build API: `dotnet build backend/src/Minto.Api/Minto.Api.csproj`
- Test API: `dotnet test backend/tests/Minto.Api.Tests/Minto.Api.Tests.csproj`
- Build web: `npm run build` in `frontend/apps/minto`

## Production hardening checklist

- Replace the development JWT signing key.
- Enable Keycloak and validate issuer, audience, HTTPS, and token lifetime.
- Use PostgreSQL migrations rather than `EnsureCreated`.
- Configure RabbitMQ credentials and monitor unpublished outbox messages.
- Add Valkey-backed idempotency and cache policies where required.
- Add API integration, browser, and deployment smoke tests.
- Remove development login accounts and demo credentials.
