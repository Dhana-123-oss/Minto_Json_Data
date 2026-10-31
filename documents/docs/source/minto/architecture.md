# Minto architecture

Minto is a modular monolith: one deployable API with explicit business boundaries. This keeps the MVP easy to run while preserving seams for future service extraction.

## Boundaries

| Boundary | Responsibility |
|---|---|
| `Identity` | Development login and JWT creation; Keycloak validation seam |
| `Commerce` | Product catalogue, cart, checkout, orders |
| `Operations` | Picker queue, rider queue, admin summary, guarded transitions |
| `Data` | EF Core context and database access |
| `Domain` | Entities, inventory, order statuses, order invariants |
| `Features` | CQRS handlers and SignalR tracking hub |
| `Infrastructure` | Transactional outbox publisher and messaging integration |

## Request path

```text
React/Vite → ASP.NET Core endpoint → authorization policy
                         ↓
                feature handler/service
                         ↓
                 domain + EF Core data
                         ↓
               outbox / SignalR integration
```

Checkout and inventory reservation are committed together. PostgreSQL is the production store, while RabbitMQ, Valkey, and Keycloak are optional integrations in Development.
