# Rivhit - Attendance Clock (React + ASP.NET Core + SQL Server)

Time clock system that lets employees Clock In / Clock Out.

Key rule: all punches are recorded using authoritative time fetched server-side from an external API (`timeapi.io`) for `Europe/Zurich`. The client never supplies timestamps.

## Tech

- Frontend: React + TypeScript + Vite (`frontend/`)
- Backend: ASP.NET Core Web API (.NET 8) (`backend/Rivhit.Api/`)
- Database: Microsoft SQL Server (via EF Core migrations)

## Local Development (Quickstart)

Prereqs:
- Node.js (v20+ recommended)
- .NET SDK 8 (installed into your user profile by `dotnet-install.ps1` if needed)
- SQL Server (Docker recommended for this repo)

### 1) Start SQL Server (Docker)

```bash
docker info
docker compose up -d
```

Defaults used by this repo:
- SQL Server: `localhost:1433`
- User: `sa`
- Password: `YourStrong!Passw0rd123` (see `docker-compose.yml`)

### 2) Run the backend API

The dev profile listens on `http://localhost:5150` and opens Swagger at `http://localhost:5150/swagger`.

```bash
dotnet --info
dotnet run --project backend/Rivhit.Api --launch-profile http
```

### 3) Run the frontend

Create `frontend/.env` (or copy `frontend/.env.example`) and point it to the backend:
- `VITE_API_BASE_URL=http://localhost:5150`

```bash
cd frontend
npm install
npm run dev
```

Vite will print the local URL (typically `http://localhost:5173`).

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
- Dev-only CORS allows any `http(s)://localhost:*` origin so Vite can use any port.

## Useful Endpoints

- `POST /auth/register`, `POST /auth/login`, `GET /auth/me`
- `POST /punches/clock-in`, `POST /punches/clock-out` (send `Idempotency-Key` header)
- `GET /me/status`, `GET /me/shifts`
- Admin: `GET /admin/open-shifts`, `POST /admin/shifts/{shiftId}/close`, `GET /admin/reports/shifts.csv`
- Debug (dev): `GET /debug/time/zurich`

