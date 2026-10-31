# Minto API contract

Base path: `/api/v1`.

## Identity

`POST /identity/login` returns a development access token for the seeded accounts. Send it as `Authorization: Bearer <token>` on protected routes.

## Commerce routes

- `GET /commerce/products` — active product catalogue.
- `GET /commerce/cart` — current customer's cart.
- `PUT /commerce/cart/items/{productId}` — add or replace quantity.
- `DELETE /commerce/cart/items/{productId}` — remove an item.
- `POST /commerce/orders` — create an order; send an `Idempotency-Key` header.
- `GET /commerce/orders` — current customer's orders.
- `GET /commerce/orders/{id}` — order details.
- `POST /commerce/orders/{id}/transition` — guarded state change.

## Operations routes

- `GET /operations/picker/queue`
- `GET /operations/rider/queue`
- `GET /operations/admin/summary`
- `POST /operations/picker/orders/{id}/transition`
- `POST /operations/rider/orders/{id}/transition`

The exact request and response shapes are defined in the corresponding contracts under `backend/src/Minto.Api` and exposed through Swagger in Development.

The order state machine is `Placed → Picking → Picked → Packed → ReadyForPickup → Assigned → Accepted → OutForDelivery → Delivered`. Invalid transitions fail closed and operational routes are role guarded.
