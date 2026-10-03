# XAUUSD AI Web

Next.js frontend for the XAUUSD AI Trading Intelligence System. Phase 1 provides
the application shell, typed API transport, and a live API status check only.

From this directory:

```powershell
npm ci
npm run dev
```

The frontend runs at `http://localhost:3001` and reads `NEXT_PUBLIC_API_URL`
from the environment, defaulting to `http://localhost:5081` for local development.
