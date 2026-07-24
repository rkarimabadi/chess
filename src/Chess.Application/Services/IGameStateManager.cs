using Chess.Domain.ValueObjects;
using Chess.Domain.Entities;

namespace Chess.Application.Services;

public interface IGameStateManager
{
    Task<LiveGameState?> GetAsync(Guid gameId);
    Task UpsertAsync(Guid gameId, LiveGameState state);
    Task RemoveAsync(Guid gameId);
    Task<int> SnapshotAllActiveAsync();
    int GetActiveCount();
}

public sealed class LiveGameState
{
    public Guid GameId { get; set; }
    public BoardState Board { get; set; } = BoardState.Initial();
    public PieceColor CurrentTurn { get; set; } = PieceColor.White;
    public long WhiteTimeMs { get; set; }
    public long BlackTimeMs { get; set; }
    public DateTime LastMoveAt { get; set; }
    public bool DrawOfferPending { get; set; }
    public List<string> PositionHistory { get; set; } = [];
    public List<MoveRecord> MoveHistory { get; set; } = [];
    public bool WhiteConnected { get; set; } = true;
    public bool BlackConnected { get; set; } = true;
    public DateTime? WhiteDisconnectedAt { get; set; }
    public DateTime? BlackDisconnectedAt { get; set; }
    public DateTime? BothDisconnectedSince { get; set; }
    public int HalfmoveClock { get; set; }
    public int FullmoveNumber { get; set; } = 1;
    public bool IsRated { get; set; }
    public string Variant { get; set; } = "Classic";
    public int IncrementMs { get; set; }

    public const int ReconnectTimeoutSeconds = 60;
    public bool BothDisconnected => !WhiteConnected && !BlackConnected;
}
