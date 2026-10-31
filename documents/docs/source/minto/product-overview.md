# Minto product overview

## Purpose

Minto is a local-grocery ordering and delivery platform. It connects a customer storefront with the store picker and delivery rider workflows required to fulfil an order.

The product promise is simple: customers select fresh daily essentials, the store prepares the order, and a rider verifies pickup before delivering it.

## Users and permissions

| Role | Main responsibility | Application surface |
|---|---|---|
| Customer | Browse products, maintain a cart, place COD orders, view order history | Customer storefront |
| Picker | Pick, pack, and mark orders ready for pickup | Picker console |
| Rider | Accept assignments, verify pickup, and complete delivery | Rider console |
| Admin | View operational metrics and access picker/rider consoles | Admin console |

Development users are seeded with the password `Minto@123`:

```text
customer@minto.local
picker@minto.local
rider@minto.local
admin@minto.local
```

## End-to-end business flow

```text
Customer browses catalogue
        ↓
Customer adds products to server-side cart
        ↓
Checkout validates availability and reserves inventory
        ↓
Order is placed with COD payment
        ↓
Picker picks, packs, and marks order ready
        ↓
Rider accepts assignment and verifies pickup code
        ↓
Rider delivers order and completes COD
```

## Current MVP scope

Implemented:

- Product catalogue and seeded grocery data
- Persistent cart operations
- Idempotent order creation
- Transactional inventory reservation
- Guarded order state transitions
- Picker and rider queues
- Pickup-code verification
- COD completion on delivery
- Role-based development login
- Admin operational summary
- Swagger, health check, SignalR hub, and outbox publisher seam

Not yet a complete production feature:

- Online payment gateway integration
- Customer address management
- Full product search and category filtering in the UI
- Real notification delivery
- Automated database migration pipeline
- End-to-end Testcontainers/browser test suite

These are roadmap items, not assumptions about the current implementation.
