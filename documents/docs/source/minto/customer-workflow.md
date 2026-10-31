# Customer workflow

## Catalogue

The customer screen loads products from `GET /api/v1/commerce/products`. Each product has an id, name, category, price, and active flag. The current UI presents the seeded products as grocery cards.

## Cart

The cart is server-side and belongs to the authenticated customer.

- Add or replace a quantity with `PUT /api/v1/commerce/cart/items/{productId}`.
- Remove an item with `DELETE /api/v1/commerce/cart/items/{productId}`.
- Load the cart with `GET /api/v1/commerce/cart`.

The browser does not treat local state as the source of truth. After every cart mutation, the API response becomes the displayed cart.

## Checkout

Checkout calls `POST /api/v1/commerce/orders` with an idempotency key, delivery address, payment method, and order lines. The backend:

1. Validates the request and line quantities.
2. Resolves the current product prices.
3. Verifies available inventory.
4. Reserves inventory and creates the order in one transaction.
5. Clears the server-side cart only after the order is saved.

The MVP uses `COD`. Payment remains incomplete until the order reaches `Delivered`.

## Order history

Customers load their orders from `GET /api/v1/commerce/orders` and can open a specific order with `GET /api/v1/commerce/orders/{id}`. The UI displays the order status, lines, total, and payment state where available.

## Failure behavior

The UI shows a retry state when catalogue, cart, or order loading fails. A rejected checkout leaves the cart available and reports that the order was not submitted.
