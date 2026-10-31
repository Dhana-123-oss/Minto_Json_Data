# Minto documentation map

This is the entry point for the Minto grocery ordering and delivery application. Read the guides in order when setting up the project or changing a workflow.

| Minto capability | What it covers | Guide |
|---|---|---|
| Product overview | Users, roles, business flow, current scope | [Product overview](../documents/docs/source/minto/product-overview.md) |
| Run locally | API, web app, development data, infrastructure | [Backend run locally](../backend/docs/guides/00-run-locally.md), [Frontend run locally](../frontend/docs/guides/00-run-locally.md) |
| Architecture | Modular monolith boundaries and dependencies | [Architecture](../documents/docs/source/minto/architecture.md) |
| Customer ordering | Catalogue, cart, checkout, inventory reservation | [Customer workflow](../documents/docs/source/minto/customer-workflow.md) |
| Picker and rider operations | Queues, transitions, pickup verification, delivery | [Operations workflow](../documents/docs/source/minto/operations.md) |
| API | Routes, authentication, request rules, state machine | [API contract](../documents/docs/source/minto/api-contract.md) |
| Database | Entities, relationships, indexes, initial schema | [Data model](../documents/docs/source/minto/data-model.md), [SQL schema](../documents/docs/source/minto-initial-schema.sql) |
| Add backend behavior | Feature slices, endpoints, tests | [Add a backend feature](../backend/docs/guides/01-add-a-backend-feature.md) |
| Add frontend behavior | Screens, API calls, role-aware UI, testing | [Add a frontend feature](../frontend/docs/guides/01-add-a-frontend-feature.md) |
| Production readiness | Configuration, integrations, known follow-up work | [Deployment and operations](../documents/docs/source/minto/deployment-and-operations.md) |

## Repository layout

```text
MintoNew/
├── backend/          ASP.NET Core API, domain workflow, persistence, tests
├── frontend/         React/Vite customer and operations application
├── documents/        Product, architecture, API, operations, and schema docs
└── docs/             Workspace documentation map
```

The source of truth for an implemented behavior is the code under `backend/src`, `backend/tests`, and `frontend/apps/minto`. If a guide and the code disagree, update the guide in the same change.
