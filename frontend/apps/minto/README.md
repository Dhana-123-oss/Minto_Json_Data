# Minto web

React/Vite application for customer shopping and picker, rider, and admin operations.

## Role-based application

- `customer@minto.local`: storefront, JSON catalogue, search, cart, checkout, and order history.
- `picker@minto.local`: picking queue and order state transitions.
- `rider@minto.local`: delivery queue, pickup handoff, and delivery transitions.
- `admin@minto.local`: operational metrics plus access to picker and rider consoles.

All development accounts use password `Minto@123`. The UI starts with local JSON data and automatically uses the API when it is available.

```powershell
npm install
npm run dev
npm run build
```
