namespace Chess.Domain.ValueObjects;

public enum PieceColor { White, Black }

public enum PieceType { King, Queen, Rook, Bishop, Knight, Pawn }

public enum UserRole { User, Moderator, Admin }

public enum UserStatus { Active, Banned, Deleted }

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

public enum RoomStatus { Waiting, Ready, Expired, Closed }

public enum ReportStatus { Open, InReview, Resolved, Rejected, EscalatedToAdmin }

public enum ReportReason { IntentionalAbandon, SpamPresetMessage, InappropriateUsername, SuspicionOfCheating, Other }

public enum SanctionType { Warn, MutePresets, TempBan, PermBan, ForceRename }

public enum FriendshipStatus { Pending, Accepted, Declined }
