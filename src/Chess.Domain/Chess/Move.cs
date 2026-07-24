using Chess.Domain.ValueObjects;

namespace Chess.Domain.Chess;

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
