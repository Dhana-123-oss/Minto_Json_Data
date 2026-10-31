# Run the Minto web app locally

## Prerequisites

- Node.js 20+
- A running Minto API for live data

## Install and start

From `MintoNew`:

```powershell
cd frontend/apps/minto
npm install
npm run dev
```

Open <http://localhost:5173>. The app uses `http://localhost:5080` by default. Set `VITE_API_URL` in `.env` when the API is hosted elsewhere.

## Build

```powershell
npm run build
```

## Sign in

Use one of the seeded accounts documented in [Product overview](../../../documents/docs/source/minto/product-overview.md). The selected role determines which navigation and operational views are available.

## Main screens

- Customer: shop, cart, and orders.
- Picker: picker queue and next valid picking action.
- Rider: rider queue, pickup verification, and delivery action.
- Admin: summary metrics plus picker and rider access.
