using Chess.Domain.Common;
using Chess.Domain.ValueObjects;

namespace Chess.Domain.Entities;

public sealed class Game : AggregateRoot
{
    public Guid WhitePlayerId { get; private set; }
    public Guid BlackPlayerId { get; private set; }
    public GameStatus Status { get; private set; } = GameStatus.Created;
    public GameResult Result { get; private set; } = GameResult.Ongoing;
    public ResultReason Reason { get; private set; } = ResultReason.None;
    public bool IsRated { get; private set; }
    public string Variant { get; private set; } = "Classic";
    public int BaseTimeSeconds { get; private set; }
    public int IncrementSeconds { get; private set; }
    public string CurrentFen { get; private set; } = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
    public int HalfmoveClock { get; private set; }
    public int FullmoveNumber { get; private set; } = 1;
    public long WhiteTimeRemainingMs { get; private set; }
    public long BlackTimeRemainingMs { get; private set; }
    public string PositionHistoryJson { get; private set; } = "[]";
    public bool DrawOfferPending { get; private set; }
    public Guid? DrawOfferedById { get; private set; }
    public List<MoveRecord> MoveHistory { get; private set; } = new();
    public bool WhiteConnected { get; set; } = true;
    public bool BlackConnected { get; set; } = true;
    public DateTime? WhiteDisconnectedAt { get; set; }
    public DateTime? BlackDisconnectedAt { get; set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; private set; }
    public DateTime? FinishedAt { get; private set; }

    private Game() { }

    public static Game Create(Guid whiteId, Guid blackId, int baseTimeSeconds, int incrementSeconds, bool isRated)
    {
        long baseMs = baseTimeSeconds * 1000L;
        return new Game
        {
            Id = Guid.NewGuid(),
            WhitePlayerId = whiteId,
            BlackPlayerId = blackId,
            BaseTimeSeconds = baseTimeSeconds,
            IncrementSeconds = incrementSeconds,
            IsRated = isRated,
            WhiteTimeRemainingMs = baseMs,
            BlackTimeRemainingMs = baseMs,
            Status = GameStatus.Active,
            StartedAt = DateTime.UtcNow
        };
    }

    public void Finish(GameResult result, ResultReason reason) { Result = result; Reason = reason; Status = GameStatus.Finished; FinishedAt = DateTime.UtcNow; }
    public void Abort() { Result = GameResult.Aborted; Reason = ResultReason.Abort; Status = GameStatus.Aborted; FinishedAt = DateTime.UtcNow; }
    public void SetFen(string fen) => CurrentFen = fen;
    public void SetTime(long whiteMs, long blackMs) { WhiteTimeRemainingMs = whiteMs; BlackTimeRemainingMs = blackMs; }
    public void SetHalfmove(int half) => HalfmoveClock = half;
    public void SetFullmove(int full) => FullmoveNumber = full;
    public void OfferDraw(Guid byId) { DrawOfferPending = true; DrawOfferedById = byId; }
    public void ClearDrawOffer() { DrawOfferPending = false; DrawOfferedById = null; }
    public void SetPositionHistory(string json) => PositionHistoryJson = json;
}
