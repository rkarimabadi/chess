using Chess.Domain.ValueObjects;

namespace Chess.Domain.Chess.Rules;

public enum MoveResultStatus { Legal, Illegal, Check, Checkmate, Stalemate, Draw }

public sealed class MoveResult
{
    public MoveResultStatus Status { get; init; }
    public string? SanNotation { get; init; }
    public string? Reason { get; init; }
}

public interface IRuleSet
{
    string VariantId { get; }
    IReadOnlyList<Move> GetLegalMoves(BoardState board, PieceColor side);
    bool IsInCheck(BoardState board, PieceColor side);
    bool IsCheckmate(BoardState board);
    bool IsStalemate(BoardState board);
    bool IsDrawByRules(BoardState board, IReadOnlyList<string> positionHistory);
    bool IsCastlingLegal(BoardState board, Square from, Square to);
    bool IsEnPassantLegal(BoardState board, Square from, Square to);
    bool IsPromotionRequired(BoardState board, Square from, Square to);
    IReadOnlyList<PieceType> GetPromotionChoices();
    MoveResult ValidateMove(BoardState board, Square from, Square to, PieceType? promotion = null);
}
