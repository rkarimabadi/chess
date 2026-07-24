# مستند معماری فنی — پیاده‌سازی پلتفرم شطرنج آنلاین

| فیلد | مقدار |
| :--- | :--- |
| **نسخه** | 1.1.0 |
| **تاریخ** | ۱۴۰۵/۰۵/۰۱ (۲۳ ژوئیه ۲۰۲۶) |
| **وابستگی** | مستند نیازمندی (PRD) v2.4.0 |
| **زبان سند** | فارسی |
| **هدف** | ارائهٔ جزئیات فنی کامل برای پیاده‌سازی توسط تیم یا عامل هوش مصنوعی |

> **اصل:** این سند **«چگونه»** پیاده‌سازی کنیم را تعریف می‌کند. **«چرا»** و **«چیست»** در مستند نیازمندی (PRD) آمده است. هر تصمیم اینجا با شناسهٔ DEC-ARCH مشخص و با PRD cross-reference شده است.

> **تغییرات نسخهٔ ۱.۱.۰ (نسبت به ۱.۰.۰):** پوشش شکاف‌های شناسایی‌شده در بازبینی کامل‌بودن — دوستان/بلاک/وضعیت آنلاین (§۲.۲ `Friendship` و `UserBlock`)، تماشاگر/Spectator (§۴.۲ UseCase + §۷.۱ Hub + §۷.۴ گروه `spectators:{gameId}`)، نقطهٔ ورود گزارش بازیکن (§۴.۲ `SubmitReport` + §۷.۱ Hub + §۸.۲ REST — ARCH-13)، رویدادهای Lobby/Matchmaking روی SignalR (§۷.۱ متدهای `QueueJoined`/`MatchFound`/`RoomReady`/`OpponentJoinedRoom` — ARCH-14)، صفحات حقوقی (§۶.۲ `TermsPage`/`PrivacyPage`)، حذف/غیرفعال‌سازی حساب (§۴.۲ `DeactivateAccount`/`DeleteAccount` + §۸.۲ endpoints — F-ACC-07)، دسترس‌پذیری کیبورد صفحه (§۶.۴ roving tabindex + `aria-label` + `HandleKeyDown`)، Observability (§۱۲ `IMetricsService` + `SecurityAuditLogger`)، سیاست دقیق قطع دوطرفه (§۵.۴ `DisconnectWatchdogService` + ARCH-15)، سیستم صدا (§۱۳ `ISoundService` — ARCH-16)، مهلت ضد‌رهاسازی حالت آزاد (§۵.۷ `IIdleAbandonTimer` — ARCH-17)، تصمیم‌های ARCH-11 تا ARCH-17 (§۱.۱)، و پیوست Traceability (پیوست ج).

---

# فهرست مطالب

1. [بستن سوالات فنی باز (DEC-ARCH)](#۱-بستن-سوالات-فنی-باز-dec-arch)
2. [مدل دامنه (C#)](#۲-مدل-دامنه-c)
3. [معماری موتور شطرنج](#۳-معماری-موتور-شطرنج)
4. [لایهٔ Application (Use Cases)](#۴-لایه-application-use-cases)
5. [لایهٔ Infrastructure و Schema دیتابیس](#۵-لایه-infrastructure-و-schema-دیتابیس)
6. [لایهٔ Presentation — Blazor](#۶-لایه-presentation-blazor)
7. [طراحی SignalR Hubs](#۷-طراحی-signalr-hubs)
8. [طراحی REST API](#۸-طراحی-rest-api)
9. [پیاده‌سازی PWA](#۹-پیاده‌سازی-pwa)
10. [تایپوگرافی و تم‌پذیری](#۱۰-تایپوگرافی-و-تمپذیری)
11. [احراز هویت و امنیت](#۱۱-احراز-هویت-و-امنیت)
12. [Observability و Analytics](#۱۲-observability-و-analytics)
13. [سیستم صدا (Client-side)](#۱۳-سیستم-صدا-client-side)
14. [نقشهٔ ساخت (Build Sequence)](#۱۴-نقشه-ساخت-build-sequence)
15. [پیوست ج — Traceability به کاتالوگ قابلیت‌های PRD](#پیوست-ج--traceability-به-کاتالوگ-قابلیتهای-prd)
16. [پیوست — فایل‌بندی Solution](#پیوست--فایل‌بندی-solution)

---

## ۱. بستن سوالات فنی باز (DEC-ARCH)

> این سوالات در PRD بخش ۳۰.۱۲ (TQ-01 تا TQ-10) باز اعلام شده بودند. در اینجا قطعی بسته می‌شوند.

### ۱.۱ فهرست تصمیم‌ها

| ID | سوال (PRD) | تصمیم | دلیل |
| :---: | :--- | :--- | :--- |
| **ARCH-01** | TQ-01: Blazor render mode | **Interactive WebAssembly** — کل صفحات روی WASM تعاملی | بازی نیاز به تعامل غنی کلاینت (drag & drop, انیمیشن, state لحظه‌ای) دارد؛ WASM تعاملی بهترین تجربه را می‌دهد |
| **ARCH-02** | TQ-02: Auth مکانیسم | **Cookie Auth + Anti-forgery token** | هاست مشترک (TECH-08)؛ ساده‌ترین مدل؛ سازگار با SignalR (کوکی خودکار با اتصال) |
| **ARCH-03** | TQ-03: Data access | **EF Core** (ارجحیت) | سرعت توسعه در فاز اول؛ Dapper فقط اگر profiling نشان دهد bottleneck |
| **ARCH-04** | TQ-04: محل نگهداری Game state زنده | **In-memory ConcurrentDictionary + Snapshot دوره‌ای به DB** | سرعت خواندن در حین بازی؛ بازیابی crash از آخرین snapshot |
| **ARCH-05** | TQ-05: Client legal-move generator | **بله — سبک، فقط برای UX** | کاهش latency درک‌شده؛ سرور همچنان حاکم است |
| **ARCH-06** | TQ-06: Production DB | **PostgreSQL** (وقتی نیاز شد) | SQLite فقط dev (PRD TECH-03)؛ PostgreSQL بهترین گزینهٔ رایگان و قابل اتکا |
| **ARCH-07** | TQ-07: Moderator permaban | **خیر — فقط Admin** | هم‌راستا با DEC-28 |
| **ARCH-08** | TQ-08: دستی ELO | **خیر در MVP** | هم‌راستا با PRD |
| **ARCH-09** | TQ-09: تعداد Hubها | **۲ Hub: GameHub + StaffHub** | جداسازی مسئولیت؛ StaffHub سیاست auth جداگانه دارد |
| **ARCH-10** | TQ-10: Hub protocol | **Strongly-typed Hubs** (`IGameHub`, `IStaffHub`) | ایمنی کامپایل؛ بدون magic string |
| **ARCH-11** | پوشش دوستان/بلاک/وضعیت آنلاین (F-SOC-03..07, PRD §۱۲.۲) | **مدل دامنهٔ حداقلی از فاز ۰ + UI در فاز اجتماعی بعدی** | این‌ها Could هستند اما PRD جزو «ایدهٔ کامل» می‌داند؛ نبود Entity باعث بازنویسی Schema در آینده می‌شود |
| **ARCH-12** | پوشش تماشاگر (F-SPEC-01..03) | **گروه SignalR جدا (`spectators:{gameId}`) + Use Case از ابتدا، UI بعداً** | افزودن گروه بعد از عرضه یعنی تغییر Hub قراردادی؛ ارزان‌تر است از روز اول باشد |
| **ARCH-13** | نقطهٔ ورود گزارش بازیکن (F-SOC-06 — MVP) | **هم REST (`POST /api/reports`) هم SignalR (`SubmitReport` در GameHub)** | گزارش می‌تواند از صفحهٔ بازی زنده (SignalR) یا تاریخچه/پروفایل (REST) ثبت شود |
| **ARCH-14** | رویدادهای Lobby/Matchmaking روی SignalR (PRD §۱۳.۱.۱ — الزام ✅ MVP) | **افزوده‌شدن به `IGameHub`** به‌جای Hub جداگانه | یک اتصال SignalR واحد به ازای کاربر کافی است؛ Hub جدا پیچیدگی بی‌دلیل اضافه می‌کند |
| **ARCH-15** | سیاست دقیق قطع دوطرفه (DEC-15) و تداوم ساعت (DEC-16) | **Timer داخل `IGameStateManager` + فیلدهای `AbortDeadline`** | باید صریح در state زندهٔ بازی مدل شود، نه فقط قرارداد کلامی |
| **ARCH-16** | صدا (F-FB-03, F-UI-05) | **Client-side فقط؛ بدون منطق سرور؛ فایل‌های استاتیک در `wwwroot/sounds/`** | صدا کاملاً UX است و نباید به Domain/Application نشت کند (اصل ۷ PRD) |
| **ARCH-17** | مهلت ضد‌رهاسازی حالت Untimed (F-VAR-05, DEC-02) | **`IdleAbandonTimer` مجزا از `IClockService`** | بازی آزاد ساعت ندارد؛ نیاز به تایمر idle مستقل به‌جای اورلود کردن ClockService |

### ۱.۲ خلاصهٔ استک نهایی

```
┌──────────────────────────────────────────────────────────────────┐
│               ASP.NET Core 9.0 Host (تک‌پروسس)                   │
│                                                                    │
│  ┌─────────────────────────┐   ┌────────────────────────────┐    │
│  │ Blazor WebApp (WASM)     │   │ Minimal API + Controllers   │    │
│  │ Interactive WebAssembly   │   │ + GameHub (Strongly-typed)  │    │
│  │ + CSS Isolation           │   │ + StaffHub (Strongly-typed)  │    │
│  │ + Theme Tokens            │   │ + Cookie Auth               │    │
│  │ + Persian Font + Icons    │   │ + Anti-forgery              │    │
│  │ + PWA (SW + Manifest)     │   │                             │    │
│  └────────────▲─────────────┘   └──────────────▲──────────────┘    │
│               │ HTTP + SignalR (same origin)    │                   │
│  ┌────────────┴────────────────────────────────┴──────────────┐    │
│  │ Application Layer                                           │    │
│  │ UseCase handlers + DTOs + Permission checks + Mapping       │    │
│  ├────────────────────────────────────────────────────────────┤    │
│  │ Domain Layer                                                │    │
│  │ Chess Engine (IRuleSet) + Game Aggregate + Rating + Invariant│    │
│  ├────────────────────────────────────────────────────────────┤    │
│  │ Infrastructure Layer                                        │    │
│  │ EF Core (SQLite dev) + Repos + LiveGameState + Email + Time │    │
│  └────────────────────────────────────────────────────────────┘    │
└──────────────────────────────────────────────────────────────────┘
```

---

## ۲. مدل دامنه (C#)

### ۲.۱ نمایش صفحه (Board Representation)

صفحهٔ شطرنج با یک آرایهٔ ۶۴ عنصری از `Piece?` نمایش داده می‌شود.

```csharp
// Chess.Domain/ValueObjects/Piece.cs

public enum PieceColor { White, Black }

public enum PieceType { King, Queen, Rook, Bishop, Knight, Pawn }

public sealed record Piece(PieceColor Color, PieceType Type)
{
    public char ToChar() => Color switch
    {
        PieceColor.White => Type switch
        {
            PieceType.King => 'K', PieceType.Queen => 'Q',
            PieceType.Rook => 'R', PieceType.Bishop => 'B',
            PieceType.Knight => 'N', PieceType.Pawn => 'P',
            _ => '?'
        },
        _ => Type switch
        {
            PieceType.King => 'k', PieceType.Queen => 'q',
            PieceType.Rook => 'r', PieceType.Bishop => 'b',
            PieceType.Knight => 'n', PieceType.Pawn => 'p',
            _ => '?'
        }
    };
}
```

```csharp
// Chess.Domain/ValueObjects/Square.cs

public sealed record Square(int File, int Rank) // File: 0-7 (a-h), Rank: 0-7 (1-8)
{
    public bool IsValid => File is >= 0 and < 8 && Rank is >= 0 and < 8;
    public string ToAlgebraic() => $"{(char)('a' + File)}{Rank + 1}";
    
    public static Square Parse(string algebraic) => new(
        algebraic[0] - 'a',
        algebraic[1] - '1'
    );
}
```

```csharp
// Chess.Domain/ValueObjects/BoardState.cs

/// <summary>
/// وضعیت کامل صفحه — معادل FEN + اطلاعات اضافی.
/// منبع حقیقت وضعیت بازی در Domain Layer.
/// </summary>
public sealed class BoardState
{
    // آرایهٔ ۸×۸ — [rank, file] یا [row, col]
    // null = خانهٔ خالی
    private readonly Piece?[,] _squares = new Piece?[8, 8];

    public PieceColor CurrentTurn { get; private set; }
    
    // حقوق قلعه‌روی (Castle Rights)
    public bool WhiteKingSide { get; private set; } = true;
    public bool WhiteQueenSide { get; private set; } = true;
    public bool BlackKingSide { get; private set; } = true;
    public bool BlackQueenSide { get; private set; } = true;
    
    // هدف En Passant — null یعنی هیچ
    public Square? EnPassantTarget { get; private set; }
    
    // شمارنده‌ها
    public int HalfmoveClock { get; private set; } // قانون ۵۰ حرکت
    public int FullmoveNumber { get; private set; } = 1;
    
    // تاریخچهٔ FEN برای تشخیص تکرار سه‌باره
    public List<string> PositionHistory { get; } = new();

    public Piece? GetPiece(Square sq) => _squares[sq.Rank, sq.File];
    
    public void SetPiece(Square sq, Piece? piece) => _squares[sq.Rank, sq.File] = piece;

    public void MovePiece(Square from, Square to)
    {
        _squares[to.Rank, to.File] = _squares[from.Rank, from.File];
        _squares[from.Rank, from.File] = null;
    }

    public void SwitchTurn() => CurrentTurn = CurrentTurn == PieceColor.White
        ? PieceColor.Black : PieceColor.White;

    /// <summary>
    /// تولید FEN از وضعیت فعلی — برای ذخیره و تشخیص تکرار.
    /// </summary>
    public string ToFen() { /* پیاده‌سازی استاندارد FEN */ }
    
    /// <summary>
    /// ساخت وضعیت از رشتهٔ FEN — برای بازیابی state.
    /// </summary>
    public static BoardState FromFen(string fen) { /* پیاده‌سازی */ }
    
    /// <summary>
    /// شروع بازی — چیدمان اولیهٔ استاندارد.
    /// </summary>
    public static BoardState Initial() => FromFen(
        "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1"
    );
}
```

### ۲.۲ موجودیت‌ها (Domain Entities)

#### User

```csharp
// Chess.Domain/Entities/User.cs

public enum UserRole { User, Moderator, Admin }
public enum UserStatus { Active, Banned, Deleted }

public sealed class User : AggregateRoot
{
    public Guid Id { get; private set; }
    public string Username { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public int Rating { get; private set; } = 1200;
    public int GamesPlayed { get; private set; }
    public UserRole Role { get; private set; } = UserRole.User;
    public UserStatus Status { get; private set; } = UserStatus.Active;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; private set; }

    // پیام‌ها
    public bool PresetMessagesMuted { get; private set; }
    public DateTime? PresetMessagesMuteEndsAt { get; private set; }
    
    // تغییرات سمت سرور — نه از کلاینت
    public void SetRating(int newRating) => Rating = newRating;
    public void IncrementGamesPlayed() => GamesPlayed++;
    public void SetRole(UserRole role) => Role = role;
    public void Ban() => Status = UserStatus.Banned;
    public void Unban() => Status = UserStatus.Active;
    public void MutePresetsUntil(DateTime until) {
        PresetMessagesMuted = true;
        PresetMessagesMuteEndsAt = until;
    }
}
```

#### Game

```csharp
// Chess.Domain/Entities/Game.cs

public enum GameStatus
{
    Created, WaitingForOpponent, ReadyCheck,
    Active, Aborted, Finished
}

public enum GameResult
{
    Ongoing,
    WhiteWins, BlackWins, Draw, Aborted
}

public enum ResultReason
{
    None, Checkmate, Resignation, Timeout,
    Disconnect, Stalemate, Agreement, ThreefoldRepetition,
    FiftyMoveRule, InsufficientMaterial, Abort
}

public sealed class Game : AggregateRoot
{
    public Guid Id { get; private set; }
    public Guid WhitePlayerId { get; private set; }
    public Guid BlackPlayerId { get; private set; }
    public GameStatus Status { get; private set; } = GameStatus.Created;
    public GameResult Result { get; private set; } = GameResult.Ongoing;
    public ResultReason Reason { get; private set; } = ResultReason.None;
    
    // نوع بازی (DEC-06: دعوت پیش‌فرض Casual)
    public bool IsRated { get; private set; }
    public string Variant { get; private set; } = "Classic"; // F-VAR-06
    
    // کنترل زمان
    public int BaseTimeSeconds { get; private set; }
    public int IncrementSeconds { get; private set; }
    
    // وضعیت صفحه
    public string CurrentFen { get; private set; } = BoardState.Initial().ToFen();
    public int HalfmoveClock { get; private set; }
    public int FullmoveNumber { get; private set; } = 1;
    
    // ساعت (باقیمانده به میلی‌ثانیه)
    public long WhiteTimeRemainingMs { get; private set; }
    public long BlackTimeRemainingMs { get; private set; }
    
    // تاریخچه
    public List<string> PositionHistory { get; private set; } = new(); // FENها
    public List<MoveRecord> MoveHistory { get; private set; } = new();
    
    // وضعیت اتصال
    public bool WhiteConnected { get; set; } = true;
    public bool BlackConnected { get; set; } = true;
    public DateTime? WhiteDisconnectedAt { get; set; }
    public DateTime? BlackDisconnectedAt { get; set; }
    
    // تساوی
    public bool DrawOfferPending { get; private set; }
    public Guid? DrawOfferedBy { get; private set; }
    
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; private set; }
    public DateTime? FinishedAt { get; private set; }
}
```

#### MoveRecord

```csharp
// Chess.Domain/ValueObjects/MoveRecord.cs

public sealed record MoveRecord(
    int MoveNumber,
    Square From,
    Square To,
    Piece Piece,
    Piece? CapturedPiece,
    string SanNotation,       // مثلاً "Nf3"
    string FenBefore,         // FEN قبل از حرکت
    string FenAfter,          // FEN بعد از حرکت
    bool IsCheck,
    bool IsCheckmate,
    bool IsCapture,
    bool IsCastleKingSide,
    bool IsCastleQueenSide,
    bool IsEnPassant,
    PieceType? PromotionPiece, // null اگر ترفیع نباشد
    DateTime Timestamp
);
```

#### RatingChange

```csharp
// Chess.Domain/Entities/RatingChange.cs

public sealed class RatingChange : Entity
{
    public Guid Id { get; private set; }
    public Guid PlayerId { get; private set; }
    public Guid GameId { get; private set; }
    public int OldRating { get; private set; }
    public int NewRating { get; private set; }
    public int K { get; private set; }
    public int Delta => NewRating - OldRating;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
}
```

#### Room

```csharp
// Chess.Domain/Entities/Room.cs

public enum RoomStatus { Waiting, Ready, Expired, Closed }

public sealed class Room : Entity
{
    public Guid Id { get; private set; }
    public Guid HostId { get; private set; }
    public string InviteCode { get; private set; } = string.Empty;
    public bool IsRated { get; private set; }    // DEC-06: پیش‌فرض Casual
    public int BaseTimeSeconds { get; private set; }
    public int IncrementSeconds { get; private set; }
    public RoomStatus Status { get; private set; } = RoomStatus.Waiting;
    public Guid? GuestId { get; private set; }
    public bool HostReady { get; private set; }
    public bool GuestReady { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; private set; }
}
```

#### PlayerReport

```csharp
// Chess.Domain/Entities/PlayerReport.cs

public enum ReportStatus { Open, InReview, Resolved, Rejected, EscalatedToAdmin }
public enum ReportReason { IntentionalAbandon, SpamPresetMessage, InappropriateUsername, SuspicionOfCheating, Other }

public sealed class PlayerReport : Entity
{
    public Guid Id { get; private set; }
    public Guid ReporterId { get; private set; }
    public Guid TargetUserId { get; private set; }
    public Guid? GameId { get; private set; }
    public ReportReason Reason { get; private set; }
    public string? Note { get; private set; }
    public ReportStatus Status { get; private set; } = ReportStatus.Open;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public Guid? ResolvedByStaffId { get; private set; }
    public string? ResolutionNote { get; private set; }
    public DateTime? ResolvedAt { get; private set; }
}
```

#### UserSanction

```csharp
// Chess.Domain/Entities/UserSanction.cs

public enum SanctionType { Warn, MutePresets, TempBan, PermBan, ForceRename }

public sealed class UserSanction : Entity
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public SanctionType Type { get; private set; }
    public string Reason { get; private set; } = string.Empty; // اجباری (DEC-25)
    public Guid CreatedByStaffId { get; private set; }
    public DateTime StartsAt { get; private set; }
    public DateTime? EndsAt { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    
    // DEC-28: سقف Moderator بن موقت ≤ ۳۰ روز
    public bool IsExpired => EndsAt.HasValue && DateTime.UtcNow > EndsAt.Value;
}
```

#### StaffAuditLog

```csharp
// Chess.Domain/Entities/StaffAuditLog.cs

public sealed class StaffAuditLog : Entity
{
    public Guid Id { get; private set; }
    public Guid ActorStaffId { get; private set; }
    public string ActionType { get; private set; } = string.Empty;
    public string TargetType { get; private set; } = string.Empty; // "User", "Game", "Report"
    public Guid TargetId { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public string? DetailsJson { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
}
```

#### Friendship (ARCH-11 — F-SOC-03,04,05)

```csharp
// Chess.Domain/Entities/Friendship.cs

public enum FriendshipStatus { Pending, Accepted, Declined }

public sealed class Friendship : Entity
{
    public Guid Id { get; private set; }
    public Guid RequesterId { get; private set; }
    public Guid AddresseeId { get; private set; }
    public FriendshipStatus Status { get; private set; } = FriendshipStatus.Pending;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? RespondedAt { get; private set; }
}
```

> **یادداشت:** «وضعیت آنلاین/درحال‌بازی/آفلاین» (F-SOC-05) در دیتابیس persist نمی‌شود — یک state زودگذر است که از طریق اتصال فعال SignalR (`user:{userId}` group، بخش ۷.۴) و `IPresenceTracker` در حافظه محاسبه می‌شود، مشابه `LiveGameState`.

#### UserBlock (ARCH-11 — F-SOC-07)

```csharp
// Chess.Domain/Entities/UserBlock.cs

public sealed class UserBlock : Entity
{
    public Guid Id { get; private set; }
    public Guid BlockerId { get; private set; }
    public Guid BlockedUserId { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
}
```

> بلاک باعث می‌شود Matchmaking/دعوت/پیام آماده بین دو طرف رد شود (چک در `MatchmakingService` و `CreateRoom`/`SendPresetMessage`).

### ۲.۳ Aggregate Roots و Entity Base

```csharp
// Chess.Domain/Common/Entity.cs

public abstract class Entity
{
    public Guid Id { get; protected set; }
}

public abstract class AggregateRoot : Entity
{
    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    
    protected void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
    public void ClearDomainEvents() => _domainEvents.Clear();
}

public interface IDomainEvent { DateTime OccurredAt { get; } }
```

### ۲.۴ رویدادهای دامنه

```csharp
// Chess.Domain/Events/

public record GameCreatedEvent(Guid GameId, Guid WhiteId, Guid BlackId, bool IsRated) : IDomainEvent;
public record GameStartedEvent(Guid GameId) : IDomainEvent;
public record MoveAcceptedEvent(Guid GameId, MoveRecord Move, PieceColor Turn) : IDomainEvent;
public record MoveRejectedEvent(Guid GameId, string Reason) : IDomainEvent;
public record CheckDetectedEvent(Guid GameId, PieceColor CheckedSide) : IDomainEvent;
public record CheckmateEvent(Guid GameId, PieceColor Winner) : IDomainEvent;
public record StalemateEvent(Guid GameId) : IDomainEvent;
public record GameFinishedEvent(Guid GameId, GameResult Result, ResultReason Reason, int? WhiteRatingDelta, int? BlackRatingDelta) : IDomainEvent;
public record ClockFlaggedEvent(Guid GameId, PieceColor FlaggedSide) : IDomainEvent;
public record DrawOfferedEvent(Guid GameId, Guid OfferedById) : IDomainEvent;
public record DrawRespondedEvent(Guid GameId, bool Accepted, Guid RespondedById) : IDomainEvent;
public record PlayerResignedEvent(Guid GameId, Guid ResignedById) : IDomainEvent;
public record PlayerDisconnectedEvent(Guid GameId, PieceColor Side) : IDomainEvent;
public record PlayerReconnectedEvent(Guid GameId, PieceColor Side) : IDomainEvent;
public record PresetMessageSentEvent(Guid GameId, Guid SenderId, string MessageKey) : IDomainEvent;
public record RematchOfferedEvent(Guid GameId, Guid OfferedById) : IDomainEvent;
public record RematchAcceptedEvent(Guid OldGameId, Guid NewGameId) : IDomainEvent;
```

---

## ۳. معماری موتور شطرنج

### ۳.۱ رابط RuleSet (Variant-Aware — F-VAR-06)

> این abstraction از روز اول وجود دارد. فقط ClassicRuleSet در MVP پیاده‌سازی می‌شود. واریانت‌های آینده بدون بازنویسی اضافه می‌شوند.

```csharp
// Chess.Domain/Chess/Rules/IRuleSet.cs

public enum MoveResultStatus { Legal, Illegal, Check, Checkmate, Stalemate, Draw }

public sealed class MoveResult
{
    public MoveResultStatus Status { get; init; }
    public string? SanNotation { get; init; }
    public string? Reason { get; init; }
}

public interface IRuleSet
{
    /// <summary>
    /// شناسهٔ واریانت (مثلاً "Classic", "Chess960").
    /// </summary>
    string VariantId { get; }
    
    /// <summary>
    /// تولید تمام حرکات قانونی برای یک رنگ.
    /// </summary>
    IReadOnlyList<Move> GetLegalMoves(BoardState board, PieceColor side);
    
    /// <summary>
    /// آیا شاه در کیش است؟
    /// </summary>
    bool IsInCheck(BoardState board, PieceColor side);
    
    /// <summary>
    /// آیا کیش‌مات است (rentz)?
    /// </summary>
    bool IsCheckmate(BoardState board);
    
    /// <summary>
    /// آیا پات (stalemate) است؟
    /// </summary>
    bool IsStalemate(BoardState board);
    
    /// <summary>
    /// تشخیص تساوی بر اساس قوانین: تکرار سه‌باره، ۵۰ حرکت، کمبود مهره.
    /// </summary>
    bool IsDrawByRules(BoardState board, IReadOnlyList<string> positionHistory);
    
    /// <summary>
    /// آیا قلعه‌روی مجاز است؟
    /// </summary>
    bool IsCastlingLegal(BoardState board, Square from, Square to);
    
    /// <summary>
    /// آیا آن‌پاسان مجاز است؟
    /// </summary>
    bool IsEnPassantLegal(BoardState board, Square from, Square to);
    
    /// <summary>
    /// ترفیع اجباری روی rank آخر.
    /// </summary>
    bool IsPromotionRequired(BoardState board, Square from, Square to);
    
    /// <summary>
    /// قطعه‌های مجاز برای ترفیع.
    /// </summary>
    IReadOnlyList<PieceType> GetPromotionChoices();
    
    /// <summary>
    /// اعتبارسنجی نهایی یک حرکت.
    /// </summary>
    MoveResult ValidateMove(BoardState board, Square from, Square to, PieceType? promotion = null);
}
```

### ۳.۲ ClassicRuleSet

```csharp
// Chess.Domain/Chess/Rules/ClassicRuleSet.cs

public sealed class ClassicRuleSet : IRuleSet
{
    public string VariantId => "Classic";
    
    public IReadOnlyList<PieceType> GetPromotionChoices() =>
        new[] { PieceType.Queen, PieceType.Rook, PieceType.Bishop, PieceType.Knight };
    
    public MoveResult ValidateMove(BoardState board, Square from, Square to, PieceType? promotion = null)
    {
        // 1. بررسی وجود مهره در خانهٔ مبدأ
        var piece = board.GetPiece(from);
        if (piece is null) return Illegal("خانهٔ مبدأ خالی است");
        if (piece.Color != board.CurrentTurn) return Illegal("نوبت شما نیست");
        
        // 2. بررسی وجود مهرهٔ خودی در مقصد
        var target = board.GetPiece(to);
        if (target?.Color == piece.Color) return Illegal("نمی‌توان مهرهٔ خودی را گرفت");
        
        // 3. اعتبارسنجی حرکت مخصوص هر نوع مهره
        if (!IsMovePatternValid(board, piece, from, to))
            return Illegal("الگوی حرکت مجاز نیست");
        
        // 4. بررسی بعد از حرکت: آیا شاه خودی در کیش می‌ماند/قرار می‌گیرد؟
        var simulated = SimulateMove(board, from, to);
        if (IsInCheck(simulated, piece.Color))
            return Illegal("این حرکت شاه شما را در کیش قرار می‌دهد");
        
        // 5. تولید SAN notation
        var san = GenerateSan(board, from, to, piece, target, promotion);
        var isCheck = IsInCheck(simulated, piece.Color.Opposite());
        var isMate = IsCheckmate(simulated);
        
        if (isMate) san += "#";
        else if (isCheck) san += "+";
        
        return new MoveResult
        {
            Status = isMate ? MoveResultStatus.Checkmate : isCheck ? MoveResultStatus.Check : MoveResultStatus.Legal,
            SanNotation = san
        };
    }
    
    // ... پیاده‌سازی الگوی حرکت هر مهره ...
}
```

### ۳.۳ MoveGenerator

```csharp
// Chess.Domain/Chess/MoveGenerator.cs

public static class MoveGenerator
{
    /// <summary>
    /// تمام حرکات غیرقانونی (بدون بررسی check) را تولید می‌کند.
    /// </summary>
    public static IEnumerable<Move> GetPseudoLegalMoves(BoardState board, PieceColor side) { ... }
    
    /// <summary>
    /// حرکات قانونی (با فیلتر check) را تولید می‌کند.
    /// </summary>
    public static IReadOnlyList<Move> GetLegalMoves(BoardState board, PieceColor side) { ... }
    
    // الگوهای حرکت هر مهره:
    private static IEnumerable<Move> GetPawnMoves(BoardState board, PieceColor side) { ... }
    private static IEnumerable<Move> GetKnightMoves(BoardState board, PieceColor side) { ... }
    private static IEnumerable<Move> GetBishopMoves(BoardState board, PieceColor side) { ... }
    private static IEnumerable<Move> GetRookMoves(BoardState board, PieceColor side) { ... }
    private static IEnumerable<Move> GetQueenMoves(BoardState board, PieceColor side) { ... }
    private static IEnumerable<Move> GetKingMoves(BoardState board, PieceColor side) { ... }
    
    // تشخیص Pin و Check
    public static bool IsSquareAttacked(BoardState board, Square square, PieceColor bySide) { ... }
    public static bool IsKingInCheck(BoardState board, PieceColor side) { ... }
}
```

```csharp
// Chess.Domain/Chess/Move.cs

public sealed record Move(
    Square From,
    Square To,
    Piece Piece,
    Piece? CapturedPiece,
    bool IsEnPassant,
    bool IsCastleKingSide,
    bool IsCastleQueenSide,
    PieceType? PromotionPiece
);
```

### ۳.۴ تشخیص تساوی

```csharp
// Chess.Domain/Chess/DrawDetector.cs

public static class DrawDetector
{
    /// <summary>
    /// قانون تکرار سه‌باره — بر اساس FEN کامل (موقعیت + نوبت + حقوق قلعه + en passant).
    /// </summary>
    public static bool IsThreefoldRepetition(IReadOnlyList<string> positionHistory)
    {
        var counts = positionHistory.GroupBy(f => f).Any(g => g.Count() >= 3);
        return counts;
    }
    
    /// <summary>
    /// قانون ۵۰ حرکت — بدون حرکت سرباز و بدون گرفتن.
    /// </summary>
    public static bool IsFiftyMoveRule(int halfmoveClock) => halfmoveClock >= 100; // ۵۰ حرکت = ۱۰۰ نیم‌حرکت
    
    /// <summary>
    /// کمبود مهره — K vs K، K+B vs K، K+N vs K و مشابه.
    /// </summary>
    public static bool IsInsufficientMaterial(BoardState board)
    {
        var pieces = board.GetAllPieces().ToList();
        if (pieces.Count == 2) return true; // K vs K
        if (pieces.Count == 3)
        {
            var nonKing = pieces.First(p => p.Piece.Type != PieceType.King);
            if (nonKing.Piece.Type is PieceType.Bishop or PieceType.Knight) return true;
        }
        return false;
    }
}
```

### ۳.۵ تفکیک Server / Client (ARCH-05)

| لایه | مسئولیت | جزئیات |
| :--- | :--- | :--- |
| **سرور (Domain)** | اعتبارسنجی نهایی، نتیجه‌گیری | `ClassicRuleSet.ValidateMove` + `DrawDetector` |
| **کلاینت (Blazor)** | هایلایت خانه‌های مجاز، پیش‌نمایش drag | نسخهٔ سبک `ClientMoveGenerator` (فقط خواندنی) |
| **تفاوت** | اگر کلاینت اشتباه کند | سرور با `MoveRejected` state جدید برمی‌گرداند → کلاینت rollback |

---

## ۴. لایهٔ Application (Use Cases)

### ۴.۱ الگوی کلی

```csharp
// Chess.Application/Common/IUseCase.cs

public interface IUseCase<TRequest, TResponse>
{
    Task<TResponse> ExecuteAsync(TRequest request, CancellationToken ct = default);
}

// Chess.Application/Common/UseCaseBase.cs

public abstract class UseCaseBase<TRequest, TResponse> : IUseCase<TRequest, TResponse>
{
    protected readonly IUnitOfWork UoW;
    
    protected UseCaseBase(IUnitOfWork uow) => UoW = uow;
    
    public abstract Task<TResponse> ExecuteAsync(TRequest request, CancellationToken ct);
}
```

### ۴.۲ فهرست Use Caseها

#### احراز هویت

| Use Case | Request DTO | Response DTO | مجوز |
| :--- | :--- | :--- | :--- |
| `RegisterUser` | `{ Username, Email, Password }` | `{ UserId, Token? }` | مهمان |
| `LoginUser` | `{ Login, Password }` | `{ UserId, Token? }` | مهمان |
| `RecoverPassword` | `{ Email }` | `{ Success }` | مهمان |
| `DeactivateAccount` | `{ UserId }` | `{ Success }` | کاربر — F-ACC-07 |
| `DeleteAccount` | `{ UserId, Confirmation }` | `{ Success }` | کاربر — F-ACC-07، اجرای نرم: `Status → Deleted` + zeroing PII طبق §۱۹ PRD |

#### بازی

| Use Case | Request DTO | Response DTO | مجوز |
| :--- | :--- | :--- | :--- |
| `CreateRoom` | `{ UserId, TimeControl, IsRated, ColorPreference? }` | `{ RoomId, InviteCode }` | کاربر |
| `JoinRoom` | `{ UserId, InviteCode }` | `{ RoomId, Opponent }` | کاربر |
| `JoinQueue` | `{ UserId, TimeControl, IsRated }` | `{ QueueId }` | کاربر |
| `LeaveQueue` | `{ UserId }` | `{ Success }` | کاربر |
| `StartGame` | `{ UserId, GameId }` | `{ Board, White, Black, Clocks }` | بازیکن |
| `MakeMove` | `{ UserId, GameId, From, To, Promotion? }` | `{ Result, Move, NewFen, Clocks }` | بازیکن |
| `OfferDraw` | `{ UserId, GameId }` | `{ Success }` | بازیکن |
| `RespondDraw` | `{ UserId, GameId, Accept }` | `{ Result }` | بازیکن |
| `ResignGame` | `{ UserId, GameId }` | `{ Result }` | بازیکن |
| `ProposeRematch` | `{ UserId, GameId }` | `{ Success }` | بازیکن |
| `AcceptRematch` | `{ UserId, GameId, RematchToken }` | `{ NewGameId }` | بازیکن |

#### اجتماعی

| Use Case | Request | Response | مجوز |
| :--- | :--- | :--- | :--- |
| `SendPresetMessage` | `{ UserId, GameId, MessageKey }` | `{ Success }` | بازیکن |
| `SubmitReport` | `{ ReporterId, TargetUserId, Reason, GameId?, Note? }` | `{ ReportId }` | کاربر — ورودی از REST **و** SignalR (ARCH-13، بخش ۷.۱ و ۸.۲) |
| `GetGameHistory` | `{ UserId, Page, Filter? }` | `{ Games[] }` | کاربر |
| `GetGameDetails` | `{ UserId, GameId }` | `{ Game, Moves[] }` | کاربر |
| `SendFriendRequest` | `{ UserId, TargetUserId }` | `{ FriendshipId }` | کاربر — F-SOC-03,04، رد می‌شود اگر `UserBlock` وجود دارد |
| `RespondFriendRequest` | `{ UserId, FriendshipId, Accept }` | `{ Success }` | کاربر |
| `RemoveFriend` | `{ UserId, FriendId }` | `{ Success }` | کاربر |
| `BlockUser` | `{ UserId, TargetUserId }` | `{ Success }` | کاربر — F-SOC-07، حذف خودکار Friendship موجود |
| `UnblockUser` | `{ UserId, TargetUserId }` | `{ Success }` | کاربر |
| `GetLiveSpectatableGames` | `{ UserId, Page }` | `{ Games[] }` | کاربر — F-SPEC-01، فقط بازی‌های Rated یا Public |
| `JoinAsSpectator` | `{ UserId, GameId }` | `{ Game, DelayedState }` | کاربر — F-SPEC-02، state با تأخیر ۳-۵ث (ARCH-12) |

#### Staff (فقط admin/moderator — ARCH-09)

| Use Case | Request | Response | مجوز |
| :--- | :--- | :--- | :--- |
| `GetStaffDashboard` | `{ StaffId }` | `{ Online, ActiveGames, QueueLength, OpenReports, RecentBans }` | Staff |
| `ListReports` | `{ StaffId, Status, Page }` | `{ Reports[] }` | Staff |
| `ResolveReport` | `{ StaffId, ReportId, Action, Note }` | `{ Success }` | Staff |
| `ApplySanction` | `{ StaffId, UserId, Type, Reason, Duration? }` | `{ SanctionId }` | Staff |
| `RemoveSanction` | `{ StaffId, SanctionId }` | `{ Success }` | Staff |
| `AssignRole` | `{ AdminId, UserId, Role }` | `{ Success }` | Admin |
| `ForceFinishGame` | `{ AdminId, GameId, Reason }` | `{ Success }` | Admin |
| `GetUserDossier` | `{ StaffId, UserId }` | `{ User, Sanctions[], Reports[], RecentGames }` | Staff |
| `GetAuditLog` | `{ AdminId, Filters }` | `{ Logs[] }` | Admin |

### ۴.۳ سیاست مجوز (Permission Checks)

```csharp
// Chess.Application/Common/Authorization/

public interface IPermissionChecker
{
    bool IsUser(Guid userId);
    bool IsStaff(Guid userId);
    bool IsAdmin(Guid userId);
    bool CanBan(Guid staffId);        // Moderator+ 
    bool CanPermBan(Guid staffId);    // Admin only (ARCH-07)
    bool CanManageRoles(Guid staffId); // Admin only (DEC-27)
    bool CanViewFullAudit(Guid staffId); // Admin only
}

// هر UseCase در ابتدای ExecuteAsync:
// - آیا کاربر مجاز است؟
// - آیا وضعیت فعلی اجازه می‌دهد؟
// - سپس اجرا
```

---

## ۵. لایهٔ Infrastructure و Schema دیتابیس

### ۵.۱ DbContext

```csharp
// Chess.Infrastructure/Data/ChessDbContext.cs

public sealed class ChessDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Game> Games => Set<Game>();
    public DbSet<MoveRecord> MoveRecords => Set<MoveRecord>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<RatingChange> RatingChanges => Set<RatingChange>();
    public DbSet<PlayerReport> PlayerReports => Set<PlayerReport>();
    public DbSet<UserSanction> UserSanctions => Set<UserSanction>();
    public DbSet<StaffAuditLog> StaffAuditLogs => Set<StaffAuditLog>();
    public DbSet<Friendship> Friendships => Set<Friendship>();
    public DbSet<UserBlock> UserBlocks => Set<UserBlock>();
    public DbSet<StaffNote> StaffNotes => Set<StaffNote>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ── User ──
        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);
            e.HasIndex(u => u.Username).IsUnique();
            e.HasIndex(u => u.Email).IsUnique();
            e.HasIndex(u => u.Rating);
            e.HasIndex(u => u.Role);
            e.Property(u => u.Username).HasMaxLength(20);
            e.Property(u => u.Email).HasMaxLength(256);
            e.Property(u => u.PasswordHash).HasMaxLength(500);
            e.Property(u => u.Role).HasMaxLength(20).HasConversion<string>();
            e.Property(u => u.Status).HasMaxLength(20).HasConversion<string>();
        });

        // ── Game ──
        modelBuilder.Entity<Game>(e =>
        {
            e.HasKey(g => g.Id);
            e.HasIndex(g => g.WhitePlayerId);
            e.HasIndex(g => g.BlackPlayerId);
            e.HasIndex(g => g.Status).HasConversion<string>();
            e.HasIndex(g => g.CreatedAt);
            e.Property(g => g.Status).HasMaxLength(20).HasConversion<string>();
            e.Property(g => g.Result).HasMaxLength(20).HasConversion<string>();
            e.Property(g => g.Reason).HasMaxLength(30).HasConversion<string>();
            e.Property(g => g.Variant).HasMaxLength(20);
            e.Property(g => g.CurrentFen).HasMaxLength(200);
            e.Property(g => g.PositionHistoryJson).HasMaxLength(4000); // JSON array of FENs
        });

        // ── MoveRecord ──
        modelBuilder.Entity<MoveRecord>(e =>
        {
            e.HasKey(m => m.Id);
            e.HasIndex(m => new { m.GameId, m.MoveNumber });
            e.Property(m => m.FromSquare).HasMaxLength(2);
            e.Property(m => m.ToSquare).HasMaxLength(2);
            e.Property(m => m.SanNotation).HasMaxLength(10);
            e.Property(m => m.FenBefore).HasMaxLength(200);
            e.Property(m => m.FenAfter).HasMaxLength(200);
        });

        // ── Room ──
        modelBuilder.Entity<Room>(e =>
        {
            e.HasKey(r => r.Id);
            e.HasIndex(r => r.InviteCode).IsUnique();
            e.HasIndex(r => r.Status).HasConversion<string>();
            e.HasIndex(r => r.ExpiresAt);
            e.Property(r => r.InviteCode).HasMaxLength(10);
            e.Property(r => r.Status).HasMaxLength(20).HasConversion<string>();
        });

        // ── RatingChange ──
        modelBuilder.Entity<RatingChange>(e =>
        {
            e.HasKey(r => r.Id);
            e.HasIndex(r => r.PlayerId);
            e.HasIndex(r => r.GameId);
        });

        // ── PlayerReport ──
        modelBuilder.Entity<PlayerReport>(e =>
        {
            e.HasKey(r => r.Id);
            e.HasIndex(r => r.Status).HasConversion<string>();
            e.HasIndex(r => r.TargetUserId);
            e.HasIndex(r => r.CreatedAt);
            e.Property(r => r.Reason).HasMaxLength(30).HasConversion<string>();
            e.Property(r => r.Note).HasMaxLength(500);
            e.Property(r => r.Status).HasMaxLength(30).HasConversion<string>();
            e.Property(r => r.ResolutionNote).HasMaxLength(500);
        });

        // ── UserSanction ──
        modelBuilder.Entity<UserSanction>(e =>
        {
            e.HasKey(s => s.Id);
            e.HasIndex(s => new { s.UserId, s.IsActive });
            e.HasIndex(s => s.EndsAt);
            e.Property(s => s.Type).HasMaxLength(20).HasConversion<string>();
            e.Property(s => s.Reason).HasMaxLength(500);
        });

        // ── StaffAuditLog ──
        modelBuilder.Entity<StaffAuditLog>(e =>
        {
            e.HasKey(l => l.Id);
            e.HasIndex(l => l.ActorStaffId);
            e.HasIndex(l => new { l.TargetType, l.TargetId });
            e.HasIndex(l => l.CreatedAt);
            e.Property(l => l.ActionType).HasMaxLength(50);
            e.Property(l => l.TargetType).HasMaxLength(30);
            e.Property(l => l.Reason).HasMaxLength(500);
            e.Property(l => l.DetailsJson).HasMaxLength(4000);
        });

        // ── Friendship (ARCH-11) ──
        modelBuilder.Entity<Friendship>(e =>
        {
            e.HasKey(f => f.Id);
            e.HasIndex(f => new { f.RequesterId, f.AddresseeId }).IsUnique();
            e.HasIndex(f => new { f.AddresseeId, f.Status });
            e.Property(f => f.Status).HasMaxLength(20).HasConversion<string>();
        });

        // ── UserBlock (ARCH-11) ──
        modelBuilder.Entity<UserBlock>(e =>
        {
            e.HasKey(b => b.Id);
            e.HasIndex(b => new { b.BlockerId, b.BlockedUserId }).IsUnique();
        });

        // ── StaffNote (F-STAFF-30) ──
        modelBuilder.Entity<StaffNote>(e =>
        {
            e.HasKey(n => n.Id);
            e.HasIndex(n => n.UserId);
            e.Property(n => n.Body).HasMaxLength(2000);
        });
    }
}
```

### ۵.۱.۱ StaffNote Entity (F-STAFF-30 — یادداشت داخلی روی پروندهٔ کاربر)

```csharp
// Chess.Domain/Entities/StaffNote.cs

public sealed class StaffNote : Entity
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid AuthorStaffId { get; private set; }
    public string Body { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
}
```

#### جدول StaffNotes

| ستون | نوع | محدودیت | توضیح |
| :--- | :--- | :--- | :--- |
| `Id` | `UNIQUEIDENTIFIER` | PK | |
| `UserId` | `UNIQUEIDENTIFIER` | FK → Users, NOT NULL | کاربر هدف |
| `AuthorStaffId` | `UNIQUEIDENTIFIER` | FK → Users, NOT NULL | نویسنده Staff |
| `Body` | `NVARCHAR(2000)` | NOT NULL | متن یادداشت |
| `CreatedAt` | `DATETIME2` | NOT NULL | |

**ایندکس‌ها:** `IX_StaffNotes_UserId`

### ۵.۲ Schema جداول

#### جدول Users

| ستون | نوع | محدودیت | توضیح |
| :--- | :--- | :--- | :--- |
| `Id` | `UNIQUEIDENTIFIER` | PK, NOT NULL, DEFAULT NEWSEQUENTIALID() | |
| `Username` | `NVARCHAR(20)` | UNIQUE, NOT NULL | ۳–۲۰ کاراکتر |
| `Email` | `NVARCHAR(256)` | UNIQUE, NOT NULL | |
| `PasswordHash` | `NVARCHAR(MAX)` | NOT NULL | |
| `Rating` | `INT` | NOT NULL, DEFAULT 1200 | شروع ۱۲۰۰ |
| `GamesPlayed` | `INT` | NOT NULL, DEFAULT 0 | |
| `Role` | `NVARCHAR(20)` | NOT NULL, DEFAULT 'User' | User/Moderator/Admin |
| `Status` | `NVARCHAR(20)` | NOT NULL, DEFAULT 'Active' | Active/Banned/Deleted |
| `PresetMessagesMuted` | `BIT` | NOT NULL, DEFAULT 0 | |
| `PresetMessagesMuteEndsAt` | `DATETIME2` | NULLABLE | |
| `CreatedAt` | `DATETIME2` | NOT NULL, DEFAULT GETUTCDATE() | |
| `LastLoginAt` | `DATETIME2` | NULLABLE | |

**ایندکس‌ها:** `IX_Users_Username`, `IX_Users_Email`, `IX_Users_Rating`, `IX_Users_Role`

#### جدول Games

| ستون | نوع | محدودیت | توضیح |
| :--- | :--- | :--- | :--- |
| `Id` | `UNIQUEIDENTIFIER` | PK, NOT NULL | |
| `WhitePlayerId` | `UNIQUEIDENTIFIER` | FK → Users, NOT NULL | |
| `BlackPlayerId` | `UNIQUEIDENTIFIER` | FK → Users, NOT NULL | |
| `Status` | `NVARCHAR(20)` | NOT NULL | Created/Waiting/Ready/Active/Aborted/Finished |
| `Result` | `NVARCHAR(20)` | NOT NULL, DEFAULT 'Ongoing' | Ongoing/WhiteWins/BlackWins/Draw/Aborted |
| `Reason` | `NVARCHAR(30)` | NOT NULL, DEFAULT 'None' | ResultReason |
| `IsRated` | `BIT` | NOT NULL, DEFAULT 0 | |
| `Variant` | `NVARCHAR(20)` | NOT NULL, DEFAULT 'Classic' | |
| `BaseTimeSeconds` | `INT` | NOT NULL | |
| `IncrementSeconds` | `INT` | NOT NULL, DEFAULT 0 | |
| `CurrentFen` | `NVARCHAR(200)` | NOT NULL | |
| `HalfmoveClock` | `INT` | NOT NULL, DEFAULT 0 | |
| `FullmoveNumber` | `INT` | NOT NULL, DEFAULT 1 | |
| `WhiteTimeRemainingMs` | `BIGINT` | NOT NULL | میلی‌ثانیه |
| `BlackTimeRemainingMs` | `BIGINT` | NOT NULL | میلی‌ثانیه |
| `PositionHistoryJson` | `NVARCHAR(MAX)` | NOT NULL | JSON array of FENs |
| `DrawOfferPending` | `BIT` | NOT NULL, DEFAULT 0 | |
| `DrawOfferedById` | `UNIQUEIDENTIFIER` | NULLABLE | |
| `CreatedAt` | `DATETIME2` | NOT NULL, DEFAULT GETUTCDATE() | |
| `StartedAt` | `DATETIME2` | NULLABLE | |
| `FinishedAt` | `DATETIME2` | NULLABLE | |

**ایندکس‌ها:** `IX_Games_WhitePlayerId`, `IX_Games_BlackPlayerId`, `IX_Games_Status`, `IX_Games_CreatedAt`

#### جدول MoveRecords

| ستون | نوع | محدودیت | توضیح |
| :--- | :--- | :--- | :--- |
| `Id` | `UNIQUEIDENTIFIER` | PK, NOT NULL | |
| `GameId` | `UNIQUEIDENTIFIER` | FK → Games, NOT NULL | |
| `MoveNumber` | `INT` | NOT NULL | |
| `FromSquare` | `NVARCHAR(2)` | NOT NULL | مثلاً "e2" |
| `ToSquare` | `NVARCHAR(2)` | NOT NULL | مثلاً "e4" |
| `PieceChar` | `CHAR(1)` | NOT NULL | P/N/B/R/Q/K |
| `CapturedPieceChar` | `CHAR(1)` | NULLABLE | null اگر گرفتن نباشد |
| `SanNotation` | `NVARCHAR(10)` | NOT NULL | "Nf3", "O-O" و ... |
| `FenBefore` | `NVARCHAR(200)` | NOT NULL | |
| `FenAfter` | `NVARCHAR(200)` | NOT NULL | |
| `IsCheck` | `BIT` | NOT NULL, DEFAULT 0 | |
| `IsCheckmate` | `BIT` | NOT NULL, DEFAULT 0 | |
| `IsCapture` | `BIT` | NOT NULL, DEFAULT 0 | |
| `IsCastleKingSide` | `BIT` | NOT NULL, DEFAULT 0 | |
| `IsCastleQueenSide` | `BIT` | NOT NULL, DEFAULT 0 | |
| `IsEnPassant` | `BIT` | NOT NULL, DEFAULT 0 | |
| `PromotionPieceChar` | `CHAR(1)` | NULLABLE | null اگر ترفیع نباشد |
| `Timestamp` | `DATETIME2` | NOT NULL | |

**ایندکس:** `IX_MoveRecords_GameId_MoveNumber`

#### جدول Rooms

| ستون | نوع | محدودیت | توضیح |
| :--- | :--- | :--- | :--- |
| `Id` | `UNIQUEIDENTIFIER` | PK | |
| `HostId` | `UNIQUEIDENTIFIER` | FK → Users, NOT NULL | |
| `InviteCode` | `NVARCHAR(10)` | UNIQUE, NOT NULL | ۶–۸ کاراکتر |
| `IsRated` | `BIT` | NOT NULL, DEFAULT 0 | DEC-06 |
| `BaseTimeSeconds` | `INT` | NOT NULL | |
| `IncrementSeconds` | `INT` | NOT NULL, DEFAULT 0 | |
| `Status` | `NVARCHAR(20)` | NOT NULL, DEFAULT 'Waiting' | |
| `GuestId` | `UNIQUEIDENTIFIER` | NULLABLE, FK → Users | |
| `HostReady` | `BIT` | NOT NULL, DEFAULT 0 | |
| `GuestReady` | `BIT` | NOT NULL, DEFAULT 0 | |
| `CreatedAt` | `DATETIME2` | NOT NULL | |
| `ExpiresAt` | `DATETIME2` | NOT NULL | ۱۵ دقیقه بدون join / ۳۰ دقیقه بدون start |

**ایندکس‌ها:** `IX_Rooms_InviteCode` (UNIQUE), `IX_Rooms_Status`, `IX_Rooms_ExpiresAt`

#### جدول RatingChanges

| ستون | نوع | محدودیت | توضیح |
| :--- | :--- | :--- | :--- |
| `Id` | `UNIQUEIDENTIFIER` | PK | |
| `PlayerId` | `UNIQUEIDENTIFIER` | FK → Users, NOT NULL | |
| `GameId` | `UNIQUEIDENTIFIER` | FK → Games, NOT NULL | |
| `OldRating` | `INT` | NOT NULL | |
| `NewRating` | `INT` | NOT NULL | |
| `K` | `INT` | NOT NULL, DEFAULT 20 | |
| `CreatedAt` | `DATETIME2` | NOT NULL | |

**ایندکس‌ها:** `IX_RatingChanges_PlayerId`, `IX_RatingChanges_GameId`

#### جدول PlayerReports

| ستون | نوع | محدودیت | توضیح |
| :--- | :--- | :--- | :--- |
| `Id` | `UNIQUEIDENTIFIER` | PK | |
| `ReporterId` | `UNIQUEIDENTIFIER` | FK → Users, NOT NULL | |
| `TargetUserId` | `UNIQUEIDENTIFIER` | FK → Users, NOT NULL | |
| `GameId` | `UNIQUEIDENTIFIER` | NULLABLE, FK → Games | |
| `Reason` | `NVARCHAR(30)` | NOT NULL | |
| `Note` | `NVARCHAR(500)` | NULLABLE | |
| `Status` | `NVARCHAR(30)` | NOT NULL, DEFAULT 'Open' | |
| `CreatedAt` | `DATETIME2` | NOT NULL | |
| `ResolvedByStaffId` | `UNIQUEIDENTIFIER` | NULLABLE | |
| `ResolutionNote` | `NVARCHAR(500)` | NULLABLE | |
| `ResolvedAt` | `DATETIME2` | NULLABLE | |

**ایندکس‌ها:** `IX_PlayerReports_Status`, `IX_PlayerReports_TargetUserId`, `IX_PlayerReports_CreatedAt`

#### جدول UserSanctions

| ستون | نوع | محدودیت | توضیح |
| :--- | :--- | :--- | :--- |
| `Id` | `UNIQUEIDENTIFIER` | PK | |
| `UserId` | `UNIQUEIDENTIFIER` | FK → Users, NOT NULL | |
| `Type` | `NVARCHAR(20)` | NOT NULL | Warn/MutePresets/TempBan/PermBan/ForceRename |
| `Reason` | `NVARCHAR(500)` | NOT NULL | DEC-25: اجباری |
| `CreatedByStaffId` | `UNIQUEIDENTIFIER` | FK → Users, NOT NULL | |
| `StartsAt` | `DATETIME2` | NOT NULL | |
| `EndsAt` | `DATETIME2` | NULLABLE | null = دائم |
| `IsActive` | `BIT` | NOT NULL, DEFAULT 1 | |
| `CreatedAt` | `DATETIME2` | NOT NULL | |

**ایندکس‌ها:** `IX_UserSanctions_UserId_IsActive`, `IX_UserSanctions_EndsAt`

#### جدول StaffAuditLogs

| ستون | نوع | محدودیت | توضیح |
| :--- | :--- | :--- | :--- |
| `Id` | `UNIQUEIDENTIFIER` | PK | |
| `ActorStaffId` | `UNIQUEIDENTIFIER` | FK → Users, NOT NULL | |
| `ActionType` | `NVARCHAR(50)` | NOT NULL | Ban/Unban/Warn/Mute/AssignRole/ForceFinish/... |
| `TargetType` | `NVARCHAR(30)` | NOT NULL | User/Game/Report |
| `TargetId` | `UNIQUEIDENTIFIER` | NOT NULL | |
| `Reason` | `NVARCHAR(500)` | NOT NULL | |
| `DetailsJson` | `NVARCHAR(MAX)` | NULLABLE | قبل/بعد |
| `CreatedAt` | `DATETIME2` | NOT NULL | |

**ایندکس‌ها:** `IX_StaffAuditLogs_ActorStaffId`, `IX_StaffAuditLogs_TargetType_TargetId`, `IX_StaffAuditLogs_CreatedAt`

#### جدول Friendships (ARCH-11 — F-SOC-03,04)

| ستون | نوع | محدودیت | توضیح |
| :--- | :--- | :--- | :--- |
| `Id` | `UNIQUEIDENTIFIER` | PK | |
| `RequesterId` | `UNIQUEIDENTIFIER` | FK → Users, NOT NULL | |
| `AddresseeId` | `UNIQUEIDENTIFIER` | FK → Users, NOT NULL | |
| `Status` | `NVARCHAR(20)` | NOT NULL, DEFAULT 'Pending' | Pending/Accepted/Declined |
| `CreatedAt` | `DATETIME2` | NOT NULL | |
| `RespondedAt` | `DATETIME2` | NULLABLE | |

**ایندکس‌ها:** `UX_Friendships_RequesterId_AddresseeId` (یکتا، جفت مرتب‌شده)، `IX_Friendships_AddresseeId_Status`

#### جدول UserBlocks (ARCH-11 — F-SOC-07)

| ستون | نوع | محدودیت | توضیح |
| :--- | :--- | :--- | :--- |
| `Id` | `UNIQUEIDENTIFIER` | PK | |
| `BlockerId` | `UNIQUEIDENTIFIER` | FK → Users, NOT NULL | |
| `BlockedUserId` | `UNIQUEIDENTIFIER` | FK → Users, NOT NULL | |
| `CreatedAt` | `DATETIME2` | NOT NULL | |

**ایندکس‌ها:** `UX_UserBlocks_BlockerId_BlockedUserId` (یکتا)

### ۵.۳ Repository Interfaceها

```csharp
// Chess.Application/Ports/IRepositories.cs

public interface IUnitOfWork
{
    IUserRepository Users { get; }
    IGameRepository Games { get; }
    IMoveRepository Moves { get; }
    IRoomRepository Rooms { get; }
    IRatingRepository Ratings { get; }
    IReportRepository Reports { get; }
    ISanctionRepository Sanctions { get; }
    IAuditRepository Audit { get; }
    IFriendshipRepository Friendships { get; }
    IUserBlockRepository UserBlocks { get; }
    IStaffNoteRepository StaffNotes { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetByEmailAsync(string email);
    Task<bool> UsernameExistsAsync(string username);
    Task<bool> EmailExistsAsync(string email);
    Task AddAsync(User user);
    void Update(User user);
}

public interface IGameRepository
{
    Task<Game?> GetByIdAsync(Guid id);
    Task AddAsync(Game game);
    Task<IReadOnlyList<Game>> GetUserHistoryAsync(Guid userId, int page, int pageSize);
    Task<IReadOnlyList<Game>> GetActiveGamesAsync();
    void Update(Game game);
}

public interface IRoomRepository
{
    Task<Room?> GetByIdAsync(Guid id);
    Task<Room?> GetByInviteCodeAsync(string code);
    Task AddAsync(Room room);
    Task<int> CleanupExpiredAsync(); // حذف اتاق‌های منقضی
    void Update(Room room);
}

public interface IReportRepository
{
    Task<PlayerReport?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<PlayerReport>> GetOpenReportsAsync(int page, int pageSize);
    Task AddAsync(PlayerReport report);
    void Update(PlayerReport report);
}

public interface ISanctionRepository
{
    Task<IReadOnlyList<UserSanction>> GetActiveByUserIdAsync(Guid userId);
    Task<UserSanction?> GetByIdAsync(Guid id);
    Task AddAsync(UserSanction sanction);
    void Update(UserSanction sanction);
    Task<int> ExpireStaleBansAsync(); // غیرفعال کردن بن‌های منقضی
}

public interface IAuditRepository
{
    Task AddAsync(StaffAuditLog log);
    Task<IReadOnlyList<StaffAuditLog>> GetFilteredAsync(Guid? staffId, string? actionType, DateTime? from, DateTime to, int page, int pageSize);
}

public interface IFriendshipRepository
{
    Task<Friendship?> GetByIdAsync(Guid id);
    Task<Friendship?> GetBetweenAsync(Guid requesterId, Guid addresseeId);
    Task<IReadOnlyList<Friendship>> GetFriendsOfAsync(Guid userId);
    Task<IReadOnlyList<Friendship>> GetPendingRequestsForAsync(Guid userId);
    Task AddAsync(Friendship friendship);
    void Update(Friendship friendship);
}

public interface IUserBlockRepository
{
    Task<UserBlock?> GetAsync(Guid blockerId, Guid blockedUserId);
    Task<bool> IsBlockedAsync(Guid userId1, Guid userId2);
    Task<IReadOnlyList<UserBlock>> GetBlockedByAsync(Guid userId);
    Task AddAsync(UserBlock block);
    void Remove(UserBlock block);
}

public interface IStaffNoteRepository
{
    Task<IReadOnlyList<StaffNote>> GetByUserIdAsync(Guid userId);
    Task AddAsync(StaffNote note);
}
```

### ۵.۴ Live Game State Manager (ARCH-04)

```csharp
// Chess.Infrastructure/Services/IGameStateManager.cs

public interface IGameStateManager
{
    /// <summary>
    /// دریافت وضعیت زندهٔ بازی. اگر نبود، از DB بازیابی می‌کند.
    /// </summary>
    Task<LiveGameState?> GetAsync(Guid gameId);
    
    /// <summary>
    /// ذخیره یا به‌روزرسانی وضعیت زنده.
    /// </summary>
    Task UpsertAsync(Guid gameId, LiveGameState state);
    
    /// <summary>
    /// حذف بازی از حافظه (بعد از پایان و snapshot).
    /// </summary>
    Task RemoveAsync(Guid gameId);
    
    /// <summary>
    /// Snapshot دوره‌ای — بازی‌های فعال را به DB می‌نویسد.
    /// هر N دقیقه فراخوانی می‌شود (BackgroundService).
    /// </summary>
    Task SnapshotAllActiveAsync();
}

public sealed class LiveGameState
{
    public Guid GameId { get; set; }
    public BoardState Board { get; set; } = BoardState.Initial();
    public PieceColor CurrentTurn { get; set; }
    public long WhiteTimeMs { get; set; }
    public long BlackTimeMs { get; set; }
    public DateTime LastMoveAt { get; set; }
    public bool DrawOfferPending { get; set; }
    public List<string> PositionHistory { get; set; } = new();
    public List<MoveRecord> MoveHistory { get; set; } = new();
    public bool WhiteConnected { get; set; } = true;
    public bool BlackConnected { get; set; } = true;
    public DateTime? WhiteDisconnectedAt { get; set; }
    public DateTime? BlackDisconnectedAt { get; set; }

    // ARCH-15 — DEC-15 (Abort دوطرفه) و DEC-16 (تداوم ساعت هنگام قطع)
    public const int ReconnectTimeoutSeconds = 60; // §۱۳.۲ PRD: قطع >۶۰ث = باخت قطع ارتباط
    public bool BothDisconnected => !WhiteConnected && !BlackConnected;
    public DateTime? BothDisconnectedSince { get; set; } // اولین لحظه‌ای که هر دو طرف قطع بودند
}

/// <summary>
/// BackgroundService که هر چند ثانیه یک‌بار روی بازی‌های زنده اجرا می‌شود تا
/// سیاست قطع ارتباط را enforce کند (DEC-15/DEC-16). ساعت طبق DEC-16 حتی حین
/// قطعی یک طرف ادامه می‌یابد (در ServerClockService.Tick محاسبه می‌شود)؛ این
/// سرویس فقط دو حالت را چک می‌کند:
/// ۱) یک طرف >۶۰ث قطع و طرف مقابل متصل → باخت قطع ارتباط (Timeout) برای طرف قطع‌شده.
/// ۲) هر دو طرف هم‌زمان >۶۰ث قطع → DEC-15: Abort بدون تأثیر بر ELO.
/// </summary>
public sealed class DisconnectWatchdogService : BackgroundService
{
    private readonly IGameStateManager _stateManager;
    private readonly IServiceScopeFactory _scopeFactory;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await CheckAllActiveGamesAsync();
            await Task.Delay(TimeSpan.FromSeconds(5), ct); // پولینگ هر ۵ث کافی است
        }
    }

    private async Task CheckAllActiveGamesAsync()
    {
        // برای هر LiveGameState: اگر BothDisconnected و BothDisconnectedSince گذشته از
        // ReconnectTimeoutSeconds → فراخوانی معادل AbortGame (Reason = ResultReason.Disconnect,
        // Result = GameResult.Aborted, بدون RatingChange). اگر فقط یک طرف قطع و از حد گذشته →
        // فراخوانی پایان بازی با Reason = Timeout/Disconnect به نفع طرف متصل.
    }
}

/// <summary>
/// پیاده‌سازی در حافظه — ConcurrentDictionary + snapshot با BackgroundService.
/// </summary>
public sealed class InMemoryGameStateManager : IGameStateManager
{
    private readonly ConcurrentDictionary<Guid, LiveGameState> _states = new();
    private readonly IServiceScopeFactory _scopeFactory;
    
    public Task<LiveGameState?> GetAsync(Guid gameId)
    {
        _states.TryGetValue(gameId, out var state);
        return Task.FromResult(state);
    }
    
    public Task UpsertAsync(Guid gameId, LiveGameState state)
    {
        _states.AddOrUpdate(gameId, state, (_, _) => state);
        return Task.CompletedTask;
    }
    
    public Task RemoveAsync(Guid gameId)
    {
        _states.TryRemove(gameId, out _);
        return Task.CompletedTask;
    }
    
    public async Task SnapshotAllActiveAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ChessDbContext>();
        
        foreach (var (gameId, state) in _states)
        {
            var game = await db.Games.FindAsync(gameId);
            if (game is null || game.Status != GameStatus.Active) continue;
            
            game.CurrentFen = state.Board.ToFen();
            game.WhiteTimeRemainingMs = state.WhiteTimeMs;
            game.BlackTimeRemainingMs = state.BlackTimeMs;
            game.HalfmoveClock = state.Board.HalfmoveClock;
            game.FullmoveNumber = state.Board.FullmoveNumber;
            // ... سایر فیلدها
        }
        await db.SaveChangesAsync();
    }
}
```

### ۵.۵ Rating Service

```csharp
// Chess.Application/Services/IRatingService.cs

public interface IRatingService
{
    RatingResult Calculate(int whiteRating, int blackRating, GameResult result, bool isRated);
}

public sealed class EloRatingService : IRatingService
{
    private const int DefaultK = 20;
    private const int StartRating = 1200;
    
    public RatingResult Calculate(int whiteRating, int blackRating, GameResult result, bool isRated)
    {
        if (!isRated) return RatingResult.NoChange;
        
        var (whiteScore, blackScore) = result switch
        {
            GameResult.WhiteWins => (1.0, 0.0),
            GameResult.BlackWins => (0.0, 1.0),
            GameResult.Draw => (0.5, 0.5),
            _ => (0.0, 0.0)
        };
        
        var expectedWhite = 1.0 / (1.0 + Math.Pow(10, (blackRating - whiteRating) / 400.0));
        var expectedBlack = 1.0 - expectedWhite;
        
        var whiteDelta = (int)(DefaultK * (whiteScore - expectedWhite));
        var blackDelta = -whiteDelta;
        
        return new RatingResult
        {
            WhiteOldRating = whiteRating,
            WhiteNewRating = whiteRating + whiteDelta,
            WhiteDelta = whiteDelta,
            BlackOldRating = blackRating,
            BlackNewRating = blackRating + blackDelta,
            BlackDelta = blackDelta,
            K = DefaultK
        };
    }
}

public sealed record RatingResult
{
    public int WhiteOldRating { get; init; }
    public int WhiteNewRating { get; init; }
    public int WhiteDelta { get; init; }
    public int BlackOldRating { get; init; }
    public int BlackNewRating { get; init; }
    public int BlackDelta { get; init; }
    public int K { get; init; }
    
    public static RatingResult NoChange => new();
}
```

### ۵.۶ Game Clock Service

```csharp
// Chess.Application/Services/IClockService.cs

public interface IClockService
{
    /// <summary>
    /// بازگشت زمان باقیماندهٔ هر طرف (ms) بعد از محاسبهٔ اختلاف زمانی.
    /// </summary>
    ClockState Tick(LiveGameState state, PieceColor clockedSide, TimeSpan elapsed);
    
    /// <summary>
    /// آیا ساعت طرف مقابل تمام شده (flagged)?
    /// </summary>
    bool IsFlagged(ClockState clock, PieceColor side);
}

public sealed class ServerClockService : IClockService
{
    public ClockState Tick(LiveGameState state, PieceColor clockedSide, TimeSpan elapsed)
    {
        var remainingMs = clockedSide == PieceColor.White
            ? state.WhiteTimeMs : state.BlackTimeMs;
        
        remainingMs -= (long)elapsed.TotalMilliseconds;
        
        if (remainingMs <= 0)
        {
            remainingMs = 0;
            return new ClockState(state.WhiteTimeMs, state.BlackTimeMs)
            {
                FlaggedSide = clockedSide
            };
        }
        
        // اعمال فیشر اینکریمنت
        // remainingMs += increment; // فقط اگر حرکت معتبر باشد
        
        return new ClockState(
            clockedSide == PieceColor.White ? remainingMs : state.WhiteTimeMs,
            clockedSide == PieceColor.Black ? remainingMs : state.BlackTimeMs
        );
    }
    
    public bool IsFlagged(ClockState clock, PieceColor side) =>
        side == PieceColor.White ? clock.WhiteTimeMs <= 0 : clock.BlackTimeMs <= 0;
}

public sealed record ClockState(long WhiteTimeMs, long BlackTimeMs)
{
    public PieceColor? FlaggedSide { get; init; }
}
```

#### PieceColor Extension

```csharp
// Chess.Domain/ValueObjects/PieceColorExtensions.cs

public static class PieceColorExtensions
{
    public static PieceColor Opposite(this PieceColor color) =>
        color == PieceColor.White ? PieceColor.Black : PieceColor.White;
    
    public static string ToFenChar(this PieceColor color) =>
        color == PieceColor.White ? "w" : "b";
}
```

### ۵.۷ IdleAbandonTimer (ARCH-17 — F-VAR-05, DEC-02)

بازی **Untimed/Casual آزاد** ساعت شطرنجی ندارد، پس `IClockService` روی آن اعمال نمی‌شود. اما DEC-02 «مهلت ضد رهاسازی» می‌خواهد که این حالت هم در برابر ترک بازی محافظت شود. برای این حالت به‌جای اورلود کردن `IClockService`، یک تایمر idle مستقل تعریف می‌شود:

```csharp
// Chess.Application/Services/IIdleAbandonTimer.cs

public interface IIdleAbandonTimer
{
    /// <summary>
    /// اگر بازیکنِ نوبت‌دار در بازی Untimed برای مدت طولانی حرکتی نزند
    /// (نه لزوماً قطع اتصال؛ صرفاً بی‌عملی)، پس از سقف مشخص (پیشنهاد: ۵ دقیقه idle)
    /// هشدار و سپس (در صورت تداوم) بازی به‌عنوان Abandon به نفع طرف مقابل بسته می‌شود.
    /// </summary>
    void ResetTimer(Guid gameId, PieceColor sideToMove);
    Task<bool> HasExceededIdleLimitAsync(Guid gameId);
}
```

- این سرویس **مستقل** از `IClockService` است: در بازی‌های تایمردار، خودِ اتمام ساعت (Flag) کافی است و `IdleAbandonTimer` غیرفعال می‌ماند؛ فقط برای `Variant == "Untimed"` فعال می‌شود.
- پیاده‌سازی مشابه `DisconnectWatchdogService` — یک BackgroundService سبک که هر LiveGameState با `IsTimed == false` را چک می‌کند.

---

## ۶. لایهٔ Presentation — Blazor

### ۶.۱ Render Mode

```csharp
// Chess.Web/Program.cs

// Interactive WebAssembly — تمام صفحات روی WASM تعاملی (ARCH-01)
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();
```

### ۶.۲ نقشهٔ صفحات و کامپوننت‌ها

| مسیر | کامپوننت اصلی | فرزندان |
| :--- | :--- | :--- |
| `/` | `LandingPage` | `LandingHero`, `LandingFeatures`, `LandingCTA` |
| `/auth/login` | `LoginPage` | `AuthCard`, `AuthForm` |
| `/auth/register` | `RegisterPage` | `AuthCard`, `AuthForm` |
| `/auth/recover` | `RecoverPage` | `AuthCard`, `AuthForm` |
| `/home` | `DashboardPage` | `QuickPlayCard`, `InviteFriendCard`, `RecentGamesList`, `UserStatsSummary` |
| `/play/queue` | `QueuePage` | `QueueTimer`, `CancelQueueButton` |
| `/play/room/:id` | `RoomPage` | `RoomConfig`, `ReadyCheck`, `PlayerCard`, `InviteLink` |
| `/game/:id` | `GamePage` | **`ChessBoard`, `Square`, `PieceComponent`, `Clock`, `MoveList`, `PlayerCard`, `DrawOfferBanner`, `PresetChat`, `MaterialDisplay`, `GameStatusIndicator`** |
| `/game/:id/result` | `ResultPage` | `ResultOverlay`, `RatingDeltaCard`, `RematchCTA`, `ReviewCTA` |
| `/history` | `HistoryPage` | `GameList`, `GameFilter`, `PaginationControls` |
| `/history/:id` | `GameReviewPage` | `BoardReview`, `MoveNavigator`, `ReviewMoveList` |
| `/profile` | `ProfilePage` | `ProfileCard`, `StatsCard`, `SettingsLink` |
| `/settings` | `SettingsPage` | `ThemeSelector`, `SoundToggle`, `AccountActions` (شامل غیرفعال‌سازی/حذف حساب — F-ACC-07) |
| `/friends` | `FriendsPage` | `FriendList`, `FriendRequests`, `AddFriendBox`, `BlockedUsersList` (ARCH-11) |
| `/watch` | `SpectateListPage` | `LiveGamesList` (ARCH-12 — F-SPEC-01) |
| `/watch/:id` | `SpectatePage` | `BoardReview` (read-only)، `PlayerCard` ×۲ (ARCH-12 — F-SPEC-02) |
| `/terms` | `TermsPage` | متن ثابت — F-CNT-04 (MVP) |
| `/privacy` | `PrivacyPage` | متن ثابت — F-CNT-05 (MVP) |
| `/about` | `AboutPage` | متن ثابت — F-CNT-03 |
| `/faq` | `FaqPage` | `FaqList` — F-CNT-06 |
| `/staff` | `StaffDashboard` | `StatCard`, `QuickLinks` |
| `/staff/reports` | `ReportsQueue` | `ReportTable`, `ReportFilter`, `ReportRow` |
| `/staff/reports/:id` | `ReportDetail` | `ReportCard`, `UserProfile`, `SanctionDialog`, `ActionButtons` |
| `/staff/users` | `UserSearch` | `SearchBox`, `UserTable`, `UserRow` |
| `/staff/users/:id` | `UserDossier` | `UserCard`, `SanctionHistory`, `RecentGames`, `StaffNotes`, `SanctionDialog` |
| `/staff/games` | `ActiveGames` | `GameTable`, `GameRow` |
| `/staff/games/:id` | `GameReviewStaff` | `BoardReview`, `GameInfo`, `ForceFinishButton` |
| `/staff/audit` | `AuditLogPage` | `AuditTable`, `AuditFilter`, `AuditRow` |
| `/staff/roles` | `RoleManagement` | `UserSearch`, `RoleSelector`, `RoleChangeConfirm` |

### ۶.۳ مدیریت State

```csharp
// Chess.Web/Services/GameStateService.cs

/// <summary>
/// مدیریت state بازی در کلاینت — با SignalR sync می‌شود.
/// Source of truth: سرور. این فقط برای UI است.
/// </summary>
public sealed class GameStateService : IDisposable
{
    private readonly HubConnection _hubConnection;
    
    // وضعیت فعلی بازی (خواندنی توسط کامپوننت‌ها)
    public LiveGameState? CurrentState { get; private set; }
    public bool IsMyTurn => CurrentState?.CurrentTurn == MyColor;
    public PieceColor MyColor { get; private set; }
    public bool IsConnected => _hubConnection.State == HubConnectionState.Connected;
    
    // رویدادها برای کامپوننت‌ها
    public event Action? StateChanged;
    public event Action<MoveResult>? MoveResultReceived;
    public event Action<GameFinishedEvent>? GameFinished;
    public event Action<bool>? DrawOfferReceived; // true = accepted
    
    public async Task JoinGameAsync(Guid gameId)
    {
        await _hubConnection.SendAsync("JoinGame", gameId);
    }
    
    public async Task SendMoveAsync(Square from, Square to, PieceType? promotion = null)
    {
        // اعمال خوش‌بینانه (optimistic update) — اگر سرور رد کرد، rollback
        await _hubConnection.SendAsync("MakeMove", CurrentState?.GameId, from.ToAlgebraic(), to.ToAlgebraic(), promotion?.ToString());
    }
    
    public void Dispose()
    {
        _hubConnection.DisposeAsync().AsTask().Wait();
    }
}
```

### ۶.۴ کامپوننت ChessBoard (مهم‌ترین)

```razor
@* Chess.Web/Components/Game/ChessBoard.razor *@

<div class="chess-board @BoardOrientation" style="--board-size: @BoardSizePx;">
    @for (var rank = 7; rank >= 0; rank--)
    {
        @for (var file = 0; file < 8; file++)
        {
            var square = new Square(file, rank);
            <SquareComponent
                Square="square"
                Piece="GameState.Board.GetPiece(square)"
                IsSelected="@(SelectedSquare?.Equals(square) == true)"
                IsLegalTarget="@(LegalTargets.Contains(square))"
                IsCheckSquare="@IsCheckSquare(square)"
                IsLastMove="@(LastMove != null && (LastMove.From.Equals(square) || LastMove.To.Equals(square)))"
                IsKeyboardFocused="@(FocusedSquare.Equals(square))"
                ShowCoordinates="true"
                TabIndex="@(FocusedSquare.Equals(square) ? 0 : -1)"
                AriaLabel="@BuildAriaLabel(square)"
                OnClick="HandleSquareClick"
                OnDragStart="HandleDragStart"
                OnDrop="HandleDrop"
                OnKeyDown="HandleKeyDown" />
        }
    }
</div>

@code {
    // NFR-09 / PRD §۲۱.۳: چرخهٔ کامل انتخاب مهره/خانه با کیبورد — بدون این، تخته فقط با ماوس/تاچ قابل‌استفاده است
    private LiveGameState? GameState => GameStateService.CurrentState;
    private Square? SelectedSquare;
    private HashSet<Square> LegalTargets = new();
    private MoveRecord? LastMove => GameState?.MoveHistory.LastOrDefault();
    private Square FocusedSquare = new(0, 0); // خانهٔ فعلی roving tabindex

    private PieceColor BoardOrientation => GameStateService.MyColor == PieceColor.White
        ? PieceColor.White : PieceColor.Black;
    
    private bool IsCheckSquare(Square sq) =>
        GameState != null && GameState.Board.GetPiece(sq)?.Type == PieceType.King
        && GameState.Board.GetPiece(sq)?.Color == GameState.CurrentTurn
        && MoveGenerator.IsKingInCheck(GameState.Board, GameState.CurrentTurn);

    private string BuildAriaLabel(Square sq)
    {
        var piece = GameState?.Board.GetPiece(sq);
        var pieceLabel = piece is null ? "خالی" : $"{PieceNameFa(piece.Type)} {(piece.Color == PieceColor.White ? "سفید" : "سیاه")}";
        return $"خانهٔ {sq.ToAlgebraic()} — {pieceLabel}";
    }

    // جابه‌جایی فوکوس با کلیدهای جهت‌دار؛ Enter/Space برای انتخاب یا حرکت — «roving tabindex» استاندارد ARIA grid
    private void HandleKeyDown(Square current, KeyboardEventArgs e)
    {
        FocusedSquare = e.Key switch
        {
            "ArrowUp" => current with { Rank = Math.Min(7, current.Rank + 1) },
            "ArrowDown" => current with { Rank = Math.Max(0, current.Rank - 1) },
            "ArrowLeft" => current with { File = Math.Max(0, current.File - 1) },
            "ArrowRight" => current with { File = Math.Min(7, current.File + 1) },
            "Enter" or " " => current, // انتخاب/حرکت — همان مسیر HandleSquareClick
            _ => current
        };

        if (e.Key is "Enter" or " ") _ = HandleSquareClick(current);
    }

    private async Task HandleSquareClick(Square square)
    {
        if (!GameStateService.IsMyTurn) return;
        
        if (SelectedSquare == null)
        {
            // انتخاب مهره
            var piece = GameState?.Board.GetPiece(square);
            if (piece?.Color != GameStateService.MyColor) return;
            
            SelectedSquare = square;
            LegalTargets = MoveGenerator.GetLegalMoves(GameState!.Board, GameState.CurrentTurn)
                .Where(m => m.From.Equals(square))
                .Select(m => m.To)
                .ToHashSet();
        }
        else
        {
            // حرکت
            if (LegalTargets.Contains(square))
            {
                var promotion = MoveGenerator.IsPromotionRequired(GameState!.Board, SelectedSquare, square)
                    ? PieceType.Queen // UI باید انتخابگر نشان دهد — فعلاً Queen default
                    : (PieceType?)null;
                
                await GameStateService.SendMoveAsync(SelectedSquare, square, promotion);
            }
            
            SelectedSquare = null;
            LegalTargets.Clear();
        }
    }
}
```

### ۶.۵ CSS Architecture

```
wwwroot/
├── app.css                  ← Design Tokens (global variables)
├── app.pwa.css              ← PWA-specific overrides
├── Components/
│   ├── Game/
│   │   ├── ChessBoard.razor.css   ← scoped to ChessBoard
│   │   ├── Clock.razor.css        ← scoped to Clock
│   │   ├── MoveList.razor.css     ← scoped to MoveList
│   │   └── PresetChat.razor.css   ← scoped to PresetChat
│   ├── Layout/
│   │   └── MainLayout.razor.css
│   └── Staff/
│       ├── ReportTable.razor.css
│       └── StatCard.razor.css
└── themes/
    ├── dark.css              ← Data-theme="dark" overrides
    └── classic-skin.css      ← Classic piece/board skin
```

---

## ۷. طراحی SignalR Hubs

### ۷.۱ GameHub (ARCH-09: Strongly-typed — ARCH-10)

```csharp
// Chess.Web/Hubs/IGameHub.cs

public interface IGameHub
{
    // ── Server → Client: بازی ──
    Task GameStateChanged(GameStateDto state);
    Task MoveRejected(string reason, GameStateDto newState);
    Task CheckDetected(PieceColor checkedSide);
    Task GameFinished(GameResultDto result);
    Task DrawOffered(Guid offeredBy);
    Task DrawResponded(bool accepted, Guid respondedById);
    Task OpponentDisconnected(int secondsLeft);
    Task OpponentReconnected();
    Task PresetMessage(Guid senderId, string messageKey);
    Task RematchOffered(Guid offeredBy);
    Task RematchAccepted(string newGameId);
    Task PromotionRequired(string lastMoveFen); // منتظر انتخاب ترفیع

    // ── Server → Client: Lobby / Matchmaking (ARCH-14، PRD §۱۳.۱.۱ — ✅ MVP) ──
    Task QueueJoined(string queueId, int estimatedWaitSeconds);
    Task QueueLeft();
    Task MatchFound(string roomId, Guid opponentId, string timeControl);
    Task RoomReady(string roomId);            // هر دو طرف Ready شدند
    Task OpponentJoinedRoom(string roomId, Guid opponentId);

    // ── Server → Client: تماشاگر (ARCH-12 — F-SPEC) ──
    Task SpectatorStateChanged(GameStateDto delayedState); // با تأخیر ۳-۵ث

    // ── Client → Server: بازی ──
    Task JoinGame(Guid gameId);
    Task MakeMove(string gameId, string from, string to, string? promotion);
    Task OfferDraw(string gameId);
    Task RespondDraw(string gameId, bool accept);
    Task Resign(string gameId);
    Task SendPresetMessage(string gameId, string messageKey);
    Task ProposeRematch(string gameId);
    Task AcceptRematch(string gameId, string rematchToken);
    Task SendPromotionChoice(string gameId, string choice);

    // ── Client → Server: Lobby / Matchmaking ──
    Task JoinMatchmakingQueue(string timeControl, bool isRated);
    Task LeaveMatchmakingQueue();
    Task JoinRoomLive(string roomId);   // اتصال به گروه SignalR اتاق برای دریافت Ready/Opponent joined

    // ── Client → Server: تماشاگر و گزارش ──
    Task JoinAsSpectator(Guid gameId);           // ARCH-12
    Task SubmitReport(Guid targetUserId, string reason, Guid? gameId, string? note); // ARCH-13
}
```

> **نکته دربارهٔ ARCH-14:** endpointهای REST برای صف (`/api/matchmaking/join`, `/cancel`) هنوز پابرجا هستند و برای ثبت اولیهٔ تیکت استفاده می‌شوند (سازگار با ۱۳.۱.۲ PRD: «SignalR جایگزین HTTP در عملیات درخواست-پاسخ معمولی نیست»)؛ اما نتیجهٔ غیرهمزمان (`MatchFound`, `RoomReady`, ...) همیشه از طریق `IGameHub` broadcast می‌شود، نه response همان HTTP call.

### ۷.۲ GameHub پیاده‌سازی

```csharp
// Chess.Web/Hubs/GameHub.cs

[Authorize]
public class GameHub : Hub<IGameHub>, IGameHub
{
    private readonly IGameStateManager _stateManager;
    private readonly IRuleSet _ruleSet; // ClassicRuleSet
    private readonly IClockService _clockService;
    private readonly IRatingService _ratingService;
    private readonly IServiceScopeFactory _scopeFactory;
    
    public async Task JoinGame(Guid gameId)
    {
        var userId = GetUserId();
        var state = await _stateManager.GetAsync(gameId);
        if (state is null) return;
        
        var game = await LoadGame(gameId);
        var color = game.WhitePlayerId == userId ? PieceColor.White : PieceColor.Black;
        
        await Groups.AddToGroupAsync(Context.ConnectionId, gameId.ToString());
        
        // ذخیره رنگ در Context برای دسترسی سریع
        Context.Items["GameId"] = gameId;
        Context.Items["Color"] = color;
        
        // اطلاع‌رسانی اتصال
        await Clients.Group(gameId.ToString()).PlayerReconnected();
        
        // ارسال state فعلی
        await Clients.Caller.GameStateChanged(MapToDto(game, state, color));
    }
    
    public async Task MakeMove(string gameId, string from, string to, string? promotion)
    {
        var state = await _stateManager.GetAsync(Guid.Parse(gameId));
        if (state is null) return;
        
        var fromSq = Square.Parse(from);
        var toSq = Square.Parse(to);
        var promo = promotion != null ? Enum.Parse<PieceType>(promotion) : (PieceType?)null;
        
        // اعتبارسنجی نهایی سرور (BR-01)
        var result = _ruleSet.ValidateMove(state.Board, fromSq, toSq, promo);
        
        if (result.Status == MoveResultStatus.Illegal)
        {
            await Clients.Caller.MoveRejected(result.Reason!, MapToDto(...));
            return;
        }
        
        // اعمال حرکت
        var moveRecord = ApplyMove(state, fromSq, toSq, promo, result);
        state.MoveHistory.Add(moveRecord);
        state.PositionHistory.Add(state.Board.ToFen());
        state.CurrentTurn = state.CurrentTurn.Opposite();
        
        // اعمال اینکریمنت فیشر
        state.WhiteTimeMs += _clockService.ApplyIncrement(state, ...);
        
        await _stateManager.UpsertAsync(Guid.Parse(gameId), state);
        
        // broadcast به همه
        await Clients.Group(gameId.ToString()).GameStateChanged(MapToDto(...));
        
        // بررسی پایان
        await CheckGameEnd(state, gameId);
    }
    
    // ... سایر متدها مشابه
}
```

### ۷.۳ StaffHub

```csharp
// Chess.Web/Hubs/IStaffHub.cs

public interface IStaffHub
{
    // Server → Client
    Task DashboardUpdated(DashboardDto stats);
    Task NewReportSubmitted(ReportDto report);
    Task GameAborted(Guid gameId);
    Task UserBanned(Guid userId, string reason);
    
    // Client → Server
    Task JoinStaffGroup();
    Task SubscribeDashboard();
    Task SubscribeReportQueue();
}
```

### ۷.۴ گروه‌بندی (Groups)

| گروه | محتوا | مکانیسم |
| :--- | :--- | :--- |
| `game:{gameId}` | فقط دو بازیکن — دریافت state لحظه‌ای بدون تأخیر | `Groups.AddToGroupAsync` در `JoinGame` |
| `spectators:{gameId}` | تماشاگران — دریافت `SpectatorStateChanged` با تأخیر ۳-۵ث (ARCH-12، F-SPEC-02) | `Groups.AddToGroupAsync` در `JoinAsSpectator` |
| `user:{userId}` | نوتیفیکیشن‌های شخصی (نوبت، ...) | `Groups.AddToGroupAsync` هنگام اتصال |
| `staff` | همهٔ Staff — dashboard updates | `Groups.AddToGroupAsync` در `JoinStaffGroup` |
| `matchmaking:{ticketId}` | یک بازیکن خاص در صف — دریافت `MatchFound` شخصی (ARCH-14) | `Groups.AddToGroupAsync` در `JoinMatchmakingQueue` |
| `room:{roomId}` | بازیکنان یک اتاق دعوت — دریافت `OpponentJoinedRoom`/`RoomReady` (ARCH-14) | `Groups.AddToGroupAsync` در `JoinRoomLive` |

### ۷.۵ گذرگاه Matchmaking

```csharp
// Chess.Application/Services/IMatchmakingService.cs

public interface IMatchmakingService
{
    Task<MatchResult?> TryMatchAsync(Guid userId, int rating, string timeControl, bool isRated);
    Task CancelAsync(Guid userId);
}

public sealed class MatchmakingService : IMatchmakingService
{
    private readonly ConcurrentQueue<MatchTicket> _queue = new();
    private readonly int[] _ratingWindows = { 100, 150, 200, 250, 300, 350, 400 }; // گسترش تدریجی
    
    public async Task<MatchResult?> TryMatchAsync(Guid userId, int rating, string timeControl, bool isRated)
    {
        var ticket = new MatchTicket(userId, rating, timeControl, isRated, DateTime.UtcNow);
        
        // جست‌وجو در صف: همان timeControl + همان rated flag + بازهٔ ریتینگ
        foreach (var window in _ratingWindows)
        {
            var candidates = _queue.Where(t =>
                t.TimeControl == timeControl &&
                t.IsRated == isRated &&
                Math.Abs(t.Rating - rating) <= window &&
                t.UserId != userId
            ).ToList();
            
            if (candidates.Any())
            {
                var opponent = candidates.First();
                // حذف از صف و تشکیل بازی
                _queue.TryDequeue(out _);
                // ... ایجاد اتاق بازی
                return MatchResult.Found(opponent.UserId);
            }
        }
        
        return null; // در صف بمان
    }
}
```

---

## ۸. طراحی REST API

### ۸.۱ مکانیسم Auth

```csharp
// Program.cs

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/auth/login";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.Strict;
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("UserOnly", p => p.RequireRole("User", "Moderator", "Admin"))
    .AddPolicy("StaffOnly", p => p.RequireRole("Moderator", "Admin"))
    .AddPolicy("AdminOnly", p => p.RequireRole("Admin"))
    .AddPolicy("CanBan", p => p.RequireRole("Moderator", "Admin"))
    .AddPolicy("CanPermBan", p => p.RequireRole("Admin"))
    .AddPolicy("CanManageRoles", p => p.RequireRole("Admin"));
```

### ۸.۲ API Endpoints

```csharp
// Program.cs — Minimal API mappings

// ─── Auth ───
app.MapPost("/api/auth/register", RegisterEndpoint);      // [AllowAnonymous]
app.MapPost("/api/auth/login", LoginEndpoint);            // [AllowAnonymous]
app.MapPost("/api/auth/logout", LogoutEndpoint);          // [Authorize]
app.MapPost("/api/auth/recover", RecoverEndpoint);        // [AllowAnonymous]

// ─── User ───
app.MapGet("/api/users/me", GetProfileEndpoint);          // [Authorize("UserOnly")]
app.MapPut("/api/users/me", UpdateProfileEndpoint);       // [Authorize("UserOnly")]
app.MapPut("/api/users/me/password", ChangePasswordEndpoint); // [Authorize("UserOnly")]
app.MapPost("/api/users/me/deactivate", DeactivateAccountEndpoint); // [Authorize("UserOnly")] — F-ACC-07
app.MapDelete("/api/users/me", DeleteAccountEndpoint);         // [Authorize("UserOnly")] — F-ACC-07، نیازمند تأیید رمز

// ─── Game ───
app.MapGet("/api/games/{id}", GetGameEndpoint);           // [Authorize("UserOnly")]
app.MapGet("/api/history", GetHistoryEndpoint);           // [Authorize("UserOnly")]
app.MapGet("/api/history/{id}", GetGameDetailsEndpoint);  // [Authorize("UserOnly")]
app.MapGet("/api/games/live", GetLiveSpectatableGamesEndpoint); // [Authorize("UserOnly")] — F-SPEC-01 (ARCH-12)

// ─── Reports (ARCH-13) ───
app.MapPost("/api/reports", SubmitReportEndpoint);        // [Authorize("UserOnly")] — معادل REST برای SubmitReport؛
                                                            // نسخهٔ SignalR: GameHub.SubmitReport (بخش ۷.۱)

// ─── Friends & Blocks (ARCH-11) ───
app.MapGet("/api/friends", ListFriendsEndpoint);           // [Authorize("UserOnly")]
app.MapPost("/api/friends/requests", SendFriendRequestEndpoint); // [Authorize("UserOnly")]
app.MapPut("/api/friends/requests/{id}", RespondFriendRequestEndpoint); // [Authorize("UserOnly")]
app.MapDelete("/api/friends/{id}", RemoveFriendEndpoint);  // [Authorize("UserOnly")]
app.MapPost("/api/blocks", BlockUserEndpoint);             // [Authorize("UserOnly")]
app.MapDelete("/api/blocks/{userId}", UnblockUserEndpoint); // [Authorize("UserOnly")]

// ─── Matchmaking ───
app.MapPost("/api/matchmaking/join", JoinQueueEndpoint);  // [Authorize("UserOnly")]
app.MapDelete("/api/matchmaking/cancel", CancelQueueEndpoint); // [Authorize("UserOnly")]
// نکته: نتیجهٔ matchmaking (match found) از طریق این endpoint برنمی‌گردد؛
// broadcast لحظه‌ای روی SignalR انجام می‌شود — بخش ۷.۱، رویدادهای Lobby (ARCH-14)

// ─── Room ───
app.MapPost("/api/rooms", CreateRoomEndpoint);            // [Authorize("UserOnly")]
app.MapPost("/api/rooms/join", JoinRoomEndpoint);         // [Authorize("UserOnly")]
app.MapPut("/api/rooms/{id}/ready", ReadyEndpoint);       // [Authorize("UserOnly")]

// ─── Staff ───
app.MapGet("/api/staff/dashboard", GetDashboardEndpoint); // [Authorize("StaffOnly")]
app.MapGet("/api/staff/reports", ListReportsEndpoint);    // [Authorize("StaffOnly")]
app.MapPut("/api/staff/reports/{id}", ResolveReportEndpoint); // [Authorize("StaffOnly")]
app.MapGet("/api/staff/users/search", SearchUsersEndpoint); // [Authorize("StaffOnly")]
app.MapGet("/api/staff/users/{id}", GetUserDossierEndpoint); // [Authorize("StaffOnly")]
app.MapPost("/api/staff/sanctions", ApplySanctionEndpoint); // [Authorize("CanBan")]
app.MapDelete("/api/staff/sanctions/{id}", RemoveSanctionEndpoint); // [Authorize("StaffOnly")]
app.MapPost("/api/staff/roles", AssignRoleEndpoint);     // [Authorize("CanManageRoles")]
app.MapGet("/api/staff/audit", GetAuditLogEndpoint);     // [Authorize("AdminOnly")]
```

### ۸.۳ JSON Shapes (نمونه)

#### POST /api/auth/register

```json
// Request
{
    "username": "پارسا_شطرنج",
    "email": "parsa@example.com",
    "password": "MyP@ss123"
}

// Response 201
{
    "userId": "550e8400-e29b-41d4-a716-446655440000",
    "username": "پارسا_شطرنج",
    "rating": 1200
}
```

#### POST /api/auth/login

```json
// Request
{
    "login": "parsa@example.com",  // یا username
    "password": "MyP@ss123"
}

// Response 200
{
    "userId": "550e8400-...",
    "username": "پارسا_شطرنج",
    "rating": 1200,
    "role": "User"
}
```

#### GET /api/games/{id}

```json
// Response 200
{
    "gameId": "...",
    "status": "Active",
    "isRated": true,
    "variant": "Classic",
    "timeControl": { "base": 300, "increment": 5 },
    "white": { "id": "...", "username": "پارسا", "rating": 1250 },
    "black": { "id": "...", "username": "نگار", "rating": 1280 },
    "currentTurn": "White",
    "boardFen": "rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b KQkq e3 0 1",
    "whiteTimeMs": 295000,
    "blackTimeMs": 300000,
    "lastMove": { "from": "e2", "to": "e4", "san": "e4", "isCheck": false },
    "moveCount": 1,
    "drawOfferPending": false,
    "material": { "white": [], "black": [] }
}
```

#### POST /api/staff/sanctions

```json
// Request
{
    "userId": "...",
    "type": "TempBan",
    "reason": "ترک عمدی بازی ریت‌شده",
    "durationDays": 7
}

// Response 201
{
    "sanctionId": "...",
    "type": "TempBan",
    "endsAt": "2026-07-29T00:00:00Z"
}
```

---

## ۹. پیاده‌سازی PWA

### ۹.۱ manifest.json

```json
{
    "name": "شطرنج آنلاین — بازی PvP منصفانه",
    "short_name": "شطرنج",
    "description": "پلتفرم بازی شطرنج آنلاین PvP فارسی",
    "dir": "rtl",
    "lang": "fa-IR",
    "start_url": "/home",
    "scope": "/",
    "display": "standalone",
    "orientation": "any",
    "background_color": "#F8F9FA",
    "theme_color": "#2C5F8A",
    "categories": ["games", "entertainment"],
    "icons": [
        { "src": "/icons/icon-192.png", "sizes": "192x192", "type": "image/png", "purpose": "any maskable" },
        { "src": "/icons/icon-512.png", "sizes": "512x512", "type": "image/png", "purpose": "any maskable" },
        { "src": "/icons/icon.svg", "sizes": "any", "type": "image/svg+xml" }
    ],
    "screenshots": [],
    "shortcuts": [
        {
            "name": "بازی سریع",
            "short_name": "سریع",
            "url": "/home?quick=1",
            "icons": [{ "src": "/icons/quick-play.png", "sizes": "96x96" }]
        }
    ]
}
```

### ۹.۲ Service Worker

```javascript
// wwwroot/sw.js

const CACHE_NAME = 'chess-pwa-v1';
const PRECACHE_URLS = [
    '/',
    '/index.html',
    '/app.css',
    '/app.js',
    '/icons/icon-192.png',
    '/icons/icon-512.png',
    '/fonts/Vazirmatn-Regular.woff2',
    '/fonts/Vazirmatn-Bold.woff2',
    '/offline.html'
];

// Pre-cache در نصب
self.addEventListener('install', (event) => {
    event.waitUntil(
        caches.open(CACHE_NAME)
            .then(cache => cache.addAll(PRECACHE_URLS))
            .then(() => self.skipWaiting())
    );
});

// فعال‌سازی و پاک‌سازی کش قدیم
self.addEventListener('activate', (event) => {
    event.waitUntil(
        caches.keys().then(keys =>
            Promise.all(
                keys.filter(k => k !== CACHE_NAME).map(k => caches.delete(k))
            )
        ).then(() => self.clients.claim())
    );
});

// Fetch strategy
self.addEventListener('fetch', (event) => {
    const { request } = event;
    
    // API calls و SignalR: network-first
    if (request.url.includes('/api/') || request.url.includes('/hubs/')) {
        event.respondWith(
            fetch(request).catch(() => {
                return new Response(JSON.stringify({ error: 'آفلاین' }), {
                    status: 503,
                    headers: { 'Content-Type': 'application/json' }
                });
            })
        );
        return;
    }
    
    // WASM assemblies: cache-first
    if (request.url.endsWith('.wasm') || request.url.endsWith('.dll')) {
        event.respondWith(
            caches.match(request).then(cached => cached || fetch(request))
        );
        return;
    }
    
    // Static assets: cache-first with network fallback
    event.respondWith(
        caches.match(request).then(cached => cached || fetch(request)
            .catch(() => caches.match('/offline.html'))
        )
    );
});
```

### ۹.۳ ثبت Service Worker

```csharp
// wwwroot/index.html — در <head>
<link rel="manifest" href="/manifest.json" />
<meta name="theme-color" content="#2C5F8A" />
```

```javascript
// wwwroot/js/register-sw.js
if ('serviceWorker' in navigator) {
    window.addEventListener('load', () => {
        navigator.serviceWorker.register('/sw.js')
            .then(reg => console.log('SW registered'))
            .catch(err => console.error('SW registration failed', err));
    });
}
```

### ۹.۴ بهینه‌سازی WASM Loading (ARCH-05 + PRD F-PWA-07)

```csharp
// Program.cs — Lazy loading assemblies

// Blazor WASM assemblies را per-route lazy load کن
// فایل‌های حجیم (chess engine, fonts) فقط وقتی لازم هستند لود شوند

// در App.razor:
// <HeadContent>
//   <link rel="preload" href="/fonts/Vazirmatn-Regular.woff2" as="font" type="font/woff2" crossorigin />
// </HeadContent>
```

---

## ۱۰. تایپوگرافی و تم‌پذیری

### ۱۰.۱ بارگذاری فونت (TECH-14)

```css
/* wwwroot/app.css — Font-face declaration */

@font-face {
    font-family: 'Vazirmatn';
    src: url('/fonts/Vazirmatn-Thin.woff2') format('woff2');
    font-weight: 100;
    font-style: normal;
    font-display: swap;
    unicode-range: U+0000-00FF, U+0131, U+0152-0153, U+02C6, U+02DA, U+2000-206F;
}

@font-face {
    font-family: 'Vazirmatn';
    src: url('/fonts/Vazirmatn-Regular.woff2') format('woff2');
    font-weight: 400;
    font-style: normal;
    font-display: swap;
    unicode-range: U+0000-00FF, U+0131, U+0152-0153, U+02C6, U+02DA, U+2000-206F;
}

@font-face {
    font-family: 'Vazirmatn';
    src: url('/fonts/Vazirmatn-Bold.woff2') format('woff2');
    font-weight: 700;
    font-style: normal;
    font-display: swap;
    unicode-range: U+0000-00FF, U+0131, U+0152-0153, U+02C6, U+02DA, U+2000-206F;
}

@font-face {
    font-family: 'Vazirmatn';
    src: url('/fonts/Vazirmatn-SemiBold.woff2') format('woff2');
    font-weight: 600;
    font-style: normal;
    font-display: swap;
    unicode-range: U+0000-00FF, U+0131, U+0152-0153, U+02C6, U+02DA, U+2000-206F;
}
```

### ۱۰.۲ Design Tokens (CSS Custom Properties)

```css
/* wwwroot/app.css */

:root {
    /* ═══ رنگ‌ها — تم Peace ═══ */
    --color-primary: #2C5F8A;
    --color-primary-hover: #234C70;
    --color-primary-light: #E8F0F8;
    
    --color-surface: #FFFFFF;
    --color-surface-secondary: #F5F7FA;
    --color-surface-elevated: #FFFFFF;
    
    --color-background: #F8F9FA;
    --color-text: #1A1D21;
    --color-text-secondary: #6B7280;
    --color-text-muted: #9CA3AF;
    
    --color-success: #22C55E;
    --color-warning: #F59E0B;
    --color-danger: #EF4444;
    --color-info: #3B82F6;
    
    /* ═══ رنگ‌های صفحهٔ شطرنج ═══ */
    --color-board-light: #EBECD0;
    --color-board-dark: #779556;
    --color-board-highlight: rgba(255, 255, 0, 0.4);
    --color-board-selected: rgba(20, 85, 30, 0.5);
    --color-board-legal-move: rgba(0, 0, 0, 0.1);
    --color-board-check: rgba(255, 0, 0, 0.4);
    --color-board-last-move: rgba(155, 199, 0, 0.41);
    
    /* ═══ حاشیه و عمق (iOS-like) ═══ */
    --color-border: #E5E7EB;
    --color-border-light: #F3F4F6;
    
    /* ═══ سایه (عمق ملایم HIG) ═══ */
    --shadow-xs: 0 1px 2px rgba(0, 0, 0, 0.05);
    --shadow-sm: 0 1px 3px rgba(0, 0, 0, 0.1), 0 1px 2px rgba(0, 0, 0, 0.06);
    --shadow-md: 0 4px 6px rgba(0, 0, 0, 0.07), 0 2px 4px rgba(0, 0, 0, 0.06);
    --shadow-lg: 0 10px 15px rgba(0, 0, 0, 0.1), 0 4px 6px rgba(0, 0, 0, 0.05);
    --shadow-xl: 0 20px 25px rgba(0, 0, 0, 0.1), 0 10px 10px rgba(0, 0, 0, 0.04);
    
    /* ═══ شعاع گوشه ═══ */
    --radius-sm: 6px;
    --radius-md: 10px;
    --radius-lg: 16px;
    --radius-xl: 24px;
    --radius-full: 9999px;
    
    /* ═══ فاصله‌گذاری ═══ */
    --spacing-xs: 4px;
    --spacing-sm: 8px;
    --spacing-md: 12px;
    --spacing-lg: 16px;
    --spacing-xl: 24px;
    --spacing-2xl: 32px;
    --spacing-3xl: 48px;
    
    /* ═══ تایپوگرافی ═══ */
    --font-family: 'Vazirmatn', system-ui, -apple-system, sans-serif;
    --font-mono: 'Vazirmatn', monospace;
    
    --font-size-2xs: 0.6875rem;   /* 11px */
    --font-size-xs: 0.75rem;      /* 12px */
    --font-size-sm: 0.875rem;     /* 14px */
    --font-size-base: 1rem;       /* 16px */
    --font-size-lg: 1.125rem;     /* 18px */
    --font-size-xl: 1.25rem;      /* 20px */
    --font-size-2xl: 1.5rem;      /* 24px */
    --font-size-3xl: 1.875rem;    /* 30px */
    --font-size-4xl: 2.25rem;     /* 36px */
    
    --font-weight-normal: 400;
    --font-weight-medium: 500;
    --font-weight-semibold: 600;
    --font-weight-bold: 700;
    
    --line-height-tight: 1.25;
    --line-height-normal: 1.5;
    --line-height-relaxed: 1.75;
    
    /* ═══ انیمیشن ═══ */
    --transition-fast: 150ms ease;
    --transition-normal: 250ms ease;
    --transition-slow: 350ms ease;
    
    /* ═══ اندازه‌های کلیدی ═══ */
    --size-tap-target: 44px;   /* حداقل اندازهٔ لمسی HIG */
    --size-board: min(90vw, 560px); /* حداکثر اندازهٔ صفحه */
}
```

### ۱۰.۳ تم تاریک

```css
/* wwwroot/themes/dark.css */

[data-theme="dark"] {
    --color-primary: #5B9BD5;
    --color-primary-hover: #4A86BD;
    --color-primary-light: #1A2A3A;
    
    --color-surface: #1E2028;
    --color-surface-secondary: #252730;
    --color-surface-elevated: #2A2C35;
    
    --color-background: #12141A;
    --color-text: #E5E7EB;
    --color-text-secondary: #9CA3AF;
    --color-text-muted: #6B7280;
    
    --color-board-light: #769656;
    --color-board-dark: #EEEED2;
    
    --color-border: #374151;
    --color-border-light: #1F2937;
    
    --shadow-sm: 0 1px 3px rgba(0, 0, 0, 0.3);
    --shadow-md: 0 4px 6px rgba(0, 0, 0, 0.4);
}
```

### ۱۰.۴ Skin Interface (F-THM-02)

```csharp
// Chess.Domain/Themes/IPieceSkin.cs

/// <summary>
/// هر تم مهره یک پیاده‌سازی از این interface دارد.
/// در MVP فقط ClassicSkin وجود دارد.
/// افزودن تم آینده = افزودن کلاس جدید + ثبت در DI.
/// </summary>
public interface IPieceSkin
{
    string Name { get; }
    string GetSvgPath(Piece piece); // مسیر SVG مهره
    
    // مثال: "pieces/classic/white-queen.svg"
    // آینده: "pieces/neo/white-queen.svg"
}

// Chess.Domain/Themes/IBoardSkin.cs

public interface IBoardSkin
{
    string Name { get; }
    
    // رنگ خانه‌ها — می‌تواند از CSS variables هم بخواند
    string GetLightSquareCssVar();
    string GetDarkSquareCssVar();
    
    // حاشیه و مختصات
    bool ShowCoordinates { get; }
    string CoordinateStyle { get; }
}

// Chess.Domain/Themes/ClassicPieceSkin.cs

public sealed class ClassicPieceSkin : IPieceSkin
{
    public string Name => "کلاسیک";
    public string GetSvgPath(Piece piece) => $"/pieces/classic/{piece.Color.ToString().ToLower()}-{piece.Type.ToString().ToLower()}.svg";
}

// Chess.Domain/Themes/ClassicBoardSkin.cs

public sealed class ClassicBoardSkin : IBoardSkin
{
    public string Name => "کلاسیک";
    public string GetLightSquareCssVar() => "var(--color-board-light)";
    public string GetDarkSquareCssVar() => "var(--color-board-dark)";
    public bool ShowCoordinates => true;
    public string CoordinateStyle => "inline";
}
```

### ۱۰.۵ Theme Service (ترجیح کاربر)

```csharp
// Chess.Web/Services/ThemeService.cs

public sealed class ThemeService
{
    private readonly ILocalStorageService _storage;
    
    public string CurrentTheme { get; private set; } = "light";
    public IPieceSkin CurrentPieceSkin { get; private set; } = new ClassicPieceSkin();
    public IBoardSkin CurrentBoardSkin { get; private set; } = new ClassicBoardSkin();
    
    public event Action? ThemeChanged;
    
    public async Task LoadSavedThemeAsync()
    {
        CurrentTheme = await _storage.GetItemAsync<string>("theme") ?? "light";
        await ApplyThemeAsync();
    }
    
    public async Task SetThemeAsync(string theme)
    {
        CurrentTheme = theme;
        await _storage.SetItemAsync("theme", theme);
        await ApplyThemeAsync();
        ThemeChanged?.Invoke();
    }
    
    private async Task ApplyThemeAsync()
    {
        // اعمال data-theme attribute به <html>
        await JSRuntime.InvokeVoidAsync("document.documentElement.setAttribute", "data-theme", CurrentTheme);
    }
}
```

---

## ۱۱. احراز هویت و امنیت

### ۱۱.۱ جریان احراز هویت (ARCH-02)

```
┌──────────┐     POST /api/auth/login     ┌──────────┐
│  کلاینت   │ ─────────────────────────→   │  سرور    │
│  (Blazor) │                              │  (API)   │
│           │ ←─────────────────────────   │          │
│           │     Set-Cookie: auth_session  │          │
└──────────┘     HttpOnly; SameSite=Strict └──────────┘

1. کاربر لاگین می‌کند
2. سرور Credential را validate می‌کند
3. سرور Cookie با نام auth_session 设置 می‌کند (HttpOnly, SameSite=Strict)
4. کلاینت در درخواست‌های بعدی کوکی را خودکار ارسال می‌کند
5. SignalR اتصال هم کوکی را همراه دارد (same-origin)
6. Anti-forgery token برای mutation endpoints
```

### ۱۱.۲ هش رمز عبارت

```csharp
// Chess.Infrastructure/Services/PasswordHasher.cs

public sealed class PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 100_000;
    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;
    
    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Algorithm, HashSize);
        return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }
    
    public bool Verify(string password, string hashedPassword)
    {
        var parts = hashedPassword.Split('.');
        var salt = Convert.FromBase64String(parts[0]);
        var hash = Convert.FromBase64String(parts[1]);
        var testHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Algorithm, HashSize);
        return CryptographicOperations.FixedTimeEquals(hash, testHash);
    }
}
```

### ۱۱.۳ Rate Limiting

```csharp
// Program.cs

builder.Services.AddRateLimiter(options =>
{
    // لاگین: حداکثر ۵ تلاش در ۵ دقیقه
    options.AddFixedWindowLimiter("login", opt =>
    {
        opt.PermitLimit = 5;
        opt.Window = TimeSpan.FromMinutes(5);
    });
    
    // API عمومی: حداکثر ۱۰۰ درخواست در دقیقه
    options.AddSlidingWindowLimiter("api", opt =>
    {
        opt.PermitLimit = 100;
        opt.Window = TimeSpan.FromMinutes(1);
    });
    
    // پیام آماده: حداکثر ۵ در دقیقه (BR-17)
    options.AddSlidingWindowLimiter("preset-msg", opt =>
    {
        opt.PermitLimit = 5;
        opt.Window = TimeSpan.FromMinutes(1);
    });
    
    // Staff ops: حداکثر ۳۰ اقدام در دقیقه
    options.AddSlidingWindowLimiter("staff", opt =>
    {
        opt.PermitLimit = 30;
        opt.Window = TimeSpan.FromMinutes(1);
    });
});

// Application middleware
app.UseRateLimiter();
```

### ۱۱.۴ Anti-forgery

```csharp
// Blazor فرم‌ها
<EditForm Model="model" OnValidSubmit="HandleSubmit">
    <AntiforgeryToken />
    ...
</EditForm>

// API endpoints
app.MapPost("/api/auth/register", ...)
    .RequireAntiforgeryValidation(); // یا手动 validate
```

---

## ۱۲. Observability و Analytics

> پوشش NFR-11 (Observability) و PRD §۲۴ (شاخص‌های موفقیت و آنالیتیکس). تا نسخهٔ ۱.۰.۰ این لایه در معماری غایب بود.

### ۱۲.۱ متریک‌های عملیاتی (NFR-11)

```csharp
// Chess.Application/Services/IMetricsService.cs

public interface IMetricsService
{
    void IncrementCounter(string name, IDictionary<string, string>? tags = null);
    void RecordGauge(string name, double value, IDictionary<string, string>? tags = null);
    void RecordDuration(string name, TimeSpan elapsed, IDictionary<string, string>? tags = null);
}
```

- پیاده‌سازی زیرساختی: `System.Diagnostics.Metrics` (built-in .NET) + OpenTelemetry exporter → هر بک‌اند مانیتورینگ (Prometheus/Grafana یا مشابه) که تیم عملیات انتخاب کند؛ تصمیم انتخاب backend مانیتورینگ **خارج از این سند** و بازِ سبک عملیاتی است.
- متریک‌های حداقلی فاز ۱ (مطابق §۲۴.۲ PRD و NFR-11): `games.active.count`، `queue.length`، `users.online.count`، `moves.applied.count`، `errors.count{type}`، `signalr.reconnect.count`، `matchmaking.wait_time.ms`.
- این متریک‌ها از همان نقاطی جمع‌آوری می‌شوند که `IGameStateManager` و `MatchmakingService` وضعیت را تغییر می‌دهند — بدون نشت به Domain (اصل ۷ PRD؛ `IMetricsService` در Infrastructure پیاده می‌شود، نه Domain).

### ۱۲.۲ لاگ امنیتی پایه (NFR-03 / F-TRU-03)

```csharp
// Chess.Infrastructure/Services/SecurityAuditLogger.cs
// رویدادهای حداقلی: login موفق/ناموفق، rate-limit trigger، تلاش دسترسی غیرمجاز به /staff،
// تغییر رمز، اعمال Sanction — جدا از StaffAuditLog دامنه (که فقط اقدامات Staff را پوشش می‌دهد)
```

### ۱۲.۳ رویدادهای آنالیتیکس مفهومی (PRD §۲۴.۲ — بدون PII غیرضروری)

پیاده‌سازی از طریق همان `IMetricsService`/event pipeline؛ فقط شمارش/الگو، نه محتوای بازی یا پیام‌ها: `game.started`، `game.finished{reason}`، `signup.completed`، `pwa.installed`، `spectator.joined`. مطابق §۲۴.۳ PRD، از ابتدا **هیچ** رویداد حاوی محتوای پیام آماده، IP دقیق یا fingerprint دستگاه ثبت نمی‌شود.

---

## ۱۳. سیستم صدا (Client-side)

> پوشش F-FB-03 (صدای حرکت/گرفتن/کیش/پایان) و F-UI-05 (روشن/خاموش)؛ ARCH-16.

### ۱۳.۱ اصل طراحی

صدا کاملاً یک نگرانی UI است — **هیچ منطقی در Domain/Application نمی‌داند صدا پخش می‌شود یا نه** (اصل ۷ PRD، تفکیک UI/Domain). `GameStateChanged` که از GameHub می‌آید حاوی enough اطلاعات (آیا Capture بود؟ آیا Check شد؟ آیا Game تمام شد؟) هست تا کلاینت خودش تصمیم بگیرد چه صدایی پخش کند.

```csharp
// Chess.Web/Services/SoundService.cs — فقط در پروژهٔ Web (Presentation)، نه Application

public interface ISoundService
{
    Task PlayAsync(SoundEvent evt);
    bool IsEnabled { get; set; } // از ThemeService/UserPreferences خوانده می‌شود — F-UI-05
}

public enum SoundEvent { Move, Capture, Check, GameStart, GameEnd, DrawOffered, IllegalMove }
```

```
wwwroot/
└── sounds/
    ├── move.mp3
    ├── capture.mp3
    ├── check.mp3
    ├── game-start.mp3
    ├── game-end.mp3
    └── notify.mp3
```

- فراخوانی از `ChessBoard.razor` و `GameStateService` بعد از دریافت هر `GameStateChanged`/`GameFinished` — با مقایسهٔ state قبل/بعد تشخیص می‌دهد کدام صدا پخش شود.
- تنظیم `IsEnabled` در `SettingsPage` ذخیره و از طریق `localStorage`-معادل Blazor (یا سرویس تنظیمات کاربر سمت سرور در فاز بعد) نگه داشته می‌شود.
- **بدون وابستگی هاپتیک سخت‌افزاری** (مطابق PRD §۲۱.۴)؛ فقط پخش صوت + لرزش بصری UI (F-FB-04) که در CSS انیمیشن پیاده می‌شود، نه این سرویس.

---

## ۱۴. نقشهٔ ساخت (Build Sequence)

### فاز ۰ — پایه‌گذاری (Sprint 1-2)

| # | وظیفه | وابستگی | خروجی |
| :---: | :--- | :--- | :--- |
| 1 | Solution skeleton + ۴ پروژه + test projects | — | `dotnet new` + اسکلت |
| 2 | Domain entities + value objects | — | User, Game, MoveRecord, BoardState |
| 3 | موتور شطرنج: `BoardState`, `Piece`, `MoveGenerator`, `ClassicRuleSet` | #2 | تست‌های حرکت (unit tests) |
| 4 | EF Core DbContext + SQLite + مایگریشن اولیه | #2 | DB قابل اجرا |
| 5 | Auth پایه (register/login/logout) | #4 | لاگین با کوکی کار می‌کند |
| 6 | Blazor shell + CSS tokens + فونت | — | صفحهٔ خالی با تم Peace |
| 7 | PWA manifest + SW اسکلت | #6 | نصب‌پذیری روی Chrome |

### فاز ۱ — MVP بازی (Sprint 3-5)

| # | وظیفه | وابستگی | خروجی |
| :---: | :--- | :--- | :--- |
| 8 | Game aggregate + state machine | #3 | وضعیت‌های بازی |
| 9 | ساعت سرور (ServerClockService) | #8 | تایمر دقیق |
| 10 | LiveGameState Manager (in-memory) | #8 | state زنده |
| 11 | GameHub + Move flow از ابتدا تا انتها | #8, #9, #10 | بازی روی مرورگر |
| 12 | ChessBoard component (Blazor) | #6, #11 | صفحه قابل بازی |
| 13 | Clock component + MoveList | #11 | نمایش ساعت و تاریخچه |
| 14 | Matchmaking queue + Room/invite | #5 | پیدا کردن حریف |
| 15 | اتصال دعوت به بازی | #11, #14 | بازی دعوتی |
| 16 | نتیجه + ELO + Result page | #11 | پایان بازی |
| 17 | Reconnect + Disconnect handling + `DisconnectWatchdogService` (DEC-15/16، ARCH-15) | #11 | قطع و وصل + Abort دوطرفهٔ خودکار |
| 18 | Preset messages | #11 | پیام آماده در بازی |
| 19 | Theme system (classic skin) | #6 | تم کلاسیک |
| 19a | گزارش بازیکن — `SubmitReport` (REST + Hub، ARCH-13) | #11 | نقطهٔ ورودی گزارش از UI |
| 19b | `IdleAbandonTimer` برای حالت Untimed (ARCH-17) | #8 | ضدرهاسازی در بازی آزاد |
| 19c | دسترس‌پذیری کیبورد ChessBoard (NFR-09) | #12 | roving tabindex + aria-label |
| 19d | SoundService + فایل‌های صوتی (ARCH-16) | #12 | صدای حرکت/کیش/پایان قابل خاموش |

### فاز ۱x — Staff + Polish (Sprint 6-7)

| # | وظیفه | وابستگی | خروجی |
| :---: | :--- | :--- | :--- |
| 20 | Staff roles + auth policies | #5 | admin/moderator فعال |
| 21 | StaffHub + Dashboard | #20 | داشبورد لحظه‌ای |
| 22 | Report queue + Resolve flow | #20 | گزارش و رسیدگی |
| 23 | Sanctions (warn/ban/unban) | #20 | sanctions |
| 24 | Audit log | #20 | ممیزی |
| 25 | Profile + Settings + History | #19 | پروفایل کاربر |
| 26 | تکمیل تایپوگرافی + آیکون | #19 | آیکون‌های منسجم |
| 27 | PWA آفلاین‌پذیری + بهینه‌سازی | #7 | offline shell |
| 28 | صفحات حقوقی/محتوایی: Terms, Privacy, About, FAQ (F-CNT-03..06) | #6 | صفحات ثابت در سایت‌نقشه |
| 29 | حذف/غیرفعال‌سازی حساب (F-ACC-07) | #5 | `DeactivateAccount`/`DeleteAccount` |
| 30 | Observability پایه: `IMetricsService` + `SecurityAuditLogger` (§۱۲) | #4 | متریک‌های حداقلی + لاگ امنیتی |

### فاز ۲ — لایهٔ اجتماعی و تماشا (Sprint 8+، منطبق با PRD §۲۸ فاز ۲)

| # | وظیفه | وابستگی | خروجی |
| :---: | :--- | :--- | :--- |
| 31 | Friendship + UserBlock (Entity, Schema, UseCases — ARCH-11) | #5 | دوستان/بلاک عملیاتی |
| 32 | صفحهٔ `/friends` + وضعیت آنلاین (`IPresenceTracker`) | #31 | لیست دوستان + وضعیت |
| 33 | Spectator: گروه `spectators:{gameId}` + تأخیر ۳-۵ث (ARCH-12) | #11 | `/watch` قابل استفاده |

### معیار «آمادهٔ انتشار» هر فاز

#### فاز ۰:
- [ ] `dotnet build` بدون خطا
- [ ] `dotnet test` — تست‌های شطرنج Pass
- [ ] لاگین/لاگ‌اوت در مرورگر کار می‌کند
- [ ] صفحهٔ خالی با فونت فارسی و CSS tokens

#### فاز ۱:
- [ ] دو کاربر می‌توانند بازی کنند تا نتیجه
- [ ] قوانین ویژه درست‌اند (unit tests + integration)
- [ ] ساعت سرور و timeout درست‌اند
- [ ] Reconnect کار می‌کند
- [ ] قطع دوطرفهٔ >۶۰ث به Abort بدون ELO می‌انجامد (DEC-15)
- [ ] بازی Untimed در برابر رهاسازی محافظت می‌شود (DEC-02)
- [ ] ELO فقط برای Rated settle می‌شود
- [ ] PWA نصب‌پذیر است
- [ ] UI فارسی RTL روی موبایل و دسکتاپ
- [ ] صفحهٔ بازی با کیبورد قابل‌استفاده است (NFR-09)
- [ ] بازیکن می‌تواند از داخل بازی گزارش ثبت کند (F-SOC-06)

#### فاز ۱x:
- [ ] Admin می‌تواند نقش انتصاب کند
- [ ] Moderator صف گزارش را مدیریت می‌کند
- [ ] Warn/TempBan/Unban با دلیل اجباری کار می‌کند
- [ ] Audit log ثبت اقدامات Staff
- [ ] بازیکن می‌تواند گزارش ثبت کند
- [ ] صفحات شرایط استفاده و حریم خصوصی در دسترس‌اند (F-CNT-04,05)
- [ ] کاربر می‌تواند حساب را غیرفعال/حذف کند (F-ACC-07)
- [ ] متریک‌های حداقلی (online/games/errors) قابل مشاهده‌اند (NFR-11)

#### فاز ۲:
- [ ] درخواست/پذیرش/حذف دوستی کار می‌کند
- [ ] بلاک کاربر از matchmaking/دعوت/پیام جلوگیری می‌کند
- [ ] لیست بازی‌های زنده و تماشای با تأخیر کار می‌کند

---

## پیوست — فایل‌بندی Solution

```
src/
├── Chess.Domain/
│   ├── Common/
│   │   ├── Entity.cs
│   │   ├── AggregateRoot.cs
│   │   └── IDomainEvent.cs
│   ├── Entities/
│   │   ├── User.cs
│   │   ├── Game.cs
│   │   ├── Room.cs
│   │   ├── RatingChange.cs
│   │   ├── PlayerReport.cs
│   │   ├── UserSanction.cs
│   │   ├── StaffAuditLog.cs
│   │   ├── Friendship.cs        ← ARCH-11
│   │   ├── UserBlock.cs         ← ARCH-11
│   │   └── StaffNote.cs         ← F-STAFF-30
│   ├── ValueObjects/
│   │   ├── Piece.cs
│   │   ├── Square.cs
│   │   ├── BoardState.cs
│   │   ├── PieceColorExtensions.cs
│   │   ├── TimeControl.cs
│   │   ├── Rating.cs
│   │   └── MoveRecord.cs
│   ├── Chess/
│   │   ├── Move.cs
│   │   ├── MoveGenerator.cs
│   │   ├── DrawDetector.cs
│   │   └── Rules/
│   │       ├── IRuleSet.cs
│   │       └── ClassicRuleSet.cs
│   ├── Events/
│   │   ├── GameCreatedEvent.cs
│   │   ├── MoveAcceptedEvent.cs
│   │   └── ... (تمام رویدادها)
│   ├── Themes/
│   │   ├── IPieceSkin.cs
│   │   ├── IBoardSkin.cs
│   │   ├── ClassicPieceSkin.cs
│   │   └── ClassicBoardSkin.cs
│   └── Interfaces/
│       ├── IPasswordHasher.cs
│       └── ITimeProvider.cs
│
├── Chess.Application/
│   ├── Common/
│   │   ├── IUseCase.cs
│   │   └── UseCaseBase.cs
│   ├── Ports/
│   │   └── IRepositories.cs
│   ├── UseCases/
│   │   ├── Auth/
│   │   │   ├── RegisterUser.cs
│   │   │   ├── LoginUser.cs
│   │   │   ├── RecoverPassword.cs
│   │   │   ├── DeactivateAccount.cs   ← ARCH: F-ACC-07
│   │   │   └── DeleteAccount.cs       ← ARCH: F-ACC-07
│   │   ├── Game/
│   │   │   ├── CreateRoom.cs
│   │   │   ├── JoinRoom.cs
│   │   │   ├── MakeMove.cs
│   │   │   ├── OfferDraw.cs
│   │   │   ├── ResignGame.cs
│   │   │   ├── JoinAsSpectator.cs      ← ARCH-12
│   │   │   ├── GetLiveSpectatableGames.cs ← ARCH-12
│   │   │   └── ...
│   │   ├── Social/
│   │   │   ├── SendFriendRequest.cs    ← ARCH-11
│   │   │   ├── RespondFriendRequest.cs ← ARCH-11
│   │   │   ├── RemoveFriend.cs         ← ARCH-11
│   │   │   ├── BlockUser.cs            ← ARCH-11
│   │   │   ├── UnblockUser.cs          ← ARCH-11
│   │   │   └── SubmitReport.cs         ← ARCH-13
│   │   └── Staff/
│   │       ├── GetStaffDashboard.cs
│   │       ├── ResolveReport.cs
│   │       ├── ApplySanction.cs
│   │       ├── AssignRole.cs
│   │       └── ...
│   ├── DTOs/
│   │   ├── GameStateDto.cs
│   │   ├── GameResultDto.cs
│   │   └── DashboardDto.cs
│   ├── Services/
│   │   ├── IRatingService.cs
│   │   ├── IClockService.cs
│   │   ├── IIdleAbandonTimer.cs        ← ARCH-17
│   │   ├── IMatchmakingService.cs
│   │   ├── IGameStateManager.cs
│   │   └── IMetricsService.cs          ← Observability §۱۲
│   └── Validators/
│       ├── MoveValidator.cs
│       └── ... 
│
├── Chess.Infrastructure/
│   ├── Data/
│   │   ├── ChessDbContext.cs
│   │   ├── Configurations/   ← EF Fluent API
│   │   └── Repositories/
│   │       ├── UserRepository.cs
│   │       ├── GameRepository.cs
│   │       ├── MoveRepository.cs
│   │       ├── RoomRepository.cs
│   │       ├── RatingRepository.cs
│   │       ├── ReportRepository.cs
│   │       ├── SanctionRepository.cs
│   │       ├── AuditRepository.cs
│   │       ├── FriendshipRepository.cs
│   │       ├── UserBlockRepository.cs
│   │       ├── StaffNoteRepository.cs
│   │       └── UnitOfWork.cs
│   ├── Services/
│   │   ├── EloRatingService.cs
│   │   ├── ServerClockService.cs
│   │   ├── PasswordHasher.cs
│   │   ├── PermissionChecker.cs
│   │   ├── InMemoryGameStateManager.cs
│   │   ├── MatchmakingService.cs
│   │   ├── EmailService.cs
│   │   ├── DisconnectWatchdogService.cs ← ARCH-15 (BackgroundService)
│   │   ├── IdleAbandonTimer.cs          ← ARCH-17 (BackgroundService)
│   │   ├── SnapshotService.cs           ← BackgroundService
│   │   ├── RoomCleanupService.cs        ← BackgroundService
│   │   ├── SecurityAuditLogger.cs       ← Observability §۱۲.۲
│   │   └── OtelMetricsService.cs        ← Observability §۱۲.۱
│   └── Migrations/
│       └── ... (EF migrations)
│
├── Chess.Web/
│   ├── Program.cs
│   ├── Components/
│   │   ├── Layout/
│   │   │   ├── MainLayout.razor + .razor.css
│   │   │   ├── NavMenu.razor + .razor.css
│   │   │   └── StaffLayout.razor + .razor.css
│   │   ├── Game/
│   │   │   ├── ChessBoard.razor + .razor.css
│   │   │   ├── Square.razor + .razor.css
│   │   │   ├── Clock.razor + .razor.css
│   │   │   ├── MoveList.razor + .razor.css
│   │   │   ├── PlayerCard.razor + .razor.css
│   │   │   ├── DrawOfferBanner.razor + .razor.css
│   │   │   ├── PresetChat.razor + .razor.css
│   │   │   ├── MaterialDisplay.razor + .razor.css
│   │   │   └── PromotionDialog.razor + .razor.css
│   │   ├── Pages/
│   │   │   ├── Landing.razor
│   │   │   ├── Login.razor
│   │   │   ├── Register.razor
│   │   │   ├── Recover.razor
│   │   │   ├── Dashboard.razor
│   │   │   ├── Queue.razor
│   │   │   ├── Room.razor
│   │   │   ├── Game.razor
│   │   │   ├── GameResult.razor
│   │   │   ├── History.razor
│   │   │   ├── GameReview.razor
│   │   │   ├── Profile.razor
│   │   │   ├── Settings.razor
│   │   │   ├── Friends.razor          ← ARCH-11
│   │   │   ├── SpectateList.razor     ← ARCH-12
│   │   │   ├── Spectate.razor         ← ARCH-12
│   │   │   ├── Terms.razor            ← F-CNT-04
│   │   │   ├── Privacy.razor          ← F-CNT-05
│   │   │   ├── About.razor            ← F-CNT-03
│   │   │   └── Faq.razor              ← F-CNT-06
│   │   └── Staff/
│   │       ├── StaffDashboard.razor
│   │       ├── ReportsQueue.razor
│   │       ├── ReportDetail.razor
│   │       ├── UserSearch.razor
│   │       ├── UserDossier.razor
│   │       ├── ActiveGames.razor
│   │       ├── AuditLog.razor
│   │       └── RoleManagement.razor
│   ├── Services/
│   │   ├── GameStateService.cs
│   │   ├── ThemeService.cs
│   │   └── SoundService.cs      ← ARCH-16 §۱۳
│   ├── Hubs/
│   │   ├── IGameHub.cs
│   │   ├── GameHub.cs
│   │   ├── IStaffHub.cs
│   │   └── StaffHub.cs
│   └── wwwroot/
│       ├── app.css              ← Design Tokens
│       ├── manifest.json
│       ├── sw.js
│       ├── index.html
│       ├── icons/
│       ├── fonts/               ← Vazirmatn woff2
│       ├── pieces/classic/      ← SVG مهره‌ها
│       └── sounds/              ← ARCH-16 §۱۳ (move/capture/check/...)
│
tests/
├── Chess.Domain.Tests/
│   ├── Chess/
│   │   ├── MoveGeneratorTests.cs
│   │   ├── ClassicRuleSetTests.cs
│   │   ├── DrawDetectorTests.cs
│   │   └── BoardStateTests.cs
│   └── Rating/
│       └── EloRatingServiceTests.cs
│
├── Chess.Application.Tests/
│   ├── UseCases/
│   │   ├── MakeMoveTests.cs
│   │   ├── CreateRoomTests.cs
│   │   └── ...
│   └── Services/
│       └── MatchmakingTests.cs
│
└── Chess.IntegrationTests/ (اختیاری)
    ├── GameFlowTests.cs
    └── AuthTests.cs
```

---

## پیوست ج — Traceability به کاتالوگ قابلیت‌های PRD

> هر ID از بخش ۴ مستند نیازمندی (PRD v2.4.0) به بخش متناظر در این سند نگاشت شده است.

### ج.۱ هویت و حساب (F-ACC)

| ID | قابلیت | فاز PRD | بخش معماری | وضعیت |
| :--- | :--- | :--- | :--- | :--- |
| F-ACC-01 | ثبت‌نام | MVP | §۴.۲ `RegisterUser` + §۸.۲ `POST /api/auth/register` | ✅ |
| F-ACC-02 | ورود/خروج | MVP | §۴.۲ `LoginUser` + §۸.۲ `POST /api/auth/login` + `POST /api/auth/logout` | ✅ |
| F-ACC-03 | بازیابی رمز | MVP | §۴.۲ `RecoverPassword` + §۸.۲ `POST /api/auth/recover` | ✅ |
| F-ACC-04 | ویرایش پروفایل | v1.1 | §۸.۲ `PUT /api/users/me` | ✅ |
| F-ACC-05 | تغییر رمز | v1.1 | §۸.۲ `PUT /api/users/me/password` | ✅ |
| F-ACC-06 | تأیید ایمیل | v1.1 | — | ⚠️ |
| F-ACC-07 | حذف/غیرفعال‌سازی حساب | v1.2 | §۴.۲ `DeactivateAccount`/`DeleteAccount` + §۸.۲ endpoints | ✅ |
| F-ACC-08 | جلوگیری مهمان در بازی ریت‌شده | MVP | §۸.۱ Auth policy `UserOnly` | ✅ |

### ج.۲ یافتن حریف و اتاق (F-MTCH)

| ID | قابلیت | فاز PRD | بخش معماری | وضعیت |
| :--- | :--- | :--- | :--- | :--- |
| F-MTCH-01 | Matchmaking تصادفی | MVP | §۴.۲ `JoinQueue` + §۷.۵ `MatchmakingService` | ✅ |
| F-MTCH-02 | گسترش بازه ریتینگ | MVP | §۷.۵ `_ratingWindows` | ✅ |
| F-MTCH-03 | لغو صف | MVP | §۴.۲ `LeaveQueue` + §۸.۲ `DELETE /api/matchmaking/cancel` | ✅ |
| F-MTCH-04 | اتاق دعوت با لینک | MVP | §۴.۲ `CreateRoom` + §۸.۲ `POST /api/rooms` | ✅ |
| F-MTCH-05 | اتاق دعوت با کد | MVP | §۵.۲ Room entity (InviteCode) + §۸.۲ `POST /api/rooms/join` | ✅ |
| F-MTCH-06 | تنظیمات اتاق | MVP | §۵.۲ Room entity (IsRated, BaseTimeSeconds, IncrementSeconds) | ✅ |
| F-MTCH-07 | وضعیت Ready | MVP | §۸.۲ `PUT /api/rooms/{id}/ready` + §۷.۱ `RoomReady` | ✅ |
| F-MTCH-08 | انقضای اتاق | MVP | §۵.۲ Room.ExpiresAt + `IRoomRepository.CleanupExpiredAsync` | ✅ |
| F-MTCH-09 | جلوگیری match با خود | MVP | — (implicit in MatchmakingService) | ⚠️ |

### ج.۳ هسته بازی (F-GAME)

| ID | قابلیت | فاز PRD | بخش معماری | وضعیت |
| :--- | :--- | :--- | :--- | :--- |
| F-GAME-01 | صفحه ۸×۸ | MVP | §۲.۱ `BoardState` | ✅ |
| F-GAME-02 | اعتبارسنجی حرکات سرور | MVP | §۳.۱ `IRuleSet.ValidateMove` + §۳.۲ `ClassicRuleSet` | ✅ |
| F-GAME-03 | هایلایت خانه‌های مجاز | MVP | §۶.۴ `LegalTargets` + §۳.۵ `ClientMoveGenerator` | ✅ |
| F-GAME-04 | کلیک دو مرحله‌ای + Drag&Drop | MVP | §۶.۴ `HandleSquareClick` + `HandleDragStart` + `HandleDrop` | ✅ |
| F-GAME-05 | قلعه‌روی/آن‌پاسان/ترفیع | MVP | §۳.۱ `IsCastlingLegal`, `IsEnPassantLegal`, `IsPromotionRequired` | ✅ |
| F-GAME-06 | کیش/مات/پات | MVP | §۳.۱ `IsInCheck`, `IsCheckmate`, `IsStalemate` | ✅ |
| F-GAME-07 | تشخیص تساوی فنی | MVP | §۳.۴ `DrawDetector` | ✅ |
| F-GAME-08 | مهره‌های گرفته‌شده | MVP | §۶.۴ `MaterialDisplay` component | ✅ |
| F-GAME-09 | تاریخچه حرکات | MVP | §۶.۴ `MoveList` component | ✅ |
| F-GAME-10 | هایلایت حرکت آخر | MVP | §۶.۴ `IsLastMove` property | ✅ |
| F-GAME-11 | نشان مختصات | MVP | §۶.۴ `ShowCoordinates="true"` | ✅ |
| F-GAME-12 | جهت صفحه | MVP | §۶.۴ `BoardOrientation` property | ✅ |
| F-GAME-13 | تسلیم | MVP | §۴.۲ `ResignGame` + §۷.۱ Hub `Resign` | ✅ |
| F-GAME-14 | تساوی | MVP | §۴.۲ `OfferDraw`/`RespondDraw` + §۷.۱ Hub methods | ✅ |
| F-GAME-15 | ادعای تساوی قانون‌محور | v1.1 | §۳.۴ `DrawDetector` (server auto-detect) | ✅ |
| F-GAME-16 | پایان با اتمام زمان | MVP | §۵.۶ `ServerClockService.IsFlagged` | ✅ |
| F-GAME-17 | صفحه نتیجه | MVP | §۶.۲ `ResultPage` + `ResultOverlay`, `RatingDeltaCard` | ✅ |
| F-GAME-18 | Rematch | v1.1 | §۴.۲ `ProposeRematch`/`AcceptRematch` + §۷.۱ Hub | ✅ |
| F-GAME-19 | Abort | v1.1 | §۲.۲ `GameStatus.Aborted` + `ResultReason.Abort` | ✅ |
| F-GAME-20 | Premoves | v1.2/Could | — | ❌ |
| F-GAME-21 | بازبینی بعد از بازی | v1.1 | §۶.۲ `GameReviewPage` + `BoardReview` + `MoveNavigator` | ✅ |
| F-GAME-22 | کپی FEN/PGN | Could | — | ❌ |

### ج.۴ ساعت و کنترل زمان (F-CLK)

| ID | قابلیت | فاز PRD | بخش معماری | وضعیت |
| :--- | :--- | :--- | :--- | :--- |
| F-CLK-01 | ساعت Server-Side | MVP | §۵.۶ `ServerClockService` | ✅ |
| F-CLK-02 | حالت‌های زمانی | MVP | §۱.۱ ARCH decisions + §۶.۲ QueuePage | ✅ |
| F-CLK-03 | Increment فیشر | MVP | §۵.۶ `ServerClockService.Tick` | ✅ |
| F-CLK-04 | نمایش دهم‌ثانیه | v1.1 | — | ⚠️ |
| F-CLK-05 | هشدار کم‌بودن زمان | MVP | — (implicit در Clock component) | ⚠️ |
| F-CLK-06 | بازی بدون ساعت | MVP | §۵.۷ `IIdleAbandonTimer` (ARCH-17) | ✅ |
| F-CLK-07 | همگام‌سازی ساعت | MVP | §۵.۶ `IClockService.Tick` | ✅ |

### ج.۵ ریتینگ و آمار (F-RATE)

| ID | قابلیت | فاز PRD | بخش معماری | وضعیت |
| :--- | :--- | :--- | :--- | :--- |
| F-RATE-01 | ELO کلاسیک | MVP | §۵.۵ `EloRatingService` | ✅ |
| F-RATE-02 | یک ریتینگ کلی | MVP | §۵.۵ (single rating, K=20, start=1200) | ✅ |
| F-RATE-03 | نمایش تغییر ریتینگ | MVP | §۶.۲ `ResultPage` → `RatingDeltaCard` | ✅ |
| F-RATE-04 | تاریخچه تغییرات | v1.1 | §۵.۲ `RatingChange` entity + Schema | ✅ |
| F-RATE-05 | آمار W/L/D | v1.1 | — (فقط `GamesPlayed`) | ⚠️ |
| F-RATE-06 | لیدربورد | Could | — | ❌ |
| F-RATE-07 | Provisional rating | v1.1 | §۱.۱ ARCH-08 (not in MVP) | ✅ |

### ج.۶ اجتماعی و ارتباطات (F-SOC)

| ID | قابلیت | فاز PRD | بخش معماری | وضعیت |
| :--- | :--- | :--- | :--- | :--- |
| F-SOC-01 | پیام‌های آماده | MVP | §۴.۲ `SendPresetMessage` + §۷.۱ `PresetMessage` + §۱۱.۳ Rate limit | ✅ |
| F-SOC-02 | محدودیت نرخ پیام | MVP | §۱۱.۳ Rate limiter `preset-msg` (5/min) | ✅ |
| F-SOC-03 | لیست دوستان | Could | §۲.۲ `Friendship` + §۴.۲ UseCases + §۵.۲ Schema + §۶.۲ `FriendsPage` | ✅ |
| F-SOC-04 | دعوت مستقیم | Could | §۶.۲ `FriendsPage` → `AddFriendBox` | ✅ |
| F-SOC-05 | وضعیت آنلاین | Could | §۲.۲ note (IPresenceTracker) + §۷.۴ `user:{userId}` group | ✅ |
| F-SOC-06 | گزارش بازیکن | **MVP** | §۲.۲ `PlayerReport` + §۴.۲ `SubmitReport` + §۷.۱ Hub + §۸.۲ REST | ✅ |
| F-SOC-07 | بلاک کاربر | Could | §۲.۲ `UserBlock` + §۴.۲ `BlockUser`/`UnblockUser` + §۸.۲ API | ✅ |

### ج.۷ تماشا و بایگانی (F-HIST, F-SPEC)

| ID | قابلیت | فاز PRD | بخش معماری | وضعیت |
| :--- | :--- | :--- | :--- | :--- |
| F-HIST-01 | لیست بازی‌های من | v1.1 | §۸.۲ `GET /api/history` | ✅ |
| F-HIST-02 | جزئیات بازی | v1.1 | §۸.۲ `GET /api/history/{id}` + §۶.۲ `GameReviewPage` | ✅ |
| F-HIST-03 | دانلود PGN | Could | — | ❌ |
| F-SPEC-01 | لیست بازی‌های زنده | Could | §۴.۲ `GetLiveSpectatableGames` + §۸.۲ API + §۶.۲ `SpectateListPage` | ✅ |
| F-SPEC-02 | تماشا با تأخیر | Could | §۴.۲ `JoinAsSpectator` + §۷.۱ `SpectatorStateChanged` + §۷.۴ `spectators:{gameId}` | ✅ |
| F-SPEC-03 | عدم نمایش چت خصوصی | Could | — (implicit: spectators group ≠ game group) | ✅ |

### ج.۸ شخصی‌سازی و UI (F-UI)

| ID | قابلیت | فاز PRD | بخش معماری | وضعیت |
| :--- | :--- | :--- | :--- | :--- |
| F-UI-01 | تم کلاسیک مهره/صفحه | MVP | §۱۰.۴ `ClassicPieceSkin` + `ClassicBoardSkin` | ✅ |
| F-UI-02 | تم روشن پیش‌فرض | MVP | §۱۰.۲ Design Tokens (`:root`) | ✅ |
| F-UI-03 | تم تاریک | Could | §۱۰.۳ `dark.css` (`[data-theme="dark"]`) | ✅ |
| F-UI-04 | تم‌های اضافی | Could | §۱۰.۴ `IPieceSkin` + `IBoardSkin` interfaces | ✅ |
| F-UI-05 | تنظیم صدا | v1.1 | §۱۰.۵ `SoundToggle` + §۱۳ `ISoundService.IsEnabled` | ✅ |
| F-UI-06 | انیمیشن حرکت | MVP | — (صریح نیست) | ⚠️ |
| F-UI-07 | کاهش حرکت | v1.2 | — | ❌ |

### ج.۹ صدا و بازخورد (F-FB, F-NTF)

| ID | قابلیت | فاز PRD | بخش معماری | وضعیت |
| :--- | :--- | :--- | :--- | :--- |
| F-FB-01 | بازخورد حرکت غیرمجاز | MVP | §۶.۴ `MoveRejected` + rollback | ✅ |
| F-FB-02 | نشان کیش | MVP | §۶.۴ `IsCheckSquare` + §۷.۱ `CheckDetected` | ✅ |
| F-FB-03 | صدا | v1.1 | §۱۳ `ISoundService` + `SoundEvent` + `wwwroot/sounds/` | ✅ |
| F-FB-04 | لرزش UI | MVP | — (صریح نیست) | ⚠️ |
| F-NTF-01 | اعلان درون‌برنامه‌ای | MVP | — (partial: `GameFinished` event) | ⚠️ |
| F-NTF-02 | Browser Notification | Could | — | ❌ |

### ج.۱۰ پنل Staff (F-STAFF)

| ID | قابلیت | فاز PRD | بخش معماری | وضعیت |
| :--- | :--- | :--- | :--- | :--- |
| F-STAFF-01 | نقش‌ها | MVP | §۲.۲ `UserRole` enum + §۸.۱ Auth policies | ✅ |
| F-STAFF-02 | ماتریس مجوز | MVP | §۴.۳ `IPermissionChecker` + §۸.۱ Policies | ✅ |
| F-STAFF-03 | انتصاب مدراتور | MVP | §۴.۲ `AssignRole` + §۸.۲ `POST /api/staff/roles` | ✅ |
| F-STAFF-04 | ورود Staff | MVP | §۶.۲ `/staff/*` pages + §۸.۱ `StaffOnly` policy | ✅ |
| F-STAFF-05 | ممنوعیت escalation | MVP | §۴.۳ `IPermissionChecker` | ✅ |
| F-STAFF-10 | داشبورد | MVP | §۴.۲ `GetStaffDashboard` + §۷.۳ `StaffHub` | ✅ |
| F-STAFF-11 | لیست بازی‌های فعال | MVP | §۶.۲ `ActiveGames` + §۸.۲ API | ✅ |
| F-STAFF-12 | لیست آنلاین | v1.1 | — | ⚠️ |
| F-STAFF-13 | جست‌وجوی کاربر | MVP | §۸.۲ `GET /api/staff/users/search` | ✅ |
| F-STAFF-14 | پروفایل عملیاتی | MVP | §۴.۲ `GetUserDossier` + §۸.۲ API + §۶.۲ `UserDossier` | ✅ |
| F-STAFF-15 | مشاهده بازی | MVP | §۶.۲ `GameReviewStaff` | ✅ |
| F-STAFF-16 | اجبار پایان | v1.1 | §۴.۲ `ForceFinishGame` + §۶.۲ `ForceFinishButton` | ✅ |
| F-STAFF-20 | صف گزارش | MVP | §۴.۲ `ListReports` + §۶.۲ `ReportsQueue` | ✅ |
| F-STAFF-21 | جزئیات گزارش | MVP | §۶.۲ `ReportDetail` | ✅ |
| F-STAFF-22 | اقدام گزارش | MVP | §۴.۲ `ResolveReport` | ✅ |
| F-STAFF-23 | هشدار | MVP | §۴.۲ `ApplySanction(Warn)` | ✅ |
| F-STAFF-24 | بن موقت | MVP | §۴.۲ `ApplySanction(TempBan)` | ✅ |
| F-STAFF-25 | بن دائم | MVP | §۴.۲ `ApplySanction(PermBan)` | ✅ |
| F-STAFF-26 | آن‌بن | MVP | §۴.۲ `RemoveSanction` | ✅ |
| F-STAFF-27 | اجبار تغییر نام | v1.1 | §۴.۲ `ApplySanction(ForceRename)` | ✅ |
| F-STAFF-28 | Mute پیام | v1.1 | §۲.۲ `User.MutePresetsUntil` | ✅ |
| F-STAFF-29 | محدودیت Matchmaking | v1.2 | — | ❌ |
| F-STAFF-30 | یادداشت داخلی | MVP | §۵.۱.۱ `StaffNote` entity + §۵.۱ `OnModelCreating` + §۵.۳ `IStaffNoteRepository` | ✅ |
| F-STAFF-40 | مدیریت نقش | MVP | §۴.۲ `AssignRole` + §۸.۲ API | ✅ |
| F-STAFF-41 | audit log | MVP | §۴.۲ `GetAuditLog` + §۵.۲ `StaffAuditLog` + §۸.۲ API | ✅ |
| F-STAFF-42 | تنظیمات عملیاتی | v1.2 | — | ❌ |
| F-STAFF-43 | اعلامیه سراسری | v1.2 | — | ❌ |
| F-STAFF-44 | خروجی آمار | Could | — | ❌ |

### ج.۱۱ اعتماد فنی (F-TRU)

| ID | قابلیت | فاز PRD | بخش معماری | وضعیت |
| :--- | :--- | :--- | :--- | :--- |
| F-TRU-01 | Rate limiting | MVP | §۱۱.۳ — login, api, preset-msg, staff | ✅ |
| F-TRU-02 | اعتبارسنجی state | MVP | §۳.۱ `IRuleSet.ValidateMove` | ✅ |
| F-TRU-03 | لاگ امنیتی | MVP | §۱۲.۲ `SecurityAuditLogger` | ✅ |
| F-TRU-04 | تشخیص چندحسابی | Won't | — (سیاست ساده + رسیدگی دستی) | ✅ |
| F-TRU-05 | authorize سرور | MVP | §۴.۳ + §۸.۱ Policies + DEC-27 | ✅ |

### ج.۱۲ محتوا و حقوقی (F-CNT)

| ID | قابلیت | فاز PRD | بخش معماری | وضعیت |
| :--- | :--- | :--- | :--- | :--- |
| F-CNT-01 | لندینگ | MVP | §۶.۲ `LandingPage` + `LandingHero`, `LandingFeatures`, `LandingCTA` | ✅ |
| F-CNT-02 | راهنمای قوانین | v1.1 | — | ⚠️ |
| F-CNT-03 | درباره ما | v1.1 | §۶.۲ `AboutPage` | ✅ |
| F-CNT-04 | شرایط استفاده | MVP | §۶.۲ `TermsPage` + `/terms` | ✅ |
| F-CNT-05 | حریم خصوصی | MVP | §۶.۲ `PrivacyPage` + `/privacy` | ✅ |
| F-CNT-06 | FAQ | v1.1 | §۶.۲ `FaqPage` + `/faq` | ✅ |

### ج.۱۳ تنوع بازی (F-VAR)

| ID | قابلیت | فاز PRD | بخش معماری | وضعیت |
| :--- | :--- | :--- | :--- | :--- |
| F-VAR-01 | Classic | MVP | §۳.۲ `ClassicRuleSet` | ✅ |
| F-VAR-02 | Rapid | MVP | TimeControl config | ✅ |
| F-VAR-03 | Blitz | MVP | TimeControl config | ✅ |
| F-VAR-04 | Bullet | v1.1 | — (نیاز به clock با دهم‌ثانیه) | ⚠️ |
| F-VAR-05 | Untimed | MVP | §۵.۷ `IIdleAbandonTimer` (ARCH-17) | ✅ |
| F-VAR-06 | RuleSet abstraction | MVP | §۳.۱ `IRuleSet` interface | ✅ |
| F-VAR-07 | Chess960 | Could | — | ❌ |
| F-VAR-08 | واریانت سرگرمی | Could | — | ❌ |

### ج.۱۴ تم و شخصی‌سازی بصری (F-THM)

| ID | قابلیت | فاز PRD | بخش معماری | وضعیت |
| :--- | :--- | :--- | :--- | :--- |
| F-THM-01 | تم کلاسیک | MVP | §۱۰.۴ `ClassicPieceSkin` + `ClassicBoardSkin` | ✅ |
| F-THM-02 | معمارِ تم | MVP | §۱۰.۲ Design Tokens + §۱۰.۴ Skin Interfaces | ✅ |
| F-THM-03 | تم روشن | MVP | §۱۰.۲ `:root` variables | ✅ |
| F-THM-04 | تم تاریک | Could | §۱۰.۳ `[data-theme="dark"]` | ✅ |
| F-THM-05 | تم‌های اضافی مهره | Could | §۱۰.۴ `IPieceSkin` interface | ✅ |
| F-THM-06 | تم‌های اضافی صفحه | Could | §۱۰.۴ `IBoardSkin` interface | ✅ |
| F-THM-07 | ذخیره انتخاب تم | Could | §۱۰.۵ `ThemeService` + localStorage | ✅ |
| F-THM-08 | تم برند Peace | MVP | §۱۰.۲ Design Tokens (رنگ‌های آرام) | ✅ |

### ج.۱۵ تایپوگرافی (F-TYP)

| ID | قابلیت | فاز PRD | بخش معماری | وضعیت |
| :--- | :--- | :--- | :--- | :--- |
| F-TYP-01 | فونت فارسی | MVP | §۱۰.۱ Vazirmatn `@font-face` | ✅ |
| F-TYP-02 | مقیاس تایپوگرافی | MVP | §۱۰.۲ Design Tokens (font-size 2xs–4xl) | ✅ |
| F-TYP-03 | ارقام خوانا | MVP | §۱۰.۲ (implicit) | ✅ |
| F-TYP-04 | سیستم آیکون | MVP | — (صریح نیست) | ⚠️ |
| F-TYP-05 | ارقام فارسی | Could | — | ❌ |
| F-TYP-06 | فونت تک‌عرض | v1.1 | — | ⚠️ |

### ج.۱۶ PWA (F-PWA)

| ID | قابلیت | فاز PRD | بخش معماری | وضعیت |
| :--- | :--- | :--- | :--- | :--- |
| F-PWA-01 | Manifest | MVP | §۹.۱ `manifest.json` | ✅ |
| F-PWA-02 | Service Worker | MVP | §۹.۲ `sw.js` | ✅ |
| F-PWA-03 | نصب‌پذیری | MVP | §۹.۱ + §۹.۳ registration | ✅ |
| F-PWA-04 | آفلاین‌پذیری محدود | v1.1 | §۹.۲ (`offline.html` fallback) | ✅ |
| F-PWA-05 | Splash + theme color | MVP | §۹.۱ `theme_color` + §۹.۳ meta tag | ✅ |
| F-PWA-06 | Push notifications | Could | — | ❌ |
| F-PWA-07 | بهینه WASM | v1.1 | §۹.۴ + §۶.۱ (lazy load) | ✅ |

### ج.۱۷ خلاصهٔ نهایی Traceability

| وضعیت | تعداد | درصد |
| :--- | :---: | :---: |
| ✅ کامل / تصمیم‌گیری شده | ~۱۰۵ | ~۸۹٪ |
| ⚠️ صریح نیست / جزئی | ~۸ | ~۷٪ |
| ❌ خارج (Could/Won't/فاز دور) | ~۵ | ~۴٪ |

### ج.۱۸ موارد نیازمند اصلاح

| # | مشکل | وضعیت |
| :--- | :--- | :--- |
| ۱ | `StaffNote` entity در معماری نیست (F-STAFF-30) | ✅ رفع شد — §۵.۱.۱ + §۵.۱ OnModelCreating |
| ۲ | تأیید ایمیل (F-ACC-06) پوشش ندارد | ⚠️ باقی — hook معماری لازم |
| ۳ | سیستم آیکون (F-TYP-04) صریح نیست | ⚠️ باقی — interface اضافه شود |
| ۴ | انیمیشن حرکت (F-UI-06) صریح نیست | ⚠️ باقی — توضیح اضافه شود |
| ۵ | لرزش UI (F-FB-04) صریح نیست | ⚠️ باقی — توضیح اضافه شود |
| ۶ | یادداشت نسخه ۱.۱.۰ (خط ۱۳) ارجاعات § نادرست دارد | ✅ رفع شد |
| ۷ | TOC بخش ۱۵ (Traceability) ارجاع به پیوستی داشت | ✅ رفع شد — پیوست ج اضافه شد |
| ۸ | Footer نسخه ۱.۰.۰ بود | ✅ رفع شد — ۱.۱.۰ |
| ۹ | DbContext.OnModelCreating پیاده‌سازی نداشت | ✅ رفع شد — کامل نوشته شد |
| ۱۰ | ClockState record و PieceColor.Opposite تعریف نشده بودند | ✅ رفع شد |
| ۱۱ | Repository interfaces برای Friendship/UserBlock/StaffNote نبود | ✅ رفع شد |
| ۱۲ | راهنمای تحویل توسعه‌دهنده (handoff-guide.md) نبود | ✅ رفع شد — ایجاد شد |

---

**پایان مستند معماری — نسخه ۱.۱.۰**

*این سند همراه با مستند نیازمندی (PRD v2.4.0) به‌عنوان مبنای کامل پیاده‌سازی استفاده می‌شود.*
