# AGENTS.md

## Project Overview

Online Chess Platform (PvP) — Persian/RTL UI with a full-stack .NET architecture.

**Stack:** C# / ASP.NET Core 9.0, Blazor WebAssembly, SQLite (dev), PostgreSQL (prod), DDD + Clean Architecture, SignalR, PWA

**Language:** UI in Persian (Farsi), RTL layout, Vazirmatn/PeydaWeb fonts, LahzehShomar-inspired design system

---

## Solution Structure

```
D:\Code\Chess\
├── architecture.md              # Full technical architecture (Persian)
├── handoff-guide.md             # Handoff/onboarding guide
├── document.md                  # Product requirements
├── chess-app-layout-guide.md    # Layout/design reference (LahzehShomar-inspired)
├── src/
│   ├── Chess.sln                # Solution file
│   ├── Chess.Domain/            # Domain layer — entities, value objects, chess engine
│   ├── Chess.Application/       # Application layer — use cases, DTOs, ports
│   ├── Chess.Infrastructure/    # Infrastructure — EF Core, repos, services
│   └── Chess.Web/
│       ├── Chess.Web/           # Server project (hosts Blazor, API, SignalR hubs)
│       └── Chess.Web.Client/    # Blazor WASM client (pages, components, styles)
└── tests/
    ├── Chess.Domain.Tests/
    ├── Chess.Application.Tests/
    └── Chess.Infrastructure.Tests/
```

---

## Architecture Principles

1. **DDD + Clean Architecture** — Domain has zero dependencies; Application references Domain; Infrastructure implements Application ports; Web orchestrates everything.
2. **Interactive WebAssembly** — All Blazor pages run on WASM (`@rendermode InteractiveWebAssembly`). The server project handles prerendering only.
3. **Two SignalR Hubs** — `GameHub` (matchmaking, moves, draw/resign) + `StaffHub` (moderation).
4. **In-memory game state** — `ConcurrentDictionary` for live games; periodic snapshots to DB via `SnapshotService`.
5. **Cookie Auth + Anti-forgery** — ASP.NET Identity with admin/moderator roles.
6. **CSS Isolation** — Component-specific styles in `.razor.css` files. Shared global styles in `wwwroot/` CSS files loaded via `App.razor`.

---

## Key Conventions

### Blazor
- Client components go in `Chess.Web.Client/Components/` (subfolders: `Pages/`, `Layout/`, `Game/`, `Staff/`).
- Child components should NOT have `@rendermode` — the parent page/layout already sets it.
- `Routes.razor` lives in the **Client** project, not the server project.
- `@inject HttpClient` fails during server-side prerendering. Use `new HttpClient()` or disable prerendering.
- Use `@rendermode InteractiveWebAssembly` on layout and pages, not on child components.
- `::deep` is required for targeting child component elements from scoped CSS.

### Domain / EF Core
- `Square` and `Piece` are records in `Chess.Domain.Entities` — add `modelBuilder.Ignore<Square>()` and `modelBuilder.Ignore<Piece>()` in `ChessDbContext`.
- `PieceColor` and `PieceType` are enums (value types) — do NOT use `modelBuilder.Ignore<T>()` on them.
- `MoveRecord` exists in both `Chess.Domain.Entities` and `Chess.Domain.ValueObjects` — the ValueObjects version is canonical; ignore the Entities one in DbContext.
- **Square must have EF Core value conversion** — `Square` is a record, so EF Core ignores it by default. Use `HasConversion(sq => sq.ToString(), s => Square.Parse(s))` on `MoveRecord.From` and `MoveRecord.To`, or they'll be null in the database.

### SignalR
- `GameHub` currently has `[Authorize]` removed to allow anonymous matchmaking connections. Re-add when auth flow is fully wired.
- Matchmaking logic: `JoinMatchmakingQueue` → `ConcurrentDictionary` queue → when 2 players queue, create game via `TryMatchPlayersAsync`.
- Hubs are strongly-typed (`IGameHub`, `IStaffHub`).
- **FenAfter must be computed before creating MoveRecord** — simulate the move on a copy of the board, then save the resulting FEN.
- **SaveChangesAsync must be called after adding moves** — `UoW.Moves.AddAsync()` alone does not persist.

### CSS / Styling
- Design tokens in `app.css` (CSS custom properties under `:root` and `[data-theme="dark"]`).
- Component-specific styles use CSS Isolation (`.razor.css` files).
- Shared global styles live in `wwwroot/` CSS files (e.g., `staff.css`) loaded via `App.razor`.
- Never use hardcoded Apple blue `#007AFF` — use project blue `#5e72e4` from design tokens.
- Duplicate CSS sections were cleaned up — avoid re-adding duplicates.
- `staff.css` contains all shared staff section styles (tables, badges, detail rows, search, messages, pagination, empty states, sanction overlay, responsive breakpoints).

### Authentication
- Cookie-based with `builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)`.
- Seed user: admin (role: `Admin`) created on startup.
- Login/Register forms are currently stubs — API calls not wired due to prerendering constraints.

### Toast Notifications
- `ToastService` is registered as **Singleton** (not Scoped) — Scoped causes the event pattern to fail across component boundaries.
- Pattern: `ToastService.OnShow += handler` in `OnInitialized`, remove in `Dispose`.
- Toast container is in `MainLayout.razor`, subscribes to `ToastService.OnShow`.

### Icons
- Use **Bootstrap Icons** (`bi bi-*` classes), not emoji.
- Icon classes: `bi-controller`, `bi-search`, `bi-flag`, `bi-people-fill`, `bi-slash-circle`, `bi-exclamation-triangle-fill`, `bi-info-circle-fill`, etc.

---

## Build & Run

```bash
# From src/ directory:
dotnet build Chess.sln
dotnet run --project Chess.Web/Chess.Web
```

**Success criteria:** 0 build errors, 0 warnings in hot path.

---

## Current State

### Working
- Full DDD architecture with all 4 layers
- Chess engine (move generation, validation, check/checkmate/stalemate)
- EF Core with SQLite, 10+ tables auto-created, admin user seeded
- 20+ REST API endpoints (auth, games, matchmaking, rooms, staff, reports, force-finish)
- SignalR hubs with matchmaking and full game flow (moves, draw offers, resign, disconnect/reconnect)
- Blazor WASM pages: Landing, Login, Register, Queue, Game, History, Terms, Privacy
- Chess components: ChessBoard, Clock, MoveList, PlayerCard, PromotionDialog, DrawOfferBanner, GameActions, ConnectionOverlay, PresetChat, RatingDeltaCard, ResultOverlay
- Staff section: Dashboard, Reports, Users, Audit, Roles (sidebar layout with modular content grid)
- Staff components: StatCard, ReportsQueue, ReportDetail, UserSearch, UserDossier, SanctionDialog, AuditLog, RoleManagement
- Toast notification system (singleton service, container in MainLayout)
- Mobile-first navigation: BottomNav on mobile, two-row top nav on desktop
- PWA support (manifest.json, service worker, icons)
- CSS Isolation on all staff and game components
- Global staff styles in `staff.css`

### In Progress / Needs Work
- Queue page SignalR connection — working but needs end-to-end testing (two browser tabs)
- Auth flow — login/register forms don't actually call API
- Dark theme toggle — CSS tokens exist but toggle logic not wired
- Room creation/join flow
- GameResult.razor — hardcoded values, no API fetch
- Clock ticking not enforced server-side

### Known Issues / Tech Debt
- `ReadyRoom.cs` misplaced in `UseCases/Staff/` namespace (not staff-specific)
- `SecurityAuditLogger.cs` unused — use cases create `StaffAuditLog` directly
- `GetGame` use case is dead — no API endpoint calls it (`/staff/games/active` uses inline code)
- `PermissionChecker` uses `.Result` (sync-over-async) which can deadlock

---

## Important Files to Know

| File | Purpose |
|---|---|
| `Chess.Web/Chess.Web/Program.cs` | Server entry — DI, auth, API endpoints (20+), SignalR hubs |
| `Chess.Web/Chess.Web/Hubs/GameHub.cs` | SignalR hub — matchmaking, moves, draw/resign, disconnect/reconnect |
| `Chess.Web/Chess.Web/wwwroot/app.css` | Global CSS (design tokens + game/nav/footer styles) |
| `Chess.Web/Chess.Web/wwwroot/staff.css` | Global staff section CSS (tables, badges, detail rows, search, etc.) |
| `Chess.Web/Chess.Web/Components/App.razor` | Root HTML — loads `app.css`, `dark.css`, `staff.css` |
| `Chess.Web/Chess.Web.Client/Components/Layout/MainLayout.razor` | Root layout — nav, ToastContainer, ConnectionOverlay |
| `Chess.Web/Chess.Web.Client/Components/Layout/StaffLayout.razor` | Staff sidebar layout (240px sidebar + modular content grid) |
| `Chess.Web/Chess.Web.Client/Components/Layout/NavMenu.razor` | Desktop top nav (header + links row) |
| `Chess.Web/Chess.Web.Client/Components/Layout/BottomNav.razor` | Mobile bottom navigation |
| `Chess.Web/Chess.Web.Client/Components/Routes.razor` | Blazor router (Client project) |
| `Chess.Web/Chess.Web.Client/Components/ToastContainer.razor` | Global toast notification container |
| `Chess.Web/Chess.Web.Client/Components/Game/` | All game UI components (ChessBoard, Clock, MoveList, etc.) |
| `Chess.Web/Chess.Web.Client/Components/Staff/` | All staff UI components (StatCard, ReportsQueue, etc.) |
| `Chess.Infrastructure/Data/ChessDbContext.cs` | EF Core context — 10+ entities, Square/Piece ignored |
| `Chess.Infrastructure/Services/InMemoryGameStateManager.cs` | Live game state (ConcurrentDictionary) |
| `Chess.Application/UseCases/Staff/` | Staff use cases (GetStaffDashboard, GetUserDossier, ForceFinishGame, etc.) |
| `Chess.Application/Ports/IRepositories.cs` | Repository interfaces |
| `Chess.Domain/Entities/` | Domain entities (Game, User, MoveRecord, etc.) |
| `Chess.Domain/ValueObjects/` | Value objects (Square, Piece, MoveRecord — canonical) |

---

## CSS Architecture

```
app.css              → Design tokens, game components, nav, footer
dark.css             → Dark theme overrides (data-theme="dark")
staff.css            → Shared staff section styles (loaded globally via App.razor)
*.razor.css          → Component-scoped styles (CSS Isolation)
```

**Rule:** Staff shared styles belong in `staff.css`, NOT in individual `.razor.css` files. CSS Isolation does not cascade to child components.

---

## Design System Reference

The project follows a design system derived from the LahzehShomar reference project. See `chess-app-layout-guide.md` for:
- Color palette (primary: `#5e72e4`, status colors, surface colors)
- Typography (Vazirmatn/PeydaWeb, scale from `--text-xs` to `--text-3xl`)
- Spacing scale (`--space-1` through `--space-16`)
- Card patterns (elevated, interactive, stat, list, detail)
- Navigation (sidebar on desktop, bottom nav on mobile)
- Staff section layout guide (section 3.9)

---

## Tests

```bash
# From src/ directory:
dotnet test Chess.sln
```

- 68 tests passing across Domain, Application, and Infrastructure
- Staff tests mock `IGameRepository`, `ISanctionRepository`, `IMatchmakingService`, `IGameStateManager`
- Test file: `tests/Chess.Application.Tests/UseCases/StaffTests.cs`
