using System.Security.Claims;
using Chess.Application.Common.Authorization;
using Chess.Application.DTOs;
using Chess.Application.Ports;
using Chess.Application.Services;
using Chess.Application.UseCases.Auth;
using Chess.Application.UseCases.Game;
using Chess.Application.UseCases.Social;
using Chess.Application.UseCases.Staff;
using Chess.Application.UseCases.User;
using Chess.Domain.Chess.Rules;
using Chess.Domain.Entities;
using Chess.Domain.Interfaces;
using Chess.Domain.ValueObjects;
using Chess.Infrastructure.Data;
using Chess.Infrastructure.Repositories;
using Chess.Infrastructure.Services;
using Chess.Web.Components;
using Chess.Web.Hubs;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Database (SQLite)
builder.Services.AddDbContext<ChessDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Authentication (Cookie Auth)
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

// Authorization Policies
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("UserOnly", p => p.RequireRole("User", "Moderator", "Admin"))
    .AddPolicy("StaffOnly", p => p.RequireRole("Moderator", "Admin"))
    .AddPolicy("AdminOnly", p => p.RequireRole("Admin"))
    .AddPolicy("CanBan", p => p.RequireRole("Moderator", "Admin"))
    .AddPolicy("CanPermBan", p => p.RequireRole("Admin"))
    .AddPolicy("CanManageRoles", p => p.RequireRole("Admin"));

// Rate Limiting
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

// SignalR
builder.Services.AddSignalR();

// Repositories
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

// Domain Services
builder.Services.AddSingleton<IRuleSet, ClassicRuleSet>();
builder.Services.AddSingleton<IGameStateManager, InMemoryGameStateManager>();
builder.Services.AddSingleton<IMatchmakingService, MatchmakingService>();

// Application Services
builder.Services.AddScoped<IClockService, ServerClockService>();
builder.Services.AddScoped<IRatingService, EloRatingService>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IPermissionChecker, PermissionChecker>();
builder.Services.AddScoped<IIdleAbandonTimer, IdleAbandonTimer>();
builder.Services.AddSingleton<IMetricsService, OtelMetricsService>();
builder.Services.AddScoped<IEmailService, EmailService>();

// Use Cases — Auth
builder.Services.AddScoped<RegisterUser>();
builder.Services.AddScoped<LoginUser>();
builder.Services.AddScoped<RecoverPassword>();
builder.Services.AddScoped<DeactivateAccount>();
builder.Services.AddScoped<DeleteAccount>();

// Use Cases — Game
builder.Services.AddScoped<CreateRoom>();
builder.Services.AddScoped<JoinRoom>();
builder.Services.AddScoped<ReadyRoom>();
builder.Services.AddScoped<JoinQueue>();
builder.Services.AddScoped<LeaveQueue>();
builder.Services.AddScoped<MakeMove>();
builder.Services.AddScoped<OfferDraw>();
builder.Services.AddScoped<RespondDraw>();
builder.Services.AddScoped<ResignGame>();
builder.Services.AddScoped<ProposeRematch>();
builder.Services.AddScoped<AcceptRematch>();
builder.Services.AddScoped<GetGame>();

// Use Cases — Social
builder.Services.AddScoped<SendPresetMessage>();
builder.Services.AddScoped<SubmitReport>();
builder.Services.AddScoped<GetGameHistory>();
builder.Services.AddScoped<GetGameDetails>();
builder.Services.AddScoped<SendFriendRequest>();
builder.Services.AddScoped<RespondFriendRequest>();
builder.Services.AddScoped<RemoveFriend>();
builder.Services.AddScoped<BlockUser>();
builder.Services.AddScoped<UnblockUser>();
builder.Services.AddScoped<ListFriends>();
builder.Services.AddScoped<GetLiveSpectatableGames>();
builder.Services.AddScoped<JoinAsSpectator>();

// Use Cases — Staff
builder.Services.AddScoped<GetStaffDashboard>();
builder.Services.AddScoped<ListReports>();
builder.Services.AddScoped<ResolveReport>();
builder.Services.AddScoped<ApplySanction>();
builder.Services.AddScoped<RemoveSanction>();
builder.Services.AddScoped<AssignRole>();
builder.Services.AddScoped<ForceFinishGame>();
builder.Services.AddScoped<GetUserDossier>();
builder.Services.AddScoped<GetAuditLog>();
builder.Services.AddScoped<SearchUsers>();

// Use Cases — User
builder.Services.AddScoped<GetUserProfile>();
builder.Services.AddScoped<UpdateProfile>();
builder.Services.AddScoped<ChangePassword>();

// Background Services
builder.Services.AddHostedService<DisconnectWatchdogService>();
builder.Services.AddHostedService<SnapshotService>();
builder.Services.AddHostedService<RoomCleanupService>();

// Blazor
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

// Client-side services (needed for prerendering)
builder.Services.AddScoped<Chess.Web.Client.Services.GameStateService>();
builder.Services.AddScoped<Chess.Web.Client.Services.ThemeService>();
builder.Services.AddScoped<Chess.Web.Client.Services.ISoundService, Chess.Web.Client.Services.SoundService>();
builder.Services.AddScoped<Chess.Web.Client.Services.ILocalStorageService, Chess.Web.Client.Services.LocalStorageService>();
builder.Services.AddSingleton<Chess.Web.Client.Services.ToastService>();

var app = builder.Build();

// Middleware Pipeline
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

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ChessDbContext>();
    db.Database.EnsureCreated();

    // Seed admin user if none exists
    if (!db.Users.Any())
    {
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var admin = Chess.Domain.Entities.User.Create("admin", "admin@chess.local", hasher.Hash("Admin123!"));
        admin.SetRole(UserRole.Admin);
        db.Users.Add(admin);
        db.SaveChanges();
    }
}

// REST API Endpoints
var api = app.MapGroup("/api");

// Auth
api.MapPost("/auth/register", async (RegisterRequest req, RegisterUser uc, HttpContext ctx) =>
{
    var result = await uc.ExecuteAsync(req);

    var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, result.UserId.ToString()),
        new Claim(ClaimTypes.Name, result.Username),
        new Claim(ClaimTypes.Role, result.Role)
    };
    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    await ctx.SignInAsync(new ClaimsPrincipal(identity));

    return Results.Created("/api/users/me", result);
}).AllowAnonymous().RequireRateLimiting("login");

api.MapPost("/auth/login", async (LoginRequest req, LoginUser uc, HttpContext ctx) =>
{
    try
    {
        var result = await uc.ExecuteAsync(req);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, result.UserId.ToString()),
            new Claim(ClaimTypes.Name, result.Username),
            new Claim(ClaimTypes.Role, result.Role)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await ctx.SignInAsync(new ClaimsPrincipal(identity));

        return Results.Ok(result);
    }
    catch (UnauthorizedAccessException)
    {
        return Results.Unauthorized();
    }
}).AllowAnonymous().RequireRateLimiting("login");

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
    try
    {
        var userId = GetUserId(ctx);
        var result = await uc.ExecuteAsync(userId);
        return Results.Ok(result);
    }
    catch (InvalidOperationException)
    {
        return Results.Unauthorized();
    }
}).RequireAuthorization("UserOnly");

api.MapPut("/users/me", async (HttpContext ctx, UpdateProfileRequest req, UpdateProfile uc) =>
{
    var userId = GetUserId(ctx);
    var result = await uc.ExecuteAsync((userId, req));
    return Results.Ok(result);
}).RequireAuthorization("UserOnly");

api.MapPut("/users/me/password", async (HttpContext ctx, ChangePasswordRequest req, ChangePassword uc) =>
{
    var userId = GetUserId(ctx);
    var result = await uc.ExecuteAsync((userId, req));
    return Results.Ok(result);
}).RequireAuthorization("UserOnly");

api.MapPost("/users/me/deactivate", async (HttpContext ctx, DeactivateAccount uc) =>
{
    var userId = GetUserId(ctx);
    var result = await uc.ExecuteAsync(new DeactivateAccountRequest(userId));
    return Results.Ok(result);
}).RequireAuthorization("UserOnly");

api.MapPost("/users/me/delete", async (HttpContext ctx, DeleteAccountReq body, DeleteAccount uc) =>
{
    var userId = GetUserId(ctx);
    var result = await uc.ExecuteAsync(new DeleteAccountRequest(userId, body.Confirmation));
    return Results.Ok(result);
}).RequireAuthorization("UserOnly");

// Game
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

api.MapGet("/games/active", async (HttpContext ctx, IGameRepository games, IUserRepository users) =>
{
    var userId = GetUserId(ctx);
    var activeGames = await games.GetUserActiveGamesAsync(userId);
    var result = new List<object>();
    foreach (var g in activeGames)
    {
        var opponentId = g.WhitePlayerId == userId ? g.BlackPlayerId : g.WhitePlayerId;
        var opponent = await users.GetByIdAsync(opponentId);
        result.Add(new
        {
            GameId = g.Id,
            OpponentUsername = opponent?.Username ?? "نامشخص",
            OpponentRating = opponent?.Rating ?? 1200,
            IsRated = g.IsRated,
            Variant = g.Variant,
            MoveCount = g.MoveHistory?.Count ?? 0,
            CreatedAt = g.CreatedAt,
            TimeControl = FormatTimeControl(g.BaseTimeSeconds, g.IncrementSeconds)
        });
    }
    return Results.Ok(result);
}).RequireAuthorization("UserOnly");

// Reports (ARCH-13)
api.MapPost("/reports", async (HttpContext ctx, SubmitReportRequest req, SubmitReport uc) =>
{
    var reporterId = GetUserId(ctx);
    var result = await uc.ExecuteAsync(req with { ReporterId = reporterId });
    return Results.Created($"/api/reports/{result.ReportId}", result);
}).RequireAuthorization("UserOnly");

api.MapGet("/games/{id:guid}", async (Guid id, GetGame uc) =>
{
    var result = await uc.ExecuteAsync(id);
    return result is not null ? Results.Ok(result) : Results.NotFound();
}).RequireAuthorization("UserOnly");

api.MapGet("/games/live", async (GetLiveSpectatableGames uc, int page = 1) =>
{
    var result = await uc.ExecuteAsync(new GetLiveSpectatableGamesRequest(page));
    return Results.Ok(result);
}).RequireAuthorization("UserOnly");

// Friends & Blocks
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
    return Results.Created($"/api/friends/requests/{result}", result);
}).RequireAuthorization("UserOnly");

api.MapPut("/friends/requests/{id:guid}", async (Guid id, HttpContext ctx, RespondFriendRequestRequest req, RespondFriendRequest uc) =>
{
    var userId = GetUserId(ctx);
    var result = await uc.ExecuteAsync((userId, id, req.Accept));
    return Results.Ok(result);
}).RequireAuthorization("UserOnly");

api.MapDelete("/friends/{id:guid}", async (Guid id, HttpContext ctx, RemoveFriend uc) =>
{
    var userId = GetUserId(ctx);
    var result = await uc.ExecuteAsync((userId, id));
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
    var currentUserId = GetUserId(ctx);
    var result = await uc.ExecuteAsync((currentUserId, userId));
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
api.MapPost("/rooms", async (HttpContext ctx, CreateRoomRequest req, CreateRoom uc, IHubContext<GameHub, IGameHub> hubContext) =>
{
    var userId = GetUserId(ctx);
    var result = await uc.ExecuteAsync(req with { UserId = userId });
    await hubContext.Clients.Group("room-list").RoomListUpdated();
    return Results.Created($"/api/rooms/{result.RoomId}", result);
}).RequireAuthorization("UserOnly");

api.MapPost("/rooms/join", async (HttpContext ctx, JoinRoomRequest req, JoinRoom uc, IHubContext<GameHub, IGameHub> hubContext) =>
{
    var userId = GetUserId(ctx);
    var result = await uc.ExecuteAsync(req with { UserId = userId });
    await hubContext.Clients.Group("room-list").RoomListUpdated();
    // Notify the host that a guest has joined the room
    await hubContext.Clients.Group($"room:{result.RoomId}").OpponentJoinedRoom(result.RoomId.ToString(), userId);
    return Results.Ok(result);
}).RequireAuthorization("UserOnly");

api.MapGet("/rooms", async (HttpContext ctx, IRoomRepository rooms, int page = 1, int pageSize = 20) =>
{
    var list = await rooms.GetOpenRoomsAsync(page, pageSize);
    var result = list.Select(r => new RoomListItemDto
    {
        RoomId = r.Id,
        HostId = r.HostId,
        TimeControl = FormatTimeControl(r.BaseTimeSeconds, r.IncrementSeconds),
        IsRated = r.IsRated,
        CreatedAt = r.CreatedAt
    }).ToList();
    return Results.Ok(new { items = result, page, pageSize });
}).AllowAnonymous();

api.MapGet("/rooms/{id:guid}", async (Guid id, IRoomRepository rooms, IUserRepository users, HttpContext ctx) =>
{
    var room = await rooms.GetByIdAsync(id);
    if (room == null) return Results.NotFound();
    var userId = GetUserId(ctx);
    var host = await users.GetByIdAsync(room.HostId);
    var guest = room.GuestId.HasValue ? await users.GetByIdAsync(room.GuestId.Value) : null;
    return Results.Ok(new
    {
        Status = room.Status.ToString(),
        TimeControl = FormatTimeControl(room.BaseTimeSeconds, room.IncrementSeconds),
        room.IsRated,
        HostUsername = host?.Username ?? "میزبان",
        HasGuest = room.GuestId.HasValue,
        GuestUsername = guest?.Username,
        room.HostReady,
        room.GuestReady,
        IsHost = room.HostId == userId,
        IsGuest = room.GuestId == userId,
        MyUserId = userId
    });
}).RequireAuthorization("UserOnly");

api.MapPost("/rooms/{id:guid}/ready", async (Guid id, HttpContext ctx, ReadyRoom uc) =>
{
    var userId = GetUserId(ctx);
    var result = await uc.ExecuteAsync((userId, id));
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
    var result = await uc.ExecuteAsync((staffId, id, req));
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
    var result = await uc.ExecuteAsync((staffId, id));
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
    var result = await uc.ExecuteAsync((staffId, id));
    return Results.Ok(result);
}).RequireAuthorization("StaffOnly");

staff.MapPost("/roles", async (HttpContext ctx, AssignRoleRequest req, AssignRole uc) =>
{
    var adminId = GetUserId(ctx);
    var result = await uc.ExecuteAsync((adminId, req));
    return Results.Ok(result);
}).RequireAuthorization("CanManageRoles");

staff.MapGet("/audit", async (HttpContext ctx, GetAuditLog uc, Guid? staffId, string? actionType, DateTime? from, DateTime? to, int page = 1) =>
{
    var adminId = GetUserId(ctx);
    var filter = new AuditLogFilter(staffId, actionType, from, to, page);
    var result = await uc.ExecuteAsync((adminId, filter));
    return Results.Ok(result);
}).RequireAuthorization("AdminOnly");

staff.MapGet("/games/active", async (IGameRepository gameRepo, IUserRepository userRepo) =>
{
    var games = await gameRepo.GetActiveGamesAsync();
    var result = new List<object>();
    foreach (var g in games)
    {
        var white = await userRepo.GetByIdAsync(g.WhitePlayerId);
        var black = await userRepo.GetByIdAsync(g.BlackPlayerId);
        result.Add(new
        {
            GameId = g.Id,
            White = new { Username = white?.Username ?? "?", Rating = white?.Rating ?? 0 },
            Black = new { Username = black?.Username ?? "?", Rating = black?.Rating ?? 0 },
            Variant = g.Variant,
            MoveCount = g.MoveHistory.Count,
            CreatedAt = g.CreatedAt
        });
    }
    return Results.Ok(result);
});

staff.MapPost("/games/{id:guid}/force-finish", async (Guid id, HttpContext ctx, ForceFinishRequest req, ForceFinishGame uc) =>
{
    var adminId = GetUserId(ctx);
    var result = await uc.ExecuteAsync((adminId, id, req));
    return Results.Ok(result);
}).RequireAuthorization("AdminOnly");

staff.MapGet("/users/{id:guid}/notes", async (Guid id, IStaffNoteRepository noteRepo) =>
{
    var notes = await noteRepo.GetByUserIdAsync(id);
    return Results.Ok(notes);
});

staff.MapPost("/users/{id:guid}/notes", async (Guid id, HttpContext ctx, StaffNoteRequest req, IStaffNoteRepository noteRepo, IUnitOfWork uow) =>
{
    var staffId = GetUserId(ctx);
    var note = StaffNote.Create(id, staffId, req.Body);
    await noteRepo.AddAsync(note);
    await uow.SaveChangesAsync();
    return Results.Created($"/api/staff/users/{id}/notes", note);
});

// SignalR Hubs
app.MapHub<GameHub>("/hubs/game");
app.MapHub<StaffHub>("/hubs/staff");

// Blazor
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Chess.Web.Client._Imports).Assembly);

app.Run();

// Helper
static Guid GetUserId(HttpContext ctx) =>
    Guid.Parse(ctx.User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier)!);

static string FormatTimeControl(int baseSeconds, int increment)
{
    return baseSeconds switch
    {
        60 => "Bullet",
        180 => increment > 0 ? $"Blitz {baseSeconds}+{increment}" : "Blitz",
        600 => increment > 0 ? $"Rapid {baseSeconds}+{increment}" : "Rapid",
        3600 => "Classic",
        _ => increment > 0 ? $"{baseSeconds}+{increment}" : $"{baseSeconds}"
    };
}

public sealed class RoomListItemDto
{
    public Guid RoomId { get; init; }
    public Guid HostId { get; init; }
    public string TimeControl { get; init; } = string.Empty;
    public bool IsRated { get; init; }
    public DateTime CreatedAt { get; init; }
}

public sealed record DeleteAccountReq(string Confirmation);
