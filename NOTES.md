## 1.1 Defect Fix (DEFECT-117)

### Root Cause
In `apps/web/src/features/tickets/hooks/useTicketList.ts`, the `useEffect` hook's dependency array only included `[filters.search, filters.page, filters.pageSize]`. When changing dropdown filters (`status`, `priority`, `categoryId`, etc.), React state was updated, but the fetch effect was never triggered. When the user subsequently typed into the search box, `filters.search` changed and triggered the effect, sending the accumulated, stale filter state.

### Fix
1. Compared previous and current filters using a `useRef` to detect exactly what changed.
2. If only `search` changes, debounced the request by 300 ms to prevent per-keystroke API calls.
3. If any non-search filter changes (e.g. `status`, `priority`, or future filters like `slaStatus`), cancelled any pending search timer and dispatched an **immediate** request.
4. Used dynamic key comparison so that adding new filter fields (such as Task 2's `slaStatus`) triggers immediate requests without needing manual wiring in the hook.
5. Added an `activeRequestIdRef` counter to ignore out-of-order responses from stale network requests.

### How to Verify
- **Automated test:** Run `npm run test --workspace apps/web` (see `useTicketList.test.ts: triggers an immediate request when status changes (DEFECT-117)`).
- **Manual verification:** Open `http://localhost:5173`, change the Status dropdown to *In progress*, and inspect the Network tab: a request to `/api/tickets` is sent immediately.

## Task 1.2 – Server-side filtering

**Root cause:** `TicketQueries.GetPagedAsync` was building the query with
`TicketsWithLabels()` but applying no `WHERE` clauses. All filter params
in `TicketQuery` were bound by the model binder but silently ignored.

**Fix:** Added `ApplyFilters(IQueryable<TicketWithLabels>, TicketQuery)`
in `TicketQueries.cs`. Each filter is an independent `if` block that
chains a `.Where()` onto the `IQueryable` before `CountAsync` and
`.Skip().Take()`, so SQL does all filtering, counting, and paging.

**Filters implemented (FL-1 → FL-6):**
- Exact match: `status`, `priority`, `categoryId`, `customerId`, `assignedAgentId`
- `unassignedOnly=true` → `AssignedAgentId IS NULL`
- `search` → LIKE on `Title`, `Reference`, `Customer.Name` (trimmed)
- All filters AND-combined; `totalCount` is from the filtered set

**Tests added:** `TicketQueriesTests.cs` using in-memory SQLite – covers
exact priority match, customer-name search, unassigned-only, and combined
AND logic.

## Task 3: Login and Authentication

### 3.1 Domain & Infrastructure (AU-1, AU-2, AU-3, AU-7)

**Credential Storage Justification (AU-1):**
Added `PasswordHash` directly to the `Agent` aggregate (`Agents` table) rather than introducing a separate `Users` table. In Northwind Support, only agents authenticate (there is no customer login, public registration, or role matrix). Keeping credentials on `Agent` preserves aggregate root cohesion, eliminates unnecessary 1:1 table JOINs, and aligns with YAGNI while maintaining strict encapsulation via `SetPasswordHash(...)`.

**Password Hashing (AU-2):**
Implemented `IPasswordHasher` in Application abstractions and `IdentityPasswordHasher` in Infrastructure using ASP.NET Core Identity's `PasswordHasher<object>`. It employs vetted PBKDF2 with HMAC-SHA256, 100,000 iterations, and a cryptographic per-password 128-bit salt, protecting against rainbow table and timing attacks.

**Development Credentials (AU-3):**
All seeded agents (both active agents and inactive agent Yuki Tanaka) are seeded with a dedicated local fixture password:
- Dev Password: `DevPassword123!`
- Hashed dynamically at seed time in `SupportDbSeeder.cs` using `IPasswordHasher`.

**JWT Configuration & Token Lifetime (AU-7):**
- Signing key is loaded via ASP.NET Core configuration and documented in `.env.example` (`Jwt__Key`) and `appsettings.Development.json`. No secrets are hardcoded in source.
- Chosen token lifetime: **8 hours (480 minutes)** to support a standard agent workday without requiring refresh tokens.

**Committed Migration (AU-1):**
- Generated and applied EF Core migration: `20261020163351_AddAgentPasswordHash.cs`.
- Adds `PasswordHash NVARCHAR(500) NOT NULL DEFAULT N''` to the `Agents` table.

**Tests Added (3.3):**
- `IdentityPasswordHasherTests.cs`: Asserts salted uniqueness, positive match verification, and rejection of invalid/empty inputs.
- `JwtTokenServiceTests.cs`: Asserts token generation with expected claims (`sub`, `email`, `name`, `jti`), signing algorithm, and expiration using `FixedClock`.

---

### 3.2 Auth Endpoints & Middleware (AU-4, AU-5, AU-6, AU-8)

**POST /api/auth/login (AU-4):**
- `LoginCommandHandler` in Application layer handles email lookup, inactive account check, and password verification in constant time.
- A pre-computed dummy hash is used when the email doesn't exist to prevent timing-based user enumeration.
- Wrong email and wrong password both return the identical `401 {"detail":"Invalid email or password."}` response.
- Inactive agents cannot log in (also hashed before rejecting, for timing parity).
- On success returns `{ token, expiresAtUtc, user: { id, fullName, email, isActive } }`.

**GET /api/auth/me (AU-5):**
- `GetCurrentUserQueryHandler` resolves the agent from the `sub` claim in the bearer token.
- Returns `CurrentUserDto` or `401` if the token is missing, invalid, or the agent is inactive.

**Global Authorization Policy (AU-6):**
- `AuthorizationOptions.FallbackPolicy` requires `RequireAuthenticatedUser()` for every endpoint.
- `AuthController` methods are decorated with `[AllowAnonymous]` so the login endpoint stays open.
- Swagger UI and middleware are exempt by the ASP.NET middleware pipeline ordering.

**JWT Middleware & Swagger (AU-8):**
- `AddAuthentication(JwtBearerDefaults.AuthenticationScheme)` configured with issuer, audience, lifetime, and key validation. `ClockSkew = TimeSpan.Zero` prevents stale tokens from slipping through.
- `UseAuthentication()` / `UseAuthorization()` appear after `UseCors()` and before `MapControllers()`.
- Swagger configured with `AddSecurityDefinition("Bearer", ...)` and `AddSecurityRequirement(...)` using the Swashbuckle 10 / Microsoft.OpenApi 2.x API (`OpenApiSecuritySchemeReference`). The Authorize button appears in Swagger UI for every protected endpoint.

**Infrastructure (CurrentUserService):**
- `ICurrentUser` abstraction in Application reads `AgentId`, `FullName`, `Email`, `IsAuthenticated` from claims.
- `CurrentUserService` implementation resolves claims from `IHttpContextAccessor`, checking both standard and short-form claim type names for cross-library compatibility.

---

### 3.3 Frontend Authentication (AU-9 – AU-13)

**AU-9 — Login page (`/login`):**
- `LoginPage.tsx` renders a standalone page (no app shell/header) with email + password fields.
- Shows the identical generic message for wrong email, wrong password, and inactive account (mirrors AU-4 backend message).
- On success calls `signIn()`, updating the React auth context, then navigates back to the originally requested page (or `/`).

**AU-10 — `authStore.ts` (sessionStorage):**
- Token and user profile stored in `sessionStorage` (tab-scoped, not sent as a cookie, cleared on tab close).
- `authStore.set(token, user)` / `authStore.getToken()` / `authStore.getUser()` / `authStore.clear()`.
- `AuthContext.tsx` hydrates from `authStore` on mount so a page refresh preserves the session.

**AU-11 — Token attached to every API request:**
- `client.ts` → `request()` reads `authStore.getToken()` and injects `Authorization: Bearer <token>` header on every fetch call automatically.

**AU-12 — Route guard (`RequireAuth`):**
- `RequireAuth.tsx` reads `user` from `AuthContext`. If null, redirects to `/login` preserving `location.state.from`.
- All routes except `/login` are wrapped by `RequireAuth` in `routes.tsx`.

**AU-13 — Header logout button + 401 handling:**
- `App.tsx` shows `{user.fullName}` and a "Sign out" button in the top-right header.
- Clicking "Sign out" calls `signOut()` → `authStore.clear()` + `window.location.replace('/login')`.
- `client.ts` intercepts any `401` response, calls `authStore.clear()`, and redirects to `/login` so expired tokens are handled automatically.

