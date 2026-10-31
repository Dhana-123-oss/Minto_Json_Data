# Add a backend feature

Minto uses feature slices grouped by business boundary. Keep the route mapping, request contract, and handler close to the operation, and keep persistence in the data layer.

## Choose the boundary

- `Commerce`: products, cart, orders, checkout.
- `Operations`: picker, rider, admin queues and summaries.
- `Identity`: login and token creation.
- `Features`: shared CQRS handlers and tracking.
- `Data`: EF Core context and persistence configuration.
- `Domain`: entities and order-state rules.
- `Infrastructure`: outbox publishing and external integrations.

## Implementation checklist

1. Add the request/response contract in the relevant feature folder.
2. Add the handler or service with the domain validation it needs.
3. Map the endpoint in the boundary's endpoint class.
4. Apply the correct authorization policy.
5. Update the API contract documentation.
6. Add a focused test for success and rejection behavior.
7. Run `dotnet build` and `dotnet test`.

## Rules for order changes

Never update an order status by assigning an arbitrary enum value. Route every change through the transition operation so role checks, valid-next-state checks, pickup verification, rider assignment, and COD completion remain consistent.

## Response discipline

Return API contracts rather than exposing EF entities directly. Keep pricing and product names on order lines as checkout snapshots. Do not claim payment success before `Delivered`.
