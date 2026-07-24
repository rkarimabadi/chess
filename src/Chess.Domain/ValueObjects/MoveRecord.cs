using Chess.Domain.Common;
using Chess.Domain.Chess;

namespace Chess.Domain.ValueObjects;

public sealed class MoveRecord : Entity
{
    public Guid GameId { get; private set; }
    public int MoveNumber { get; private set; }
    public Square From { get; private set; }
    public Square To { get; private set; }
    public Piece Piece { get; private set; } = null!;
    public Piece? CapturedPiece { get; private set; }
    public string SanNotation { get; private set; } = string.Empty;
    public string FenBefore { get; private set; } = string.Empty;
    public string FenAfter { get; private set; } = string.Empty;
    public bool IsCheck { get; private set; }
    public bool IsCheckmate { get; private set; }
    public bool IsCapture { get; private set; }
    public bool IsCastleKingSide { get; private set; }
    public bool IsCastleQueenSide { get; private set; }
    public bool IsEnPassant { get; private set; }
    public PieceType? PromotionPiece { get; private set; }
    public DateTime Timestamp { get; private set; }

    public string FromSquare => From.ToAlgebraic();
    public string ToSquare => To.ToAlgebraic();
    public char PieceChar => Piece.ToChar();
    public char? CapturedPieceChar => CapturedPiece?.ToChar();
    public char? PromotionPieceChar => PromotionPiece.HasValue ? new Piece(PieceColor.White, PromotionPiece.Value).ToChar() : null;

    private MoveRecord()
    {
        From = new Square(0, 0);
        To = new Square(0, 0);
    }

    public static MoveRecord Create(Guid gameId, int moveNumber, Move move, string san, string fenBefore, string fenAfter, bool isCheck, bool isCheckmate)
    {
        return new MoveRecord
        {
            Id = Guid.NewGuid(), GameId = gameId, MoveNumber = moveNumber,
            From = move.From, To = move.To, Piece = move.Piece, CapturedPiece = move.CapturedPiece,
            SanNotation = san, FenBefore = fenBefore, FenAfter = fenAfter,
            IsCheck = isCheck, IsCheckmate = isCheckmate,
            IsCapture = move.CapturedPiece is not null,
            IsCastleKingSide = move.IsCastleKingSide, IsCastleQueenSide = move.IsCastleQueenSide,
            IsEnPassant = move.IsEnPassant, PromotionPiece = move.PromotionPiece,
            Timestamp = DateTime.UtcNow
        };
    }
}
