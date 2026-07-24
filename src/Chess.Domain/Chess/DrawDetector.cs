using Chess.Domain.ValueObjects;

namespace Chess.Domain.Chess;

public static class DrawDetector
{
    public static bool IsThreefoldRepetition(IReadOnlyList<string> positionHistory)
    {
        var counts = positionHistory.GroupBy(f => f).Any(g => g.Count() >= 3);
        return counts;
    }

    public static bool IsFiftyMoveRule(int halfmoveClock) => halfmoveClock >= 100;

    public static bool IsInsufficientMaterial(BoardState board)
    {
        var pieces = board.GetAllPieces().ToList();
        if (pieces.Count == 2) return true; // K vs K
        if (pieces.Count == 3)
        {
            var nonKing = pieces.FirstOrDefault(p => p.Piece.Type != PieceType.King);
            if (nonKing.Piece.Type is PieceType.Bishop or PieceType.Knight) return true;
        }
        if (pieces.Count == 4)
        {
            var bishops = pieces.Where(p => p.Piece.Type == PieceType.Bishop).ToList();
            if (bishops.Count == 2 && bishops[0].Piece.Color != bishops[1].Piece.Color)
            {
                bool sameColor = (bishops[0].Square.File + bishops[0].Square.Rank) % 2 ==
                                 (bishops[1].Square.File + bishops[1].Square.Rank) % 2;
                if (sameColor) return true;
            }
        }
        return false;
    }
}
