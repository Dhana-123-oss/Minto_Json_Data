# Feature API

The feature routes live under `/api/v1/features` and require a bearer token from `/api/v1/identity/login`.

They persist operational state for picking checklists and barcode verification, substitutions and approvals, quality checks, pickup handover, delivery proof and GPS locations, KYC review, inventory alerts, complaints/refunds, shifts/attendance, cash settlement, reports, coupons/loyalty, push subscriptions, addresses, wishlist items, reviews, subscriptions, support messages, payments/cancellation, stores, suppliers, purchase orders, stock transfers, rider issues, audit and login history.

They also expose order timelines and assignment/deadline updates, dashboard pending/delayed metrics, user and broadcast notifications, staff/store assignments, leave approval, performance snapshots, and admin exports for orders, refunds, audit, and reports.

Store-management read routes include `GET /features/stores`, `GET /features/suppliers`, `GET /features/purchase-orders`, and `GET /features/stock-transfers`; they require the Admin or Manager role. Workforce read routes include `GET /features/riders` for Admin/Manager rider records and `GET /features/staff/assignments` plus `GET /features/performance/{staffId}` for managed staff. Admin rider suspension uses `POST /features/riders/{id}/status` with `{ "suspended": true|false }`; suspended riders are prevented from going online.

The API is provider-neutral for integrations that require credentials. A production deployment still needs a maps provider, push provider, payment gateway, OTP/SMS provider, object storage for photos/signatures, and an actual barcode/camera client.
