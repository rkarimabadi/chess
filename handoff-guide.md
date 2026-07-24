# راهنمای تحویل توسعه‌دهنده (Developer Handoff Guide)

| فیلد | مقدار |
| :--- | :--- |
| **نسخه** | 1.0.0 |
| **تاریخ** | ۱۴۰۵/۰۵/۰۱ |
| **وابستگی** | مستند معماری v1.1.0 + مستند نیازمندی PRD v2.4.0 |
| **مخاطب** | عامل هوش مصنوعی یا توسعه‌دهندهٔ فنی که قرار است کل پروژه را پیاده‌سازی کند |

> **هدف:** این سند شکاف‌های بین مستند معماری و یک پروژهٔ قابل کامپایل/اجرا را پر می‌کند. اگر این سند + معماری را به یک عامل هوش مصنوعی بدهید، باید بتواند بدون حدس‌زدن کل برنامه را بنویسد.

---

# فهرست مطالب

1. [Program.cs کامل](#۱-programcs-کامل)
2. [تمام DTOها به‌صورت C# Records](#۲-تمام-dtoها-به‌صورت-c-records)
3. [جدول ثبت Dependency Injection](#۳-جدول-ثبت-dependency-injection)
4. [ماتریس تصمیم «Could» Features](#۴-ماتریس-تصمیم-could-features)
5. [ترتیب ساخت گام‌به‌گام با مسیر فایل‌ها](#۵-ترتیب-ساخت-گامبهگام-با-مسیر-فایلها)

---

# ۱. Program.cs کامل

```csharp
// Chess.Web/Program.cs

using Chess.Application.Common.Authorization;
using Chess.Application.Services;
using Chess.Application.UseCases.Auth;
using Chess.Application.UseCases.Game;
using Chess.Application.UseCases.Social;
using Chess.Application.UseCases.Staff;
using Chess.Domain.Entities;
using Chess.Domain.Interfaces;
using Chess.Infrastructure.Data;
using Chess.Infrastructure.Repositories;
using Chess.Infrastructure.Services;
using Chess.Web.Components;
using Chess.Web.Hubs;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ══════════════════════════════════════════
// ۱. Database (SQLite for dev — ARCH-03, TECH-03)
// ══════════════════════════════════════════
builder.Services.AddDbContext<ChessDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// ══════════════════════════════════════════
// ۲. Authentication (ARCH-02: Cookie Auth)
// ══════════════════════════════════════════
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/auth/login";
        options.LogoutPath = "/auth/logout";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.Strict;
    });

// ══════════════════════════════════════════
// ۳. Authorization Policies
// ══════════════════════════════════════════
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("UserOnly", p => p.RequireRole("User", "Moderator", "Admin"))
    .AddPolicy("StaffOnly", p => p.RequireRole("Moderator", "Admin"))
    .AddPolicy("AdminOnly", p => p.RequireRole("Admin"))
    .AddPolicy("CanBan", p => p.RequireRole("Moderator", "Admin"))
    .AddPolicy("CanPermBan", p => p.RequireRole("Admin"))
    .AddPolicy("CanManageRoles", p => p.RequireRole("Admin"));

// ══════════════════════════════════════════
// ۴. Rate Limiting (§۱۱.۳)
// ══════════════════════════════════════════
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("login", opt =>
    {
        opt.PermitLimit = 5;
        opt.Window = TimeSpan.FromMinutes(5);
    });
    options.AddSlidingWindowLimiter("api", opt =>
    {
        opt.PermitLimit = 100;
        opt.Window = TimeSpan.FromMinutes(1);
    });
    options.AddSlidingWindowLimiter("preset-msg", opt =>
    {
        opt.PermitLimit = 5;
        opt.Window = TimeSpan.FromMinutes(1);
    });
    options.AddSlidingWindowLimiter("staff", opt =>
    {
        opt.PermitLimit = 30;
        opt.Window = TimeSpan.FromMinutes(1);
    });
});

// ══════════════════════════════════════════
// ۵. SignalR (ARCH-09, ARCH-10, TECH-12)
// ══════════════════════════════════════════
builder.Services.AddSignalR();

// ══════════════════════════════════════════
// ۶. Repositories
// ══════════════════════════════════════════
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IGameRepository, GameRepository>();
builder.Services.AddScoped<IMoveRepository, MoveRepository>();
builder.Services.AddScoped<IRoomRepository, RoomRepository>();
builder.Services.AddScoped<IRatingRepository, RatingRepository>();
builder.Services.AddScoped<IReportRepository, ReportRepository>();
builder.Services.AddScoped<ISanctionRepository, SanctionRepository>();
builder.Services.AddScoped<IAuditRepository, AuditRepository>();
builder.Services.AddScoped<IFriendshipRepository, FriendshipRepository>();
builder.Services.AddScoped<IUserBlockRepository, UserBlockRepository>();
builder.Services.AddScoped<IStaffNoteRepository, StaffNoteRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// ══════════════════════════════════════════
// ۷. Domain Services
// ══════════════════════════════════════════
builder.Services.AddSingleton<IRuleSet, ClassicRuleSet>();
builder.Services.AddSingleton<IGameStateManager, InMemoryGameStateManager>();
builder.Services.AddSingleton<IMatchmakingService, MatchmakingService>();

// ══════════════════════════════════════════
// ۸. Application Services
// ══════════════════════════════════════════
builder.Services.AddScoped<IClockService, ServerClockService>();
builder.Services.AddScoped<IRatingService, EloRatingService>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IPermissionChecker, PermissionChecker>();
builder.Services.AddScoped<IIdleAbandonTimer, IdleAbandonTimer>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddSingleton<IMetricsService, OtelMetricsService>();

// ══════════════════════════════════════════
// ۹. Use Cases
// ══════════════════════════════════════════
// Auth
builder.Services.AddScoped<RegisterUser>();
builder.Services.AddScoped<LoginUser>();
builder.Services.AddScoped<RecoverPassword>();
builder.Services.AddScoped<DeactivateAccount>();
builder.Services.AddScoped<DeleteAccount>();

// Game
builder.Services.AddScoped<CreateRoom>();
builder.Services.AddScoped<JoinRoom>();
builder.Services.AddScoped<JoinQueue>();
builder.Services.AddScoped<LeaveQueue>();
builder.Services.AddScoped<MakeMove>();
builder.Services.AddScoped<OfferDraw>();
builder.Services.AddScoped<RespondDraw>();
builder.Services.AddScoped<ResignGame>();
builder.Services.AddScoped<ProposeRematch>();
builder.Services.AddScoped<AcceptRematch>();

// Social
builder.Services.AddScoped<SendPresetMessage>();
builder.Services.AddScoped<SubmitReport>();
builder.Services.AddScoped<GetGameHistory>();
builder.Services.AddScoped<GetGameDetails>();
builder.Services.AddScoped<SendFriendRequest>();
builder.Services.AddScoped<RespondFriendRequest>();
builder.Services.AddScoped<RemoveFriend>();
builder.Services.AddScoped<BlockUser>();
builder.Services.AddScoped<UnblockUser>();
builder.Services.AddScoped<GetLiveSpectatableGames>();
builder.Services.AddScoped<JoinAsSpectator>();

// Staff
builder.Services.AddScoped<GetStaffDashboard>();
builder.Services.AddScoped<ListReports>();
builder.Services.AddScoped<ResolveReport>();
builder.Services.AddScoped<ApplySanction>();
builder.Services.AddScoped<RemoveSanction>();
builder.Services.AddScoped<AssignRole>();
builder.Services.AddScoped<ForceFinishGame>();
builder.Services.AddScoped<GetUserDossier>();
builder.Services.AddScoped<GetAuditLog>();

// ══════════════════════════════════════════
// ۱۰. Background Services
// ══════════════════════════════════════════
builder.Services.AddHostedService<DisconnectWatchdogService>();
builder.Services.AddHostedService<SnapshotService>();
builder.Services.AddHostedService<RoomCleanupService>();

// ══════════════════════════════════════════
// ۱۱. Blazor (ARCH-01: Interactive WebAssembly)
// ══════════════════════════════════════════
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

// ══════════════════════════════════════════
// ۱۲. Client-side Services (for Blazor WASM)
// ══════════════════════════════════════════
builder.Services.AddScoped<GameStateService>();
builder.Services.AddScoped<ThemeService>();
builder.Services.AddScoped<ISoundService, SoundService>();
builder.Services.AddScoped<ILocalStorageService, LocalStorageService>();

var app = builder.Build();

// ══════════════════════════════════════════
// Middleware Pipeline
// ══════════════════════════════════════════
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

// ══════════════════════════════════════════
// REST API Endpoints (§۸.۲)
// ══════════════════════════════════════════
var api = app.MapGroup("/api");

// Auth
api.MapPost("/auth/register", async (RegisterRequest req, RegisterUser uc) =>
{
    var result = await uc.ExecuteAsync(req);
    return Results.Created("/api/users/me", result);
}).AllowAnonymous().WithRateLimiting("login");

api.MapPost("/auth/login", async (LoginRequest req, LoginUser uc, HttpContext ctx) =>
{
    var result = await uc.ExecuteAsync(req, ctx);
    return Results.Ok(result);
}).AllowAnonymous().WithRateLimiting("login");

api.MapPost("/auth/logout", async (HttpContext ctx) =>
{
    await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Ok();
}).RequireAuthorization();

api.MapPost("/auth/recover", async (RecoverPasswordRequest req, RecoverPassword uc) =>
{
    var result = await uc.ExecuteAsync(req);
    return Results.Ok(result);
}).AllowAnonymous();

// User
api.MapGet("/users/me", async (HttpContext ctx, GetUserProfile uc) =>
{
    var userId = GetUserId(ctx);
    var result = await uc.ExecuteAsync(userId);
    return Results.Ok(result);
}).RequireAuthorization("UserOnly");

api.MapPut("/users/me", async (HttpContext ctx, UpdateProfileRequest req, UpdateProfile uc) =>
{
    var userId = GetUserId(ctx);
    var result = await uc.ExecuteAsync(userId, req);
    return Results.Ok(result);
}).RequireAuthorization("UserOnly");

api.MapPut("/users/me/password", async (HttpContext ctx, ChangePasswordRequest req, ChangePassword uc) =>
{
    var userId = GetUserId(ctx);
    var result = await uc.ExecuteAsync(userId, req);
    return Results.Ok(result);
}).RequireAuthorization("UserOnly");

api.MapPost("/users/me/deactivate", async (HttpContext ctx, DeactivateAccount uc) =>
{
    var userId = GetUserId(ctx);
    var result = await uc.ExecuteAsync(new DeactivateAccountRequest(userId));
    return Results.Ok(result);
}).RequireAuthorization("UserOnly");

api.MapDelete("/users/me", async (HttpContext ctx, DeleteAccount uc) =>
{
    var userId = GetUserId(ctx);
    var result = await uc.ExecuteAsync(new DeleteAccountRequest(userId));
    return Results.Ok(result);
}).RequireAuthorization("UserOnly");

// Game
api.MapGet("/games/{id:guid}", async (Guid id, GetGame uc) =>
{
    var result = await uc.ExecuteAsync(id);
    return result is not null ? Results.Ok(result) : Results.NotFound();
}).RequireAuthorization("UserOnly");

api.MapGet("/history", async (HttpContext ctx, GetGameHistory uc, int page = 1) =>
{
    var userId = GetUserId(ctx);
    var result = await uc.ExecuteAsync(new GetGameHistoryRequest(userId, page));
    return Results.Ok(result);
}).RequireAuthorization("UserOnly");

api.MapGet("/history/{id:guid}", async (Guid id, HttpContext ctx, GetGameDetails uc) =>
{
    var userId = GetUserId(ctx);
    var result = await uc.ExecuteAsync(new GetGameDetailsRequest(userId, id));
    return result is not null ? Results.Ok(result) : Results.NotFound();
}).RequireAuthorization("UserOnly");

api.MapGet("/games/live", async (GetLiveSpectatableGames uc, int page = 1) =>
{
    var result = await uc.ExecuteAsync(new GetLiveSpectatableGamesRequest(page));
    return Results.Ok(result);
}).RequireAuthorization("UserOnly");

// Reports (ARCH-13)
api.MapPost("/reports", async (HttpContext ctx, SubmitReportRequest req, SubmitReport uc) =>
{
    var reporterId = GetUserId(ctx);
    var result = await uc.ExecuteAsync(req with { ReporterId = reporterId });
    return Results.Created($"/api/reports/{result.ReportId}", result);
}).RequireAuthorization("UserOnly");

// Friends & Blocks (ARCH-11)
api.MapGet("/friends", async (HttpContext ctx, ListFriends uc) =>
{
    var userId = GetUserId(ctx);
    var result = await uc.ExecuteAsync(userId);
    return Results.Ok(result);
}).RequireAuthorization("UserOnly");

api.MapPost("/friends/requests", async (HttpContext ctx, SendFriendRequestRequest req, SendFriendRequest uc) =>
{
    var userId = GetUserId(ctx);
    var result = await uc.ExecuteAsync(req with { UserId = userId });
    return Results.Ok(result);
}).RequireAuthorization("UserOnly");

api.MapPut("/friends/requests/{id:guid}", async (Guid id, HttpContext ctx, RespondFriendRequestRequest req, RespondFriendRequest uc) =>
{
    var userId = GetUserId(ctx);
    var result = await uc.ExecuteAsync(userId, id, req.Accept);
    return Results.Ok(result);
}).RequireAuthorization("UserOnly");

api.MapDelete("/friends/{id:guid}", async (Guid id, HttpContext ctx, RemoveFriend uc) =>
{
    var userId = GetUserId(ctx);
    var result = await uc.ExecuteAsync(userId, id);
    return Results.Ok(result);
}).RequireAuthorization("UserOnly");

api.MapPost("/blocks", async (HttpContext ctx, BlockUserRequest req, BlockUser uc) =>
{
    var userId = GetUserId(ctx);
    var result = await uc.ExecuteAsync(req with { UserId = userId });
    return Results.Ok(result);
}).RequireAuthorization("UserOnly");

api.MapDelete("/blocks/{userId:guid}", async (Guid userId, HttpContext ctx, UnblockUser uc) =>
{
    var blockerId = GetUserId(ctx);
    var result = await uc.ExecuteAsync(blockerId, userId);
    return Results.Ok(result);
}).RequireAuthorization("UserOnly");

// Matchmaking
api.MapPost("/matchmaking/join", async (HttpContext ctx, JoinQueueRequest req, JoinQueue uc) =>
{
    var userId = GetUserId(ctx);
    var result = await uc.ExecuteAsync(req with { UserId = userId });
    return Results.Ok(result);
}).RequireAuthorization("UserOnly");

api.MapDelete("/matchmaking/cancel", async (HttpContext ctx, LeaveQueue uc) =>
{
    var userId = GetUserId(ctx);
    var result = await uc.ExecuteAsync(userId);
    return Results.Ok(result);
}).RequireAuthorization("UserOnly");

// Room
api.MapPost("/rooms", async (HttpContext ctx, CreateRoomRequest req, CreateRoom uc) =>
{
    var userId = GetUserId(ctx);
    var result = await uc.ExecuteAsync(req with { UserId = userId });
    return Results.Created($"/api/rooms/{result.RoomId}", result);
}).RequireAuthorization("UserOnly");

api.MapPost("/rooms/join", async (HttpContext ctx, JoinRoomRequest req, JoinRoom uc) =>
{
    var userId = GetUserId(ctx);
    var result = await uc.ExecuteAsync(req with { UserId = userId });
    return Results.Ok(result);
}).RequireAuthorization("UserOnly");

api.MapPut("/rooms/{id:guid/ready", async (Guid id, HttpContext ctx, ReadyRoom uc) =>
{
    var userId = GetUserId(ctx);
    var result = await uc.ExecuteAsync(userId, id);
    return Results.Ok(result);
}).RequireAuthorization("UserOnly");

// Staff
var staff = api.MapGroup("/staff").RequireAuthorization("StaffOnly");

staff.MapGet("/dashboard", async (HttpContext ctx, GetStaffDashboard uc) =>
{
    var staffId = GetUserId(ctx);
    var result = await uc.ExecuteAsync(staffId);
    return Results.Ok(result);
});

staff.MapGet("/reports", async (HttpContext ctx, ListReports uc, string? status, int page = 1) =>
{
    var staffId = GetUserId(ctx);
    var result = await uc.ExecuteAsync(new ListReportsRequest(staffId, status, page));
    return Results.Ok(result);
});

staff.MapPut("/reports/{id:guid}", async (Guid id, HttpContext ctx, ResolveReportRequest req, ResolveReport uc) =>
{
    var staffId = GetUserId(ctx);
    var result = await uc.ExecuteAsync(staffId, id, req);
    return Results.Ok(result);
});

staff.MapGet("/users/search", async (string q, SearchUsers uc) =>
{
    var result = await uc.ExecuteAsync(q);
    return Results.Ok(result);
});

staff.MapGet("/users/{id:guid}", async (Guid id, HttpContext ctx, GetUserDossier uc) =>
{
    var staffId = GetUserId(ctx);
    var result = await uc.ExecuteAsync(staffId, id);
    return result is not null ? Results.Ok(result) : Results.NotFound();
});

staff.MapPost("/sanctions", async (HttpContext ctx, ApplySanctionRequest req, ApplySanction uc) =>
{
    var staffId = GetUserId(ctx);
    var result = await uc.ExecuteAsync(req with { StaffId = staffId });
    return Results.Created($"/api/staff/sanctions/{result.SanctionId}", result);
}).RequireAuthorization("CanBan");

staff.MapDelete("/sanctions/{id:guid}", async (Guid id, HttpContext ctx, RemoveSanction uc) =>
{
    var staffId = GetUserId(ctx);
    var result = await uc.ExecuteAsync(staffId, id);
    return Results.Ok(result);
}).RequireAuthorization("StaffOnly");

staff.MapPost("/roles", async (HttpContext ctx, AssignRoleRequest req, AssignRole uc) =>
{
    var adminId = GetUserId(ctx);
    var result = await uc.ExecuteAsync(adminId, req);
    return Results.Ok(result);
}).RequireAuthorization("CanManageRoles");

staff.MapGet("/audit", async (HttpContext ctx, GetAuditLog uc, AuditLogFilter filter) =>
{
    var adminId = GetUserId(ctx);
    var result = await uc.ExecuteAsync(adminId, filter);
    return Results.Ok(result);
}).RequireAuthorization("AdminOnly");

// ══════════════════════════════════════════
// SignalR Hubs
// ══════════════════════════════════════════
app.MapHub<GameHub>("/hubs/game");
app.MapHub<StaffHub>("/hubs/staff");

// ══════════════════════════════════════════
// Blazor
// ══════════════════════════════════════════
app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Program).Assembly);

app.Run();

// ══════════════════════════════════════════
// Helper
// ══════════════════════════════════════════
static Guid GetUserId(HttpContext ctx) =>
    Guid.Parse(ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)!);
```

---

# ۲. تمام DTOها به‌صورت C# Records

```csharp
// Chess.Application/DTOs/Common.cs

public sealed record PagedResult<T>(List<T> Items, int TotalCount, int Page, int PageSize);

public sealed record ApiResponse<T>(bool Success, T? Data, string? Error);
```

## ۲.۱ Auth DTOs

```csharp
// Chess.Application/DTOs/AuthDtos.cs

public sealed record RegisterRequest(string Username, string Email, string Password);
public sealed record LoginRequest(string Login, string Password);
public sealed record AuthResponse(Guid UserId, string Username, int Rating, string Role);
public sealed record RecoverPasswordRequest(string Email);
public sealed record ChangePasswordRequest(string OldPassword, string NewPassword);
public sealed record UpdateProfileRequest(string? DisplayName);
public sealed record DeactivateAccountRequest(Guid UserId);
public sealed record DeleteAccountRequest(Guid UserId);
```

## ۲.۲ Game DTOs

```csharp
// Chess.Application/DTOs/GameDtos.cs

public sealed record CreateRoomRequest(Guid UserId, string TimeControl, bool IsRated, string? ColorPreference);
public sealed record CreateRoomResponse(Guid RoomId, string InviteCode);
public sealed record JoinRoomRequest(Guid UserId, string InviteCode);
public sealed record JoinRoomResponse(Guid RoomId, string OpponentUsername);
public sealed record ReadyRoomRequest(Guid UserId, Guid RoomId);

public sealed record MakeMoveRequest(Guid UserId, Guid GameId, string From, string To, string? Promotion);
public sealed record MakeMoveResponse(string Status, string? SanNotation, string NewFen, long WhiteTimeMs, long BlackTimeMs);

public sealed record GameStateDto
{
    public Guid GameId { get; init; }
    public string Status { get; init; } = string.Empty;
    public bool IsRated { get; init; }
    public string Variant { get; init; } = string.Empty;
    public TimeControlDto TimeControl { get; init; } = new();
    public PlayerDto White { get; init; } = new();
    public PlayerDto Black { get; init; } = new();
    public string CurrentTurn { get; init; } = string.Empty;
    public string BoardFen { get; init; } = string.Empty;
    public long WhiteTimeMs { get; init; }
    public long BlackTimeMs { get; init; }
    public LastMoveDto? LastMove { get; init; }
    public int MoveCount { get; init; }
    public bool DrawOfferPending { get; init; }
    public MaterialDto Material { get; init; } = new();
}

public sealed record GameResultDto
{
    public Guid GameId { get; init; }
    public string Result { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
    public RatingChangeDto? WhiteRating { get; init; }
    public RatingChangeDto? BlackRating { get; init; }
}

public sealed record MoveDto
{
    public int MoveNumber { get; init; }
    public string From { get; init; } = string.Empty;
    public string To { get; init; } = string.Empty;
    public string San { get; init; } = string.Empty;
    public bool IsCheck { get; init; }
    public bool IsCheckmate { get; init; }
    public bool IsCapture { get; init; }
    public DateTime Timestamp { get; init; }
}

public sealed record PlayerDto(Guid Id, string Username, int Rating);
public sealed record TimeControlDto(int Base, int Increment);
public sealed record MaterialDto(List<string> CapturedByWhite, List<string> CapturedByBlack);
public sealed record LastMoveDto(string From, string To, string San, bool IsCheck);

public sealed record GameListItemDto
{
    public Guid GameId { get; init; }
    public string OpponentUsername { get; init; } = string.Empty;
    public int OpponentRating { get; init; }
    public string Result { get; init; } = string.Empty;
    public string Variant { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}

public sealed record GameDetailsDto
{
    public Guid GameId { get; init; }
    public string Status { get; init; } = string.Empty;
    public string Result { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
    public bool IsRated { get; init; }
    public string Variant { get; init; } = string.Empty;
    public TimeControlDto TimeControl { get; init; } = new();
    public PlayerDto White { get; init; } = new();
    public PlayerDto Black { get; init; } = new();
    public string FinalFen { get; init; } = string.Empty;
    public List<MoveDto> Moves { get; init; } = new();
    public RatingChangeDto? WhiteRating { get; init; }
    public RatingChangeDto? BlackRating { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? FinishedAt { get; init; }
}

public sealed record SpectatableGameDto
{
    public Guid GameId { get; init; }
    public PlayerDto White { get; init; } = new();
    public PlayerDto Black { get; init; } = new();
    public string Variant { get; init; } = string.Empty;
    public string CurrentTurn { get; init; } = string.Empty;
    public int MoveCount { get; init; }
    public DateTime StartedAt { get; init; }
}
```

## ۲.۳ Matchmaking/Queue DTOs

```csharp
// Chess.Application/DTOs/MatchmakingDtos.cs

public sealed record JoinQueueRequest(Guid UserId, string TimeControl, bool IsRated);
public sealed record JoinQueueResponse(string QueueId, int EstimatedWaitSeconds);
public sealed record MatchFoundDto(string RoomId, Guid OpponentId, string TimeControl);
```

## ۲.۴ Report DTOs

```csharp
// Chess.Application/DTOs/ReportDtos.cs

public sealed record SubmitReportRequest(Guid ReporterId, Guid TargetUserId, string Reason, Guid? GameId, string? Note);
public sealed record SubmitReportResponse(Guid ReportId);

public sealed record ReportDto
{
    public Guid Id { get; init; }
    public string ReporterUsername { get; init; } = string.Empty;
    public string TargetUsername { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
    public string? Note { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public Guid? GameId { get; init; }
}

public sealed record ReportListItemDto
{
    public Guid Id { get; init; }
    public string TargetUsername { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}

public sealed record ResolveReportRequest(string Action, string Note);
```

## ۲.۵ Sanction DTOs

```csharp
// Chess.Application/DTOs/SanctionDtos.cs

public sealed record ApplySanctionRequest(Guid StaffId, Guid UserId, string Type, string Reason, int? DurationDays);
public sealed record ApplySanctionResponse(Guid SanctionId, DateTime? EndsAt);

public sealed record SanctionDto
{
    public Guid Id { get; init; }
    public string Username { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
    public DateTime StartsAt { get; init; }
    public DateTime? EndsAt { get; init; }
    public bool IsActive { get; init; }
}
```

## ۲.۶ Staff DTOs

```csharp
// Chess.Application/DTOs/StaffDtos.cs

public sealed record DashboardDto
{
    public int OnlineUsers { get; init; }
    public int ActiveGames { get; init; }
    public int QueueLength { get; init; }
    public int OpenReports { get; init; }
    public int RecentBans { get; init; }
}

public sealed record UserDossierDto
{
    public UserDto User { get; init; } = new();
    public List<SanctionDto> Sanctions { get; init; } = new();
    public List<ReportListItemDto> Reports { get; init; } = new();
    public List<GameListItemDto> RecentGames { get; init; } = new();
}

public sealed record UserDto
{
    public Guid Id { get; init; }
    public string Username { get; init; } = string.Empty;
    public string? Email { get; init; } // masked for moderator (DEC-29)
    public int Rating { get; init; }
    public string Role { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public int GamesPlayed { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? LastLoginAt { get; init; }
}

public sealed record AuditLogDto
{
    public Guid Id { get; init; }
    public string ActorUsername { get; init; } = string.Empty;
    public string ActionType { get; init; } = string.Empty;
    public string TargetType { get; init; } = string.Empty;
    public Guid TargetId { get; init; }
    public string Reason { get; init; } = string.Empty;
    public string? DetailsJson { get; init; }
    public DateTime CreatedAt { get; init; }
}

public sealed record AuditLogFilter(Guid? StaffId, string? ActionType, DateTime? From, DateTime? To, int Page = 1);

public sealed record AssignRoleRequest(Guid UserId, string Role);
public sealed record ForceFinishRequest(string Reason);
```

## ۲.۷ Friend DTOs

```csharp
// Chess.Application/DTOs/FriendDtos.cs

public sealed record FriendDto(Guid Id, string Username, int Rating, bool IsOnline);
public sealed record FriendshipDto(Guid Id, string RequesterUsername, string Status, DateTime CreatedAt);
public sealed record BlockDto(Guid Id, string Username);
```

## ۲.۸ Rating DTOs

```csharp
// Chess.Application/DTOs/RatingDtos.cs

public sealed record RatingChangeDto(int OldRating, int NewRating, int Delta);
```

---

# ۳. جدول ثبت Dependency Injection

| Interface | Implementation | Lifetime | Registration |
| :--- | :--- | :---: | :--- |
| `ChessDbContext` | — | Scoped | `AddDbContext<ChessDbContext>` |
| `IUserRepository` | `UserRepository` | Scoped | `AddScoped` |
| `IGameRepository` | `GameRepository` | Scoped | `AddScoped` |
| `IMoveRepository` | `MoveRepository` | Scoped | `AddScoped` |
| `IRoomRepository` | `RoomRepository` | Scoped | `AddScoped` |
| `IRatingRepository` | `RatingRepository` | Scoped | `AddScoped` |
| `IReportRepository` | `ReportRepository` | Scoped | `AddScoped` |
| `ISanctionRepository` | `SanctionRepository` | Scoped | `AddScoped` |
| `IAuditRepository` | `AuditRepository` | Scoped | `AddScoped` |
| `IFriendshipRepository` | `FriendshipRepository` | Scoped | `AddScoped` |
| `IUserBlockRepository` | `UserBlockRepository` | Scoped | `AddScoped` |
| `IStaffNoteRepository` | `StaffNoteRepository` | Scoped | `AddScoped` |
| `IUnitOfWork` | `UnitOfWork` | Scoped | `AddScoped` |
| `IRuleSet` | `ClassicRuleSet` | Singleton | `AddSingleton` |
| `IGameStateManager` | `InMemoryGameStateManager` | Singleton | `AddSingleton` |
| `IMatchmakingService` | `MatchmakingService` | Singleton | `AddSingleton` |
| `IClockService` | `ServerClockService` | Scoped | `AddScoped` |
| `IRatingService` | `EloRatingService` | Scoped | `AddScoped` |
| `IPasswordHasher` | `PasswordHasher` | Scoped | `AddScoped` |
| `IPermissionChecker` | `PermissionChecker` | Scoped | `AddScoped` |
| `IIdleAbandonTimer` | `IdleAbandonTimer` | Scoped | `AddScoped` |
| `IEmailService` | `EmailService` | Scoped | `AddScoped` |
| `IMetricsService` | `OtelMetricsService` | Singleton | `AddSingleton` |
| `ISoundService` | `SoundService` | Scoped | `AddScoped` (client-side) |
| `GameStateService` | — | Scoped | `AddScoped` (client-side) |
| `ThemeService` | — | Scoped | `AddScoped` (client-side) |
| `ILocalStorageService` | `LocalStorageService` | Scoped | `AddScoped` (client-side) |

**Background Services:**

| Service | Lifetime |
| :--- | :---: |
| `DisconnectWatchdogService` | HostedService |
| `SnapshotService` | HostedService |
| `RoomCleanupService` | HostedService |

---

# ۴. ماتریس تصمیم «Could» Features

> تصمیم نهایی: کدام Could features در اولین ساخت (first build) اجرا شوند و کدام به فاز بعد موکول شوند.

| Feature ID | Feature Name | Decision | Phase | Rationale |
| :--- | :--- | :--- | :--- | :--- |
| F-SOC-03 | لیست دوستان | **Include** | Phase 2 | Entity و Schema از فاز ۰ آماده‌اند؛ UI فاز ۲ |
| F-SOC-04 | دعوت مستقیم | **Include** | Phase 2 | وابسته به F-SOC-03 |
| F-SOC-05 | وضعیت آنلاین | **Include** | Phase 2 | `IPresenceTracker` از فاز ۰ |
| F-SOC-07 | بلاک کاربر | **Include** | Phase 2 | Entity آماده؛ Matchmaking check لازم |
| F-SPEC-01 | لیست بازی‌های زنده | **Include** | Phase 2 | UseCase آماده |
| F-SPEC-02 | تماشا با تأخیر | **Include** | Phase 2 | گروه SignalR از فاز ۰ |
| F-SPEC-03 | عدم نمایش چت | **Include** | Phase 2 | implicit |
| F-UI-03 | تم تاریک | **Include** | Phase 1x | `dark.css` از فاز ۰ آماده |
| F-UI-04 | تم‌های اضافی | **Skip** | Phase 2 | فقط interface از فاز ۰ |
| F-UI-06 | انیمیشن حرکت | **Include** | Phase 1 | CSS animation ساده |
| F-UI-07 | کاهش حرکت | **Skip** | v1.2 | `prefers-reduced-motion` |
| F-FB-04 | لرزش UI | **Include** | Phase 1 | CSS keyframes |
| F-NTF-01 | اعلان درون‌برنامه‌ای | **Include** | Phase 1 | toast component |
| F-NTF-02 | Browser Notification | **Skip** | Phase 2 | نیاز به permission |
| F-HIST-03 | دانلود PGN | **Skip** | Phase 2 | کم‌اولویت |
| F-RATE-06 | لیدربورد | **Skip** | Phase 2 | نیاز به pagination سنگین |
| F-GAME-20 | Premoves | **Skip** | v1.2 | پیچیدگی UX |
| F-GAME-22 | کپی FEN/PGN | **Skip** | Phase 2 | کم‌اولویت |
| F-PWA-06 | Push notifications | **Skip** | Phase 2 | نیاز به service worker پیشرفته |
| F-TYP-05 | ارقام فارسی | **Skip** | Could | DEC-18: لاتین بهتر است |
| F-TYP-06 | فونت تک‌عرض | **Include** | Phase 1x | برای تاریخچه حرکات |
| F-CNT-02 | راهنمای قوانین | **Include** | Phase 1x | صفحه ثابت ساده |
| F-STAFF-12 | لیست آنلاین | **Skip** | Phase 2 | نیاز به PresenceTracker غنی |
| F-STAFF-29 | محدودیت Matchmaking | **Skip** | v1.2 | sanction نرم |
| F-STAFF-42 | تنظیمات عملیاتی | **Skip** | v1.2 | feature flags |
| F-STAFF-43 | اعلامیه سراسری | **Skip** | v1.2 | banner ساده |
| F-STAFF-44 | خروجی آمار | **Skip** | Could | export functionality |
| F-CLK-04 | نمایش دهم‌ثانیه | **Include** | Phase 1x | UX بهبود |
| F-CLK-05 | هشدار کم‌بودن زمان | **Include** | Phase 1 | clock component |
| F-VAR-04 | Bullet | **Skip** | v1.1 | نیاز به UX مخصوص |
| F-VAR-07 | Chess960 | **Skip** | Phase 3 | نیاز به بلوغ RuleSet |
| F-VAR-08 | واریانت سرگرمی | **Skip** | Phase 3 | خارج از MVP |

---

# ۵. ترتیب ساخت گام‌به‌گام با مسیر فایل‌ها

> هر مرحله یک فایل است. ترتیب اجباری است — وابستگی‌ها رعایت شده‌اند.

## Phase 0 — Foundation (Sprint 1-2)

| # | فایل | بخش معماری |
| :---: | :--- | :--- |
| 1 | `src/Chess.sln` | Solution skeleton |
| 2 | `src/Chess.Domain/Chess.Domain.csproj` | Project setup |
| 3 | `src/Chess.Domain/Common/Entity.cs` | §۲.۳ |
| 4 | `src/Chess.Domain/Common/AggregateRoot.cs` | §۲.۳ |
| 5 | `src/Chess.Domain/Common/IDomainEvent.cs` | §۲.۳ |
| 6 | `src/Chess.Domain/ValueObjects/Piece.cs` | §۲.۱ |
| 7 | `src/Chess.Domain/ValueObjects/Square.cs` | §۲.۱ |
| 8 | `src/Chess.Domain/ValueObjects/PieceColorExtensions.cs` | §۵.۶ |
| 9 | `src/Chess.Domain/ValueObjects/BoardState.cs` | §۲.۱ |
| 10 | `src/Chess.Domain/ValueObjects/MoveRecord.cs` | §۲.۲ |
| 11 | `src/Chess.Domain/Chess/Move.cs` | §۳.۳ |
| 12 | `src/Chess.Domain/Chess/MoveGenerator.cs` | §۳.۳ |
| 13 | `src/Chess.Domain/Chess/DrawDetector.cs` | §۳.۴ |
| 14 | `src/Chess.Domain/Chess/Rules/IRuleSet.cs` | §۳.۱ |
| 15 | `src/Chess.Domain/Chess/Rules/ClassicRuleSet.cs` | §۳.۲ |
| 16 | `src/Chess.Domain/Entities/User.cs` | §۲.۲ |
| 17 | `src/Chess.Domain/Entities/Game.cs` | §۲.۲ |
| 18 | `src/Chess.Domain/Entities/Room.cs` | §۲.۲ |
| 19 | `src/Chess.Domain/Entities/RatingChange.cs` | §۲.۲ |
| 20 | `src/Chess.Domain/Entities/PlayerReport.cs` | §۲.۲ |
| 21 | `src/Chess.Domain/Entities/UserSanction.cs` | §۲.۲ |
| 22 | `src/Chess.Domain/Entities/StaffAuditLog.cs` | §۲.۲ |
| 23 | `src/Chess.Domain/Entities/Friendship.cs` | §۲.۲ |
| 24 | `src/Chess.Domain/Entities/UserBlock.cs` | §۲.۲ |
| 25 | `src/Chess.Domain/Entities/StaffNote.cs` | §۵.۱.۱ |
| 26 | `src/Chess.Domain/Events/GameEvents.cs` | §۲.۴ |
| 27 | `src/Chess.Domain/Themes/IPieceSkin.cs` | §۱۰.۴ |
| 28 | `src/Chess.Domain/Themes/IBoardSkin.cs` | §۱۰.۴ |
| 29 | `src/Chess.Domain/Themes/ClassicPieceSkin.cs` | §۱۰.۴ |
| 30 | `src/Chess.Domain/Themes/ClassicBoardSkin.cs` | §۱۰.۴ |
| 31 | `src/Chess.Domain/Interfaces/IPasswordHasher.cs` | §۱۱.۲ |
| 32 | `src/Chess.Domain/Interfaces/ITimeProvider.cs` | Infrastructure |
| 33 | `src/Chess.Application/Chess.Application.csproj` | Project setup |
| 34 | `src/Chess.Application/Common/IUseCase.cs` | §۴.۱ |
| 35 | `src/Chess.Application/Common/UseCaseBase.cs` | §۴.۱ |
| 36 | `src/Chess.Application/Common/Authorization/IPermissionChecker.cs` | §۴.۳ |
| 37 | `src/Chess.Application/Ports/IRepositories.cs` | §۵.۳ |
| 38 | `src/Chess.Application/DTOs/Common.cs` | §۲ handoff |
| 39 | `src/Chess.Application/DTOs/AuthDtos.cs` | §۲ handoff |
| 40 | `src/Chess.Application/DTOs/GameDtos.cs` | §۲ handoff |
| 41 | `src/Chess.Application/DTOs/MatchmakingDtos.cs` | §۲ handoff |
| 42 | `src/Chess.Application/DTOs/ReportDtos.cs` | §۲ handoff |
| 43 | `src/Chess.Application/DTOs/SanctionDtos.cs` | §۲ handoff |
| 44 | `src/Chess.Application/DTOs/StaffDtos.cs` | §۲ handoff |
| 45 | `src/Chess.Application/DTOs/FriendDtos.cs` | §۲ handoff |
| 46 | `src/Chess.Application/DTOs/RatingDtos.cs` | §۲ handoff |
| 47 | `src/Chess.Application/Services/IRatingService.cs` | §۵.۵ |
| 48 | `src/Chess.Application/Services/IClockService.cs` | §۵.۶ |
| 49 | `src/Chess.Application/Services/IIdleAbandonTimer.cs` | §۵.۷ |
| 50 | `src/Chess.Application/Services/IMatchmakingService.cs` | §۷.۵ |
| 51 | `src/Chess.Application/Services/IGameStateManager.cs` | §۵.۴ |
| 52 | `src/Chess.Application/Services/IMetricsService.cs` | §۱۲.۱ |
| 53 | `src/Chess.Infrastructure/Chess.Infrastructure.csproj` | Project setup |
| 54 | `src/Chess.Infrastructure/Data/ChessDbContext.cs` | §۵.۱ (with OnModelCreating) |
| 55 | `src/Chess.Infrastructure/Repositories/UserRepository.cs` | §۵.۳ |
| 56 | `src/Chess.Infrastructure/Repositories/GameRepository.cs` | §۵.۳ |
| 57 | `src/Chess.Infrastructure/Repositories/MoveRepository.cs` | §۵.۳ |
| 58 | `src/Chess.Infrastructure/Repositories/RoomRepository.cs` | §۵.۳ |
| 59 | `src/Chess.Infrastructure/Repositories/RatingRepository.cs` | §۵.۳ |
| 60 | `src/Chess.Infrastructure/Repositories/ReportRepository.cs` | §۵.۳ |
| 61 | `src/Chess.Infrastructure/Repositories/SanctionRepository.cs` | §۵.۳ |
| 62 | `src/Chess.Infrastructure/Repositories/AuditRepository.cs` | §۵.۳ |
| 63 | `src/Chess.Infrastructure/Repositories/FriendshipRepository.cs` | §۵.۳ |
| 64 | `src/Chess.Infrastructure/Repositories/UserBlockRepository.cs` | §۵.۳ |
| 65 | `src/Chess.Infrastructure/Repositories/StaffNoteRepository.cs` | §۵.۳ |
| 66 | `src/Chess.Infrastructure/Repositories/UnitOfWork.cs` | §۵.۳ |
| 67 | `src/Chess.Infrastructure/Services/EloRatingService.cs` | §۵.۵ |
| 68 | `src/Chess.Infrastructure/Services/ServerClockService.cs` | §۵.۶ |
| 69 | `src/Chess.Infrastructure/Services/PasswordHasher.cs` | §۱۱.۲ |
| 70 | `src/Chess.Infrastructure/Services/InMemoryGameStateManager.cs` | §۵.۴ |
| 71 | `src/Chess.Infrastructure/Services/MatchmakingService.cs` | §۷.۵ |
| 72 | `src/Chess.Infrastructure/Services/DisconnectWatchdogService.cs` | §۵.۴ |
| 73 | `src/Chess.Infrastructure/Services/IdleAbandonTimer.cs` | §۵.۷ |
| 74 | `src/Chess.Infrastructure/Services/EmailService.cs` | Infrastructure |
| 75 | `src/Chess.Infrastructure/Services/OtelMetricsService.cs` | §۱۲.۱ |
| 76 | `src/Chess.Infrastructure/Services/SecurityAuditLogger.cs` | §۱۲.۲ |
| 77 | `tests/Chess.Domain.Tests/Chess.Domain.Tests.csproj` | Test project |
| 78 | `tests/Chess.Domain.Tests/Chess/MoveGeneratorTests.cs` | §۳.۳ |
| 79 | `tests/Chess.Domain.Tests/Chess/ClassicRuleSetTests.cs` | §۳.۲ |
| 80 | `tests/Chess.Domain.Tests/Chess/DrawDetectorTests.cs` | §۳.۴ |
| 81 | `tests/Chess.Domain.Tests/Chess/BoardStateTests.cs` | §۲.۱ |
| 82 | `tests/Chess.Domain.Tests/Rating/EloRatingServiceTests.cs` | §۵.۵ |

## Phase 1 — MVP Game (Sprint 3-5)

| # | فایل | بخش معماری |
| :---: | :--- | :--- |
| 83 | `src/Chess.Web/Chess.Web.csproj` | Project setup |
| 84 | `src/Chess.Web/Program.cs` | §۱ handoff |
| 85 | `src/Chess.Web/appsettings.json` | Config |
| 86 | `src/Chess.Web/Components/App.razor` | Blazor root |
| 87 | `src/Chess.Web/Components/Layout/MainLayout.razor` | Layout |
| 88 | `src/Chess.Web/Components/Layout/MainLayout.razor.css` | CSS Isolation |
| 89 | `src/Chess.Web/Components/Layout/NavMenu.razor` | Navigation |
| 90 | `src/Chess.Web/Components/Pages/Landing.razor` | §۶.۲ |
| 91 | `src/Chess.Web/Components/Pages/Login.razor` | §۶.۲ |
| 92 | `src/Chess.Web/Components/Pages/Register.razor` | §۶.۲ |
| 93 | `src/Chess.Web/Components/Pages/Recover.razor` | §۶.۲ |
| 94 | `src/Chess.Web/Components/Pages/Dashboard.razor` | §۶.۲ |
| 95 | `src/Chess.Web/Components/Pages/Queue.razor` | §۶.۲ |
| 96 | `src/Chess.Web/Components/Pages/Room.razor` | §۶.۲ |
| 97 | `src/Chess.Web/Components/Pages/Game.razor` | §۶.۲ |
| 98 | `src/Chess.Web/Components/Pages/GameResult.razor` | §۶.۲ |
| 99 | `src/Chess.Web/Components/Game/ChessBoard.razor` | §۶.۴ |
| 100 | `src/Chess.Web/Components/Game/ChessBoard.razor.css` | §۶.۵ |
| 101 | `src/Chess.Web/Components/Game/SquareComponent.razor` | §۶.۴ |
| 102 | `src/Chess.Web/Components/Game/Clock.razor` | §۶.۲ |
| 103 | `src/Chess.Web/Components/Game/MoveList.razor` | §۶.۲ |
| 104 | `src/Chess.Web/Components/Game/PlayerCard.razor` | §۶.۲ |
| 105 | `src/Chess.Web/Components/Game/DrawOfferBanner.razor` | §۶.۲ |
| 106 | `src/Chess.Web/Components/Game/PresetChat.razor` | §۶.۲ |
| 107 | `src/Chess.Web/Components/Game/MaterialDisplay.razor` | §۶.۲ |
| 108 | `src/Chess.Web/Components/Game/PromotionDialog.razor` | §۶.۲ |
| 109 | `src/Chess.Web/Components/Game/GameStatusIndicator.razor` | §۶.۲ |
| 110 | `src/Chess.Web/Components/Game/ResultOverlay.razor` | §۶.۲ |
| 111 | `src/Chess.Web/Components/Game/RatingDeltaCard.razor` | §۶.۲ |
| 112 | `src/Chess.Web/Hubs/IGameHub.cs` | §۷.۱ |
| 113 | `src/Chess.Web/Hubs/GameHub.cs` | §۷.۲ |
| 114 | `src/Chess.Web/Hubs/IStaffHub.cs` | §۷.۳ |
| 115 | `src/Chess.Web/Hubs/StaffHub.cs` | §۷.۳ |
| 116 | `src/Chess.Web/Services/GameStateService.cs` | §۶.۳ |
| 117 | `src/Chess.Web/Services/ThemeService.cs` | §۱۰.۵ |
| 118 | `src/Chess.Web/Services/SoundService.cs` | §۱۳ |
| 119 | `src/Chess.Web/Services/LocalStorageService.cs` | Client-side |
| 120 | `src/Chess.Application/UseCases/Auth/RegisterUser.cs` | §۴.۲ |
| 121 | `src/Chess.Application/UseCases/Auth/LoginUser.cs` | §۴.۲ |
| 122 | `src/Chess.Application/UseCases/Auth/RecoverPassword.cs` | §۴.۲ |
| 123 | `src/Chess.Application/UseCases/Auth/DeactivateAccount.cs` | §۴.۲ |
| 124 | `src/Chess.Application/UseCases/Auth/DeleteAccount.cs` | §۴.۲ |
| 125 | `src/Chess.Application/UseCases/Game/CreateRoom.cs` | §۴.۲ |
| 126 | `src/Chess.Application/UseCases/Game/JoinRoom.cs` | §۴.۲ |
| 127 | `src/Chess.Application/UseCases/Game/JoinQueue.cs` | §۴.۲ |
| 128 | `src/Chess.Application/UseCases/Game/LeaveQueue.cs` | §۴.۲ |
| 129 | `src/Chess.Application/UseCases/Game/MakeMove.cs` | §۴.۲ |
| 130 | `src/Chess.Application/UseCases/Game/OfferDraw.cs` | §۴.۲ |
| 131 | `src/Chess.Application/UseCases/Game/RespondDraw.cs` | §۴.۲ |
| 132 | `src/Chess.Application/UseCases/Game/ResignGame.cs` | §۴.۲ |
| 133 | `src/Chess.Application/UseCases/Game/ProposeRematch.cs` | §۴.۲ |
| 134 | `src/Chess.Application/UseCases/Game/AcceptRematch.cs` | §۴.۲ |
| 135 | `src/Chess.Application/UseCases/Social/SendPresetMessage.cs` | §۴.۲ |
| 136 | `src/Chess.Application/UseCases/Social/SubmitReport.cs` | §۴.۲ |
| 137 | `src/Chess.Application/UseCases/Social/GetGameHistory.cs` | §۴.۲ |
| 138 | `src/Chess.Application/UseCases/Social/GetGameDetails.cs` | §۴.۲ |
| 139 | `src/Chess.Web/wwwroot/app.css` | §۱۰.۲ |
| 140 | `src/Chess.Web/wwwroot/manifest.json` | §۹.۱ |
| 141 | `src/Chess.Web/wwwroot/sw.js` | §۹.۲ |
| 142 | `src/Chess.Web/wwwroot/js/register-sw.js` | §۹.۳ |
| 143 | `src/Chess.Web/wwwroot/sounds/move.mp3` | §۱۳ |
| 144 | `src/Chess.Web/wwwroot/sounds/capture.mp3` | §۱۳ |
| 145 | `src/Chess.Web/wwwroot/sounds/check.mp3` | §۱۳ |
| 146 | `src/Chess.Web/wwwroot/sounds/game-end.mp3` | §۱۳ |
| 147 | `src/Chess.Web/wwwroot/fonts/` (Vazirmatn woff2) | §۱۰.۱ |
| 148 | `src/Chess.Web/wwwroot/pieces/classic/` (SVG files) | §۱۰.۴ |
| 149 | `tests/Chess.Application.Tests/` | Test project |
| 150 | `tests/Chess.Application.Tests/UseCases/MakeMoveTests.cs` | §۴.۲ |
| 151 | `tests/Chess.Application.Tests/UseCases/CreateRoomTests.cs` | §۴.۲ |
| 152 | `tests/Chess.Application.Tests/Services/MatchmakingTests.cs` | §۷.۵ |

## Phase 1x — Staff + Polish (Sprint 6-7)

| # | فایل | بخش معماری |
| :---: | :--- | :--- |
| 153 | `src/Chess.Web/Components/Layout/StaffLayout.razor` | §۶.۲ |
| 154 | `src/Chess.Web/Components/Staff/StaffDashboard.razor` | §۶.۲ |
| 155 | `src/Chess.Web/Components/Staff/StatCard.razor` | §۶.۲ |
| 156 | `src/Chess.Web/Components/Staff/ReportsQueue.razor` | §۶.۲ |
| 157 | `src/Chess.Web/Components/Staff/ReportDetail.razor` | §۶.۲ |
| 158 | `src/Chess.Web/Components/Staff/UserSearch.razor` | §۶.۲ |
| 159 | `src/Chess.Web/Components/Staff/UserDossier.razor` | §۶.۲ |
| 160 | `src/Chess.Web/Components/Staff/ActiveGames.razor` | §۶.۲ |
| 161 | `src/Chess.Web/Components/Staff/AuditLog.razor` | §۶.۲ |
| 162 | `src/Chess.Web/Components/Staff/RoleManagement.razor` | §۶.۲ |
| 163 | `src/Chess.Web/Components/Staff/SanctionDialog.razor` | §۶.۲ |
| 164 | `src/Chess.Web/Components/Pages/Profile.razor` | §۶.۲ |
| 165 | `src/Chess.Web/Components/Pages/Settings.razor` | §۶.۲ |
| 166 | `src/Chess.Web/Components/Pages/History.razor` | §۶.۲ |
| 167 | `src/Chess.Web/Components/Pages/GameReview.razor` | §۶.۲ |
| 168 | `src/Chess.Web/Components/Pages/Terms.razor` | §۶.۲ |
| 169 | `src/Chess.Web/Components/Pages/Privacy.razor` | §۶.۲ |
| 170 | `src/Chess.Web/Components/Pages/About.razor` | §۶.۲ |
| 171 | `src/Chess.Web/Components/Pages/Faq.razor` | §۶.۲ |
| 172 | `src/Chess.Web/wwwroot/themes/dark.css` | §۱۰.۳ |
| 173 | `src/Chess.Web/wwwroot/themes/classic-skin.css` | §۱۰.۴ |
| 174 | `src/Chess.Application/UseCases/Staff/GetStaffDashboard.cs` | §۴.۲ |
| 175 | `src/Chess.Application/UseCases/Staff/ListReports.cs` | §۴.۲ |
| 176 | `src/Chess.Application/UseCases/Staff/ResolveReport.cs` | §۴.۲ |
| 177 | `src/Chess.Application/UseCases/Staff/ApplySanction.cs` | §۴.۲ |
| 178 | `src/Chess.Application/UseCases/Staff/RemoveSanction.cs` | §۴.۲ |
| 179 | `src/Chess.Application/UseCases/Staff/AssignRole.cs` | §۴.۲ |
| 180 | `src/Chess.Application/UseCases/Staff/ForceFinishGame.cs` | §۴.۲ |
| 181 | `src/Chess.Application/UseCases/Staff/GetUserDossier.cs` | §۴.۲ |
| 182 | `src/Chess.Application/UseCases/Staff/GetAuditLog.cs` | §۴.۲ |
| 183 | `src/Chess.Application/UseCases/Staff/SearchUsers.cs` | §۴.۲ |
| 184 | `src/Chess.Application/UseCases/Staff/ReadyRoom.cs` | §۴.۲ |
| 185 | `src/Chess.Application/UseCases/Staff/GetUserProfile.cs` | §۴.۲ |
| 186 | `src/Chess.Application/UseCases/Staff/UpdateProfile.cs` | §۴.۲ |
| 187 | `src/Chess.Application/UseCases/Staff/ChangePassword.cs` | §۴.۲ |
| 188 | `src/Chess.Application/UseCases/Staff/GetGame.cs` | §۴.۲ |
| 189 | `src/Chess.Infrastructure/Services/RoomCleanupService.cs` | BackgroundService |

## Phase 2 — Social + Spectate (Sprint 8+)

| # | فایل | بخش معماری |
| :---: | :--- | :--- |
| 190 | `src/Chess.Web/Components/Pages/Friends.razor` | §۶.۲ |
| 191 | `src/Chess.Web/Components/Social/FriendList.razor` | §۶.۲ |
| 192 | `src/Chess.Web/Components/Social/FriendRequests.razor` | §۶.۲ |
| 193 | `src/Chess.Web/Components/Social/AddFriendBox.razor` | §۶.۲ |
| 194 | `src/Chess.Web/Components/Social/BlockedUsersList.razor` | §۶.۲ |
| 195 | `src/Chess.Web/Components/Pages/SpectateList.razor` | §۶.۲ |
| 196 | `src/Chess.Web/Components/Pages/Spectate.razor` | §۶.۲ |
| 197 | `src/Chess.Application/UseCases/Social/SendFriendRequest.cs` | §۴.۲ |
| 198 | `src/Chess.Application/UseCases/Social/RespondFriendRequest.cs` | §۴.۲ |
| 199 | `src/Chess.Application/UseCases/Social/RemoveFriend.cs` | §۴.۲ |
| 200 | `src/Chess.Application/UseCases/Social/BlockUser.cs` | §۴.۲ |
| 201 | `src/Chess.Application/UseCases/Social/UnblockUser.cs` | §۴.۲ |
| 202 | `src/Chess.Application/UseCases/Social/ListFriends.cs` | §۴.۲ |
| 203 | `src/Chess.Application/UseCases/Game/GetLiveSpectatableGames.cs` | §۴.۲ |
| 204 | `src/Chess.Application/UseCases/Game/JoinAsSpectator.cs` | §۴.۲ |
| 205 | `src/Chess.Infrastructure/Services/PresenceTracker.cs` | §۲.۲ note |

---

**پایان راهنمای تحویل توسعه‌دهنده — نسخه ۱.۰.۰**
