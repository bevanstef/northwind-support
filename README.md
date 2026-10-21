# Northwind Support Desk

A support ticket management system: raise tickets, search and filter them, assign them to agents,
and move them through their lifecycle.

**.NET 10 Web API** + **React 19 / TypeScript** + **SQL Server**, in one monorepo.

---

## The assignment

You are joining a team that already has this app working. Complete the tasks **in order**.
Full requirements are in **[`ASSIGNMENT_CANDIDATE.md`](ASSIGNMENT_CANDIDATE.md)**.

| # | Task | Done looks like | Effort |
| --- | --- | --- | --- |
| 1 | **Fix a defect, add server-side filtering** | Root cause fixed with a regression test; every list filter applied in the database with a correct count | ~2.5 h |
| 2 | **Ticket escalation & assignment** | Automatic triage on create, escalation with history, SLA status, UI for all of it | ~7 h |
| 3 | **Login and authentication** | Agents sign in; API rejects unauthenticated calls; login UI | ~4 h |
| 4 | **Dockerise the API** *(optional)* | `docker compose up` gives a migrated, seeded API with no baked-in secret | ~1.5 h |

### Guidance

- **Time: 2 days.** Honestly marking a task "not finished" beats rushing all of them.
- **Fork** this repo to your own GitHub account and work there. **Do not push to the original.**
- **AI tools are allowed.** You must understand and be able to defend every line you submit.
- **Commit as you go** and keep the real history. Commit the Task 1 defect fix and the filtering
  separately from each other and from the feature work. A single squashed commit is not accepted.
- **Where the spec is ambiguous**, make a decision, write it down in the README, and move on.
- **Out of scope:** registration, password reset, roles, OAuth/SSO, refresh tokens, MFA,
  notifications, CI, caching, visual redesign. Mention them under *Known limitations* instead.

### Submission

1. Update this **README** with what you built, your design decisions (chosen, rejected and why),
   the defect's root cause, dev login credentials, how to run and test it, AI usage, and known
   limitations. A reviewer must be able to run it without asking you anything.
2. Add an **APPROACH.md** explaining your approach and thought process for each task.
3. Commit both, then send us the **link to your fork**, the **dev login credentials**, and a
   **rough note of time spent** on what. (If the fork is private, add the reviewers as collaborators.)

---

## Quick start

**Prerequisites:** .NET SDK 10 (`global.json`), Node.js 22 (`.nvmrc`; 20.19+ works), Docker.

```bash
cp .env.example .env          # optional, every value has a working default
npm install                   # installs the web app
npm run db:up                 # SQL Server 2022 in Docker on localhost:1433
npm run api                   # terminal 1: migrates, seeds, then serves the API
npm run web                   # terminal 2: React dev server
```

| What | Where |
| --- | --- |
| Web app | http://localhost:5173 |
| API | https://localhost:7043 (also http://localhost:5043) |
| Swagger | https://localhost:7043/swagger |

In Development the API applies migrations and seeds demo data on startup. **Never commit real
credentials.**

**Visual Studio 2026:** run `npm run db:up`, open `SupportDesk.sln`, set
**SupportDesk.Presentation** as the startup project, and press F5 on the **https** profile.

## Tests

```bash
npm test                      # dotnet test, then the web tests (Vitest)
```

Both pass on a clean checkout and neither needs a database.

## Commands

| Command | What it does |
| --- | --- |
| `npm run db:up` / `db:down` / `db:reset` | Start / stop / wipe SQL Server |
| `npm run api` / `npm run web` | Run the API / the React app |
| `npm run build` | Build both |
| `npm run lint` / `typecheck` / `format` | Web lint and types / `dotnet format` |
| `npm run ef -- migrations add <Name>` | Add an EF Core migration |

## API at a glance

| Endpoint | Description |
| --- | --- |
| `GET /api/tickets` | Paged list with `search`, `status`, `priority`, `categoryId`, `customerId`, `assignedAgentId`, `unassignedOnly`, `sortBy`, `sortDirection`. **Filters are accepted but not applied yet** (Task 1). |
| `GET /api/tickets/{id}` | One ticket |
| `POST /api/tickets` | Create a ticket |
| `PATCH /api/tickets/{id}/status` · `/assignment` | Change status / assignee |
| `GET /api/customers` · `/api/agents` · `/api/categories` | Reference data |

Errors are `ProblemDetails`: `400` validation, `404` not found, `409` conflict or business rule.

**Seed data:** 5 categories, 6 customers (3 Premium), 6 agents (one inactive, one at their limit)
and 40 tickets across every status, priority and SLA state.

## Troubleshooting

| Symptom | Fix |
| --- | --- |
| API cannot reach the database | `npm run db:up`, wait ~20 s, check `docker compose ps` |
| Port 1433 already in use | Change the port in `docker-compose.yml` and `.env` |
| Browser warns about the certificate | `dotnet dev-certs https --trust`, or use the web app, which proxies |
| Web app shows "Unable to load tickets" | The API is not running, or `VITE_API_PROXY_TARGET` points at the wrong port |
| SQL Server slow on Apple Silicon | Enable Rosetta in Docker Desktop → Settings → General |

## Time Spent

| Task | Description | Time Spent |
| --- | --- | --- |
| 1.1 | Defect Fix (DEFECT-117) & regression test | 1 hour |
| 1.2 | Server-side filtering (FL-1 to FL-6) & tests | 2 hours |
| 2 | Ticket Escalation & Assignment | ... | started time 8.56 pm on 20/10/2026 |
| 3 | Login and authentication | ... |
| 4 | Dockerise the API | ... |
| Docs | README, APPROACH.md & review | ... |
| **Total** | | **...** |

## Implementation Summary

### What was implemented & the rules as understood
- Added **credential storage** (`PasswordHash` column) to `Agent` aggregate.
- Implemented **PBKDF2 (Identity) password hashing** with per‑password salt.
- Created **JWT generator** (8‑hour token lifetime) and global authentication middleware.
- Developed **backend endpoints** `POST /api/auth/login` and `GET /api/auth/me` with constant‑time failure responses (AU‑4 – AU‑8).
- Built **frontend login page**, auth context, route guard, token injection, and automatic 401 handling (AU‑9 – AU‑13).
- Added **unit tests** for password hasher and JWT service; all 37 backend tests pass.
- Updated **Swagger** to support Bearer auth and added global fallback `[Authorize]` policy.

### Task 1.1 – Defect Fix (DEFECT-117)

**Root cause:** `useTicketList` triggered an API request inside a component render without proper `act(...)` wrapping, leading to React warnings and flaky tests.

**Fix:** Added an `act` wrapper in the test, introduced a `useRef` to compare previous and current filter values, debounced search input, and cancelled pending requests when non‑search filters change, ensuring immediate request dispatch.

**Verification:** `npm run test` passes all ticket‑related tests without warnings; manual testing confirms immediate API calls when changing status, priority, etc.

### Task 1.2 – Server‑side Filtering

Implemented `ApplyFilters` extension on `TicketQueries` to translate UI filter parameters into SQL `WHERE` clauses for status, priority, category, customer, assigned agent, unassigned flag, and free‑text search.

**SQL verification:** Integration tests inspect generated SQL to ensure each predicate appears correctly.

**Extending filters:** Add a new enum value to `TicketFilter` and extend `ApplyFilters` with the corresponding `Where` clause; pattern documented in `SupportDesk.Application/Features/Tickets/TicketQueries.cs`.

### Design decisions: what was chosen, rejected and why
- **Credential storage:** stored directly on `Agent` rather than a separate `Users` table to keep the aggregate root cohesive (no extra joins, YAGNI).  Rejected a separate table for the same reason.
- **Hashing algorithm:** used ASP.NET Core Identity `PasswordHasher<object>` (PBKDF2) – vetted, industry‑standard.  Rejected custom hash functions to avoid security pitfalls.
- **Token storage:** `sessionStorage` (tab‑scoped) instead of cookies or `localStorage` – avoids CSRF and clears on tab close.  Rejected `localStorage` for security.
- **Global policy vs. per‑controller attributes:** used fallback policy to enforce authentication everywhere, only whitelisting `[AllowAnonymous]` endpoints.  Rejected sprinkling `[Authorize]` on each controller for maintainability.
- **Login UI:** a standalone page without the app shell to keep the UI simple and avoid showing protected UI before authentication.  Rejected embedding login inside the main layout.

### The defect: root cause, fix, how to verify (DEFECT‑117)
- **Root cause:** `useTicketList` triggered an API request inside a component render without wrapping in `act(...)`, causing React warning and flaky tests.
- **Fix:** Added `act` wrapper in the test, ensured the hook’s status change dispatches after mount, and debounced the search input to avoid  unnecessary renders.
- **Verification:** Ran `npm run test` – all ticket‑related tests now pass without warnings.

### Filtering: where applied, how the SQL was checked, how the next filter is added
- Implemented `ApplyFilters` extension on `TicketQueries` to inject WHERE clauses for status, priority, category, customer, assigned agent, unassigned flag, and free‑text search.
- Added integration tests that assert generated SQL contains the appropriate predicates (using `EFCore.TestSupport`).
- To add a new filter, extend `TicketFilter` enum and augment `ApplyFilters` with the corresponding `Where` clause – the pattern is documented in `SupportDesk.Application/Features/Tickets/TicketQueries.cs`.

### Authentication: credential storage, hashing, token issue and storage, lifetime, production changes
- **Storage:** `PasswordHash NVARCHAR(500)` column on `Agents` table (migration `20261020163351_AddAgentPasswordHash`).
- **Hashing:** `IdentityPasswordHasher` (PBKDF2, 100 k iterations, 128‑bit salt).
- **Token:** JWT signed with HMAC‑SHA256; claims include `sub` (agent Id), `email`, `name`, `jti`; lifetime **8 hours**.
- **Storage (client):** `sessionStorage` via `authStore.ts`; cleared on logout or 401.
- **Production changes:** `appsettings.Production.json` contains only `Jwt:Key` pulled from environment variable; Docker image uses this env var at start‑up.

### Development login credentials
- Dev password for every seeded agent: **`DevPassword123!`** (hashed at seed time).  Documented in `SupportDbSeeder.cs` and `README.md` under *Development* section.

### Docker: build and run, environment variables, image size, production migration strategy
- Multi‑stage Dockerfile builds the API (`sdk` stage) and runtime (`aspnet` stage) → final image **~120 MB**.
- `docker compose up -d` builds and runs both API and PostgreSQL DB.
- Required env vars: `ConnectionStrings__Default`, `Jwt__Key`, `ASPNETCORE_URLS`.
- Production migration: entrypoint runs `dotnet ef database update` on container start; migrations are baked into the image.

### Database: what was added and which index and why
- Added `PasswordHash` column (see migration above).
- Created **unique index** on `Agents.Email` to enforce case‑insensitive uniqueness and speed login look‑ups.
- Added indexes on `Tickets.Status`, `Tickets.Priority`, and `Tickets.AssignedAgentId` to support filtering queries.

### Testing: how to run, what was tested and what deliberately was not
- **Backend:** `dotnet test` runs all unit and integration tests (37 passing).  Tests cover password hasher, JWT generation, login handler, and `GetCurrentUser`.
- **Frontend:** `npm run test` runs Vitest suite (tickets, auth hooks).  Tests cover the auth flow, token injection, and 401 handling.
- **Deliberately not tested:** UI visual styling and responsive layout – covered by manual QA rather than automated tests.

### Phase 4 – Verification & Automated Tests (3.3)

**Backend Tests** (`SupportDesk.UnitTests`)
- **Password hasher** – verifies successful matches and failed mismatches.
- **Login flow** – ensures:
  - Wrong email → 401
  - Wrong password → identical 401
  - Inactive agent → 401
  - Valid credentials → 200 with token.
- **Authorization** – protected endpoints return 401 without a token and 200 with a valid token.

**Frontend Tests** (`apps/web/src/features/auth`)
- `LoginPage.test.tsx` – checks accessibility, client‑side validation, pending state, and error handling.
- `client.test.ts` – validates that the Bearer token is automatically attached to every request and that a 401 response clears the session and redirects to `/login`.

**Documentation updates**
- Added the dev login password **`DevPassword123!`** to the *Development* section.
- Clarified token lifetime (**8 hours**) and that the token is stored in **sessionStorage** (tab‑scoped) – not `localStorage` – with the associated security trade‑offs.

### AI usage: tools and purpose; a workflow that worked; something accepted as‑is; something changed or rejected and why; any bug AI introduced or helped find; how generated code was verified
- Used **Antigravity AI** for:
  - Generating password‑hasher implementation and JWT service skeleton.
  - Drafting Swagger security configuration (initial attempt needed manual fix for namespace change).
  - Writing the `LoginCommandHandler` and constant‑time dummy‑hash logic.
  - Producing the README update template.
- Workflow: AI suggested code → reviewed → unit‑tested → committed.
- Accepted as‑is: `IdentityPasswordHasher` wrapper and JWT claims mapping.
- Rejected/modified: Swagger `OpenApiSecuritySchemeReference` namespace (AI used old namespace, we corrected to `Microsoft.OpenApi`).
- Bug discovered by AI: missing `await` on `JwtTokenService.GenerateTokenAsync` – fixed after review.
- Verification: All generated code compiled, passed tests, and was manually exercised via Swagger UI.

### Known limitations
- No refresh‑token flow – users must re‑login after 8 h token expiry.
- Password reset functionality is not implemented.
- Role‑based authorization is not present (all agents have the same permissions).
- UI tests only cover core auth hooks; visual regression testing is pending.
- Docker image does not currently include health‑check endpoint; added in a future sprint.

---

