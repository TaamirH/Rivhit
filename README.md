# Rivhit - Attendance Clock (React + ASP.NET Core + SQL Server)

Time clock system that lets employees Clock In / Clock Out.

Key rule: all punches are recorded using authoritative time fetched server-side from `worldtimeapi.org` for `Europe/Zurich`. The client never supplies timestamps.

## Tech

- Frontend: React + TypeScript + Vite (`frontend/`)
- Backend: ASP.NET Core Web API (.NET 8) (`backend/Rivhit.Api/`)
- Database: Microsoft SQL Server (via EF Core migrations)

## Local Development (Quickstart)

Prereqs:
- Node.js (v20+ recommended)
- .NET SDK 8 (installed into your user profile by `dotnet-install.ps1` if needed)
- SQL Server (LocalDB / Developer edition / Docker)

### Frontend

```bash
cd frontend
npm install
npm run dev
```

### Backend

1) Start SQL Server (Docker)

```bash
docker info
docker compose up -d
```

2) Run API

```bash
dotnet --info
dotnet run --project backend/Rivhit.Api
```

If the backend starts on a different port, set `frontend` env:
- create `frontend/.env` with `VITE_API_BASE_URL=http://localhost:<port>`

## Environment Variables

Backend (development):
- `ConnectionStrings__Default`: SQL Server connection string
- `Jwt__Issuer`, `Jwt__Audience`, `Jwt__SigningKey`: JWT config

## Seeded Accounts (Development)

Backend seeds an admin user in development using `backend/Rivhit.Api/appsettings.Development.json`:
- `admin@rivhit.local` / `Admin12345!` (role: Admin)

You can register employees from the UI or via `POST /auth/register`.

## Notes

- If the external time API is unavailable, the backend rejects Clock In/Out (no fallback to local time).
- Admin-only endpoints are separated from employee self-service endpoints.
 - Client never sends timestamps; backend fetches authoritative Zurich time per punch.

## Useful Endpoints

- `POST /auth/register`, `POST /auth/login`, `GET /auth/me`
- `POST /punches/clock-in`, `POST /punches/clock-out` (send `Idempotency-Key` header)
- `GET /me/status`, `GET /me/shifts`
- Admin: `GET /admin/open-shifts`, `POST /admin/shifts/{shiftId}/close`, `GET /admin/reports/shifts.csv`

