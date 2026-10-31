# Add a frontend feature

The current web app is a small React/Vite shell in `frontend/apps/minto`. Keep customer and operational behavior aligned with the API contract.

## Add an API-backed screen

1. Add or update the TypeScript contract near the consuming component.
2. Call the API through the shared `api` helper so the stored bearer token is sent.
3. Add loading, empty, error, and retry states.
4. Refresh server-backed state after a mutation.
5. Keep role navigation consistent with the backend policy.
6. Build the app before handing off the change.

## Checkout-specific rules

- Generate a new `Idempotency-Key` for every checkout attempt.
- Send the current server cart lines, not stale browser state.
- Do not clear the cart until the order request succeeds.
- Show COD as incomplete until the order is delivered.

## UI behavior

Use the existing green/orange Minto visual language, responsive layouts, and explicit feedback for rejected state transitions. A disabled action should communicate busy state rather than allowing duplicate requests.
