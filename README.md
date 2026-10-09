# RealEstateCRM

ASP.NET Core 9 + EF Core CRM for real estate brokers: contacts, properties, leads, tasks, interactions,
site visits, brokerage/GST tracking, and EMI / stamp-duty calculators. Static JS frontend served from `wwwroot`.

## What changed in the hardening pass

| Area | Before | Now |
|---|---|---|
| API access | Open to anyone | JWT bearer auth on every endpoint (`/api/auth/login` and `/healthz` are public) |
| Passwords | Unsalted SHA-256 | PBKDF2-SHA256 (600k iterations, per-user salt); old hashes upgrade on next login |
| Multi-tenancy | None (all data shared) | `CompanyId` on every business table + EF global query filter + auto-stamping on insert |
| Cross-tenant references | Possible | Rejected with 400 (`EnsureExistsAsync`) |
| Seeded `Test/Test/Test123` | Always, including production | Development only; production uses `Bootstrap__*` variables |
| Brute force | None | 10 logins/min/IP rate limit + 5-strike, 15-minute account lockout |
| Validation | Attributes absent from DTOs | Range / length / enum checks on every DTO (invalid input returns 400) |
| Lists | Unbounded | `?page=&pageSize=` (default 500, max 1000), total in `X-Total-Count` header |
| Errors | try/catch + plain-text 500s | One global handler returning RFC 7807 ProblemDetails |
| Roles | None | `Owner` and `Agent`; only Owners can create users |
| Database | SQLite only | SQLite (dev) or PostgreSQL (`DATABASE_URL`) |
| Tests / CI | None | xUnit unit + integration tests, GitHub Actions workflow |

## Run locally

```bash
cd RealEstateCRM
dotnet run
```

In Development the app creates company `Test`, user `Test`, password `Test@12345` (and uses a built-in dev signing key).
Open http://localhost:5133 and sign in.

## Configuration (environment variables)

| Variable | Purpose |
|---|---|
| `Jwt__Key` | **Required outside Development.** Random secret, 32+ characters. |
| `DATABASE_URL` | PostgreSQL URL (`postgres://user:pass@host:5432/db`). Render sets this for you. When unset, SQLite is used. |
| `Database__Provider` / `ConnectionStrings__DefaultConnection` | Explicit provider (`Sqlite` or `Postgres`) and connection string. |
| `Bootstrap__CompanyCode`, `Bootstrap__CompanyName`, `Bootstrap__AdminUsername`, `Bootstrap__AdminPassword` | Creates the first company and its Owner at startup (password 10+ chars). Existing users are never modified. |
| `PORT` | Listening port (Render sets this). |

Create more users as an Owner: `POST /api/auth/users` with `{ "username", "password", "role": "Agent" | "Owner" }`.
Change your password: `POST /api/auth/change-password`.

## Deploy on Render

1. Create a PostgreSQL instance and a Docker web service from this repo (root `Dockerfile`).
2. Set `DATABASE_URL` (internal URL), `Jwt__Key`, and the four `Bootstrap__*` variables.
3. Add a health check on `/healthz`.

## Database migrations (do this once before storing real data)

The old SQLite migrations were removed because the schema changed (tenancy columns, roles, lockout) and they were
SQLite-specific. Until you create new ones, the app builds the schema with `EnsureCreated` and logs a warning.
Pick the provider you will run in production, then generate the initial migration and commit it:

```bash
# PostgreSQL example
export Database__Provider=Postgres
export ConnectionStrings__DefaultConnection="Host=localhost;Database=crm;Username=postgres;Password=postgres"
dotnet tool install --global dotnet-ef
cd RealEstateCRM
dotnet ef migrations add InitialCreate
```

From then on the app applies migrations automatically at startup. Do not mix providers: migrations are provider-specific.
(A database first created by `EnsureCreated` must be dropped before adopting migrations.)

## Tests

```bash
dotnet test
```

Covers password hashing (including legacy upgrade), EMI / stamp-duty maths, database URL parsing, tenant isolation at the
DbContext level, and API integration (401 without a token, validation, paging header, cross-company access, owner-only user creation).

## Known limitations / next steps

- Agents currently see all data within their own company (no per-agent lead ownership yet).
- Entities are still returned directly from the API (tenant `CompanyId` is hidden from JSON); response DTOs are the next cleanup.
- A disabled user keeps a valid token until it expires (default 8 hours).
- No Content-Security-Policy yet: `index.html` uses inline event handlers.
