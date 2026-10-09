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
| Roles | None | `Owner` and `Agent`; Owners manage users and see everything, Agents only see leads, tasks, interactions, site visits and brokerage they own (contacts and properties are a shared company directory) |
| API output | Raw database entities | Response DTOs; internal columns (`CompanyId`, `AssignedUserId`) never leave the server |
| Disabled users | Token valid for 8 hours | Checked on every request (30 s cache); disabling a user or changing a password signs them out immediately |
| CSP | None | `script-src 'self'` (no inline scripts); the frontend uses CSP-safe event delegation (`js/events.js`) |
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

## Managing users and assignments (Owner)

- `GET /api/auth/users` list users; `POST /api/auth/users` create; `PUT /api/auth/users/{id}/active` `{ "isActive": false }` disable/enable.
- `PUT /api/leads/{id}/assign` `{ "userId": 12 }` hands a lead to an agent. Related tasks, interactions, site visits and brokerage keep their current owner.
- `POST /api/auth/change-password` returns a fresh token (all other sessions are signed out).

There is no screen for these yet; call them with any HTTP client using the Owner's bearer token.

## Known limitations / next steps

- Assigning a lead does not move its related tasks, interactions, site visits or brokerage rows to the agent.
- `style-src` still allows `'unsafe-inline'` because the markup uses inline `style` attributes (scripts are strict).
- Sign-out checks are cached for 30 seconds per server instance.
- No UI yet for user management or lead assignment.
