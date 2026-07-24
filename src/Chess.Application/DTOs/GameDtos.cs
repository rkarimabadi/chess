namespace Chess.Application.DTOs;

public sealed record CreateRoomRequest(Guid UserId, string TimeControl, bool IsRated, string? ColorPreference);
public sealed record CreateRoomResponse(Guid RoomId);
public sealed record JoinRoomRequest(Guid UserId, Guid RoomId);
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
    public string CallerColor { get; init; } = "White";
    public string BoardFen { get; init; } = string.Empty;
    public long WhiteTimeMs { get; init; }
    public long BlackTimeMs { get; init; }
    public string? LastMoveFrom { get; init; }
    public string? LastMoveTo { get; init; }
    public LastMoveDto? LastMove { get; init; }
    public int MoveCount { get; init; }
    public List<string> Moves { get; init; } = new();
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

public sealed record PlayerDto(Guid Id = default, string Username = "", int Rating = 0);
public sealed record TimeControlDto(int Base = 0, int Increment = 0);
public sealed record MaterialDto(List<string> CapturedByWhite = null!, List<string> CapturedByBlack = null!);
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
    public List<string> FenHistory { get; init; } = new();
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

public sealed record GetGameHistoryRequest(Guid UserId, int Page);
public sealed record GetGameDetailsRequest(Guid UserId, Guid GameId);
public sealed record GetLiveSpectatableGamesRequest(int Page);
public sealed record ProposeRematchResponse(string RematchToken);
