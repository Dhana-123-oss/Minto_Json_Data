# Minto requirements audit

Status meanings: **Implemented** means a persisted API and client path exists; **Partial** means some API/UI exists but a production integration or remaining screen wiring is needed; **Pending** means the capability is not yet complete.

## Implemented or substantially implemented

- Picker queue, order transitions, persisted item checklist, barcode mismatch rejection, persisted quality check on completion, priority/deadline/order assignment fields.
- Picker store selection and online/offline shift availability now resolve against the persisted store API; development data includes a real store branch and rider profile for end-to-end testing.
- Out-of-stock replacement proposal, customer approval, replacement history, complaints and refund approval APIs.
- Quality checks, delivery proof photo/OTP contract, pickup OTP, handover signature/timestamp contract.
- Admin summary/dashboard, order timeline, GPS location persistence, KYC submission/review, inventory CRUD and low-stock/expiry alerts.
- Staff shifts/attendance, cash settlement, reports, coupons, loyalty, push subscription persistence, stores, suppliers, purchase orders, stock transfers.
- Role authorization, audit records, login history, approval/leave history, notifications/broadcasts, data export endpoints.
- Customer ownership and role checks are enforced for payments, complaints, refunds, ratings, product reviews, replacement visibility, picker quality/checklist/handover, and rider/picker order timelines.
- Customer catalogue/cart/checkout/order history API integration, payment-record persistence after server checkout, active-order tracking polling, persisted wishlist, address selection, replacement approval, refund requests, and delivery ratings.
- Mobile OTP request/verify flow for seeded development accounts, including frontend login mode.

## Partial

- Picker profile, store selection, shift controls, performance dashboard, quality/handover screens: backend contracts exist but some screens still use local sample state.
- Admin people, support, inventory, reports and live-order screens: summary, live order list, rider list, complaints, notifications and low-stock data are API-backed; some secondary list/actions still use sample UI defaults when the API has no records.
- Rider queue, online/offline state from both rider entry points, browser GPS, KYC submission, delivery proof, COD settlement, persisted rain/safety issue reporting, and SOS issue reporting are API-backed; real QR/camera, routing, wallet/payout, call/chat and document verification need client/provider integration.
- Customer tracking polling, payment persistence, catalogue/cart/checkout/order history, replacement/refund APIs, addresses, reviews, subscriptions, support and cancellation APIs exist; replacement/refund approval controls and several customer controls are not yet fully wired to their screens.
- Manager catalogue/stock is API-backed and now surfaces low-stock/expiry alerts; the live Store Operations panel binds branches, store open/close, suppliers, purchase orders, stock transfers, shifts, attendance actions and reports. Audit-detail UI remains follow-up work.

## Pending external integrations

- SMS/WhatsApp OTP provider for production delivery of codes.
- Payment gateway and wallet/payout provider.
- Maps/routing provider and production geolocation policies.
- Native camera/barcode/QR scanning and object storage for photos/signatures.
- Push delivery provider (FCM/APNs/Web Push worker).
- Real-time customer/rider chat and support agent service.
- Production identity/KYC/face verification provider.

The API contracts for these integrations are documented in `backend/docs/feature-api.md`; provider credentials and deployment configuration are intentionally not fabricated in development.
