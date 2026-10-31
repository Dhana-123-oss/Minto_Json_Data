# Minto data model

## Core entities

| Entity | Purpose |
|---|---|
| `Product` | Active grocery item and current price |
| `InventoryItem` | Available and reserved quantity for a product |
| `Cart` / `CartLine` | Customer's current server-side basket |
| `Order` / `OrderLine` | Immutable checkout snapshot and fulfilment state |
| `OutboxMessage` | Durable integration event waiting to be published |

An order line stores `ProductName` and `UnitPrice` at checkout so later catalogue changes do not rewrite order history.

## Transaction boundary

Order creation and inventory reservation share one database transaction. The transaction must not report success if any requested quantity is unavailable. The cart is cleared only after the order has been persisted successfully.

## Order status

```text
Placed → Picking → Picked → Packed → ReadyForPickup
                                      ↓
Assigned → Accepted → OutForDelivery → Delivered
```

`Cancelled` exists in the domain model but is not part of the current happy-path UI transition map.

## Persistence providers

- Development: EF Core InMemory with seeded data.
- Production target: PostgreSQL using the configured `Minto` connection string.
- Reference schema: `documents/docs/source/minto-initial-schema.sql`.
