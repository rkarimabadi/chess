using Chess.Domain.ValueObjects;

namespace Chess.Domain.Chess;

public static class MoveGenerator
{
    private static readonly (int df, int dr)[] KnightOffsets = { (1,2),(2,1),(2,-1),(1,-2),(-1,-2),(-2,-1),(-2,1),(-1,2) };
    private static readonly (int df, int dr)[] KingOffsets = { (0,1),(1,1),(1,0),(1,-1),(0,-1),(-1,-1),(-1,0),(-1,1) };
    private static readonly (int df, int dr)[] BishopDirs = { (1,1),(1,-1),(-1,1),(-1,-1) };
    private static readonly (int df, int dr)[] RookDirs = { (0,1),(0,-1),(1,0),(-1,0) };

    public static IReadOnlyList<Move> GetLegalMoves(BoardState board, PieceColor side)
    {
        return GetPseudoLegalMoves(board, side)
            .Where(m =>
            {
                var sim = SimulateMove(board, m);
                return !IsKingInCheck(sim, side);
            })
            .ToList();
    }

    public static IEnumerable<Move> GetPseudoLegalMoves(BoardState board, PieceColor side)
    {
        foreach (var (sq, piece) in board.GetAllPieces())
        {
            if (piece.Color != side) continue;
            foreach (var move in GetPieceMoves(board, sq, piece))
                yield return move;
        }
    }

    public static bool IsKingInCheck(BoardState board, PieceColor side)
    {
        var kingSq = FindKing(board, side);
        if (kingSq is null) return false;
        return IsSquareAttacked(board, kingSq, side.Opposite());
    }

    public static bool IsSquareAttacked(BoardState board, Square square, PieceColor bySide)
    {
        // Knight attacks
        foreach (var (df, dr) in KnightOffsets)
        {
            var sq = new Square(square.File + df, square.Rank + dr);
            if (!sq.IsValid) continue;
            var p = board.GetPiece(sq);
            if (p?.Color == bySide && p.Type == PieceType.Knight) return true;
        }

        // King attacks
        foreach (var (df, dr) in KingOffsets)
        {
            var sq = new Square(square.File + df, square.Rank + dr);
            if (!sq.IsValid) continue;
            var p = board.GetPiece(sq);
            if (p?.Color == bySide && p.Type == PieceType.King) return true;
        }

        // Sliding attacks (Bishop/Queen diagonals, Rook/Queen straights)
        foreach (var (df, dr) in BishopDirs)
        {
            if (SlidingAttack(board, square, df, dr, bySide, PieceType.Bishop, PieceType.Queen))
                return true;
        }
        foreach (var (df, dr) in RookDirs)
        {
            if (SlidingAttack(board, square, df, dr, bySide, PieceType.Rook, PieceType.Queen))
                return true;
        }

        // Pawn attacks
        int pawnDir = bySide == PieceColor.White ? -1 : 1;
        for (int df = -1; df <= 1; df += 2)
        {
            var sq = new Square(square.File + df, square.Rank + pawnDir);
            if (!sq.IsValid) continue;
            var p = board.GetPiece(sq);
            if (p?.Color == bySide && p.Type == PieceType.Pawn) return true;
        }

        return false;
    }

    private static bool SlidingAttack(BoardState board, Square origin, int df, int dr, PieceColor bySide, params PieceType[] types)
    {
        for (int i = 1; i < 8; i++)
        {
            var sq = new Square(origin.File + df * i, origin.Rank + dr * i);
            if (!sq.IsValid) break;
            var p = board.GetPiece(sq);
            if (p is null) continue;
            if (p.Color == bySide && types.Contains(p.Type)) return true;
            break;
        }
        return false;
    }

    public static Square? FindKing(BoardState board, PieceColor side)
    {
        foreach (var (sq, piece) in board.GetAllPieces())
        {
            if (piece.Color == side && piece.Type == PieceType.King) return sq;
        }
        return null;
    }

    public static bool IsPromotionRequired(BoardState board, Square from, Square to)
    {
        var piece = board.GetPiece(from);
        if (piece is null || piece.Type != PieceType.Pawn) return false;
        return (piece.Color == PieceColor.White && to.Rank == 7) ||
               (piece.Color == PieceColor.Black && to.Rank == 0);
    }

    private static IEnumerable<Move> GetPieceMoves(BoardState board, Square sq, Piece piece)
    {
        return piece.Type switch
        {
            PieceType.Pawn => GetPawnMoves(board, sq, piece),
            PieceType.Knight => GetKnightMoves(board, sq, piece),
            PieceType.Bishop => GetSlidingMoves(board, sq, piece, BishopDirs),
            PieceType.Rook => GetSlidingMoves(board, sq, piece, RookDirs),
            PieceType.Queen => GetSlidingMoves(board, sq, piece, BishopDirs.Concat(RookDirs).ToArray()),
            PieceType.King => GetKingMoves(board, sq, piece),
            _ => Enumerable.Empty<Move>()
        };
    }

    private static IEnumerable<Move> GetPawnMoves(BoardState board, Square sq, Piece piece)
    {
        int dir = piece.Color == PieceColor.White ? 1 : -1;
        int startRank = piece.Color == PieceColor.White ? 1 : 6;
        int promoRank = piece.Color == PieceColor.White ? 7 : 0;

        // Forward one
        var fwd = new Square(sq.File, sq.Rank + dir);
        if (fwd.IsValid && board.GetPiece(fwd) is null)
        {
            if (fwd.Rank == promoRank)
            {
                foreach (var promo in GetPromotions(sq, fwd, piece, null))
                    yield return promo;
            }
            else
            {
                yield return new Move(sq, fwd, piece, null, false, false, false, null);
            }

            // Forward two from start
            if (sq.Rank == startRank)
            {
                var fwd2 = new Square(sq.File, sq.Rank + dir * 2);
                if (board.GetPiece(fwd2) is null)
                    yield return new Move(sq, fwd2, piece, null, false, false, false, null);
            }
        }

        // Captures
        for (int df = -1; df <= 1; df += 2)
        {
            var cap = new Square(sq.File + df, sq.Rank + dir);
            if (!cap.IsValid) continue;
            var target = board.GetPiece(cap);
            if (target?.Color == piece.Color.Opposite())
            {
                if (cap.Rank == promoRank)
                {
                    foreach (var promo in GetPromotions(sq, cap, piece, target))
                        yield return promo;
                }
                else
                {
                    yield return new Move(sq, cap, piece, target, false, false, false, null);
                }
            }

            // En passant
            if (board.EnPassantTarget is not null && cap == board.EnPassantTarget)
            {
                var capturedPawn = board.GetPiece(new Square(cap.File, sq.Rank));
                yield return new Move(sq, cap, piece, capturedPawn, true, false, false, null);
            }
        }
    }

    private static IEnumerable<Move> GetPromotions(Square from, Square to, Piece piece, Piece? captured)
    {
        PieceType[] types = { PieceType.Queen, PieceType.Rook, PieceType.Bishop, PieceType.Knight };
        foreach (var t in types)
            yield return new Move(from, to, piece, captured, false, false, false, t);
    }

    private static IEnumerable<Move> GetKnightMoves(BoardState board, Square sq, Piece piece)
    {
        foreach (var (df, dr) in KnightOffsets)
        {
            var to = new Square(sq.File + df, sq.Rank + dr);
            if (!to.IsValid) continue;
            var target = board.GetPiece(to);
            if (target?.Color == piece.Color) continue;
            yield return new Move(sq, to, piece, target, false, false, false, null);
        }
    }

    private static IEnumerable<Move> GetSlidingMoves(BoardState board, Square sq, Piece piece, (int df, int dr)[] dirs)
    {
        foreach (var (df, dr) in dirs)
        {
            for (int i = 1; i < 8; i++)
            {
                var to = new Square(sq.File + df * i, sq.Rank + dr * i);
                if (!to.IsValid) break;
                var target = board.GetPiece(to);
                if (target is null)
                {
                    yield return new Move(sq, to, piece, null, false, false, false, null);
                    continue;
                }
                if (target.Color == piece.Color.Opposite())
                    yield return new Move(sq, to, piece, target, false, false, false, null);
                break;
            }
        }
    }

    private static IEnumerable<Move> GetKingMoves(BoardState board, Square sq, Piece piece)
    {
        foreach (var (df, dr) in KingOffsets)
        {
            var to = new Square(sq.File + df, sq.Rank + dr);
            if (!to.IsValid) continue;
            var target = board.GetPiece(to);
            if (target?.Color == piece.Color) continue;
            yield return new Move(sq, to, piece, target, false, false, false, null);
        }

        // Castling
        if (piece.Color == PieceColor.White)
        {
            if (board.WhiteKingSide && board.GetPiece(new Square(5, 0)) is null &&
                board.GetPiece(new Square(6, 0)) is null &&
                !IsSquareAttacked(board, sq, PieceColor.Black) &&
                !IsSquareAttacked(board, new Square(5, 0), PieceColor.Black) &&
                !IsSquareAttacked(board, new Square(6, 0), PieceColor.Black))
            {
                yield return new Move(sq, new Square(6, 0), piece, null, false, true, false, null);
            }
            if (board.WhiteQueenSide && board.GetPiece(new Square(3, 0)) is null &&
                board.GetPiece(new Square(2, 0)) is null &&
                board.GetPiece(new Square(1, 0)) is null &&
                !IsSquareAttacked(board, sq, PieceColor.Black) &&
                !IsSquareAttacked(board, new Square(3, 0), PieceColor.Black) &&
                !IsSquareAttacked(board, new Square(2, 0), PieceColor.Black))
            {
                yield return new Move(sq, new Square(2, 0), piece, null, false, false, true, null);
            }
        }
        else
        {
            if (board.BlackKingSide && board.GetPiece(new Square(5, 7)) is null &&
                board.GetPiece(new Square(6, 7)) is null &&
                !IsSquareAttacked(board, sq, PieceColor.White) &&
                !IsSquareAttacked(board, new Square(5, 7), PieceColor.White) &&
                !IsSquareAttacked(board, new Square(6, 7), PieceColor.White))
            {
                yield return new Move(sq, new Square(6, 7), piece, null, false, true, false, null);
            }
            if (board.BlackQueenSide && board.GetPiece(new Square(3, 7)) is null &&
                board.GetPiece(new Square(2, 7)) is null &&
                board.GetPiece(new Square(1, 7)) is null &&
                !IsSquareAttacked(board, sq, PieceColor.White) &&
                !IsSquareAttacked(board, new Square(3, 7), PieceColor.White) &&
                !IsSquareAttacked(board, new Square(2, 7), PieceColor.White))
            {
                yield return new Move(sq, new Square(2, 7), piece, null, false, false, true, null);
            }
        }
    }

    public static BoardState SimulateMove(BoardState board, Move move)
    {
        var sim = board.Clone();
        sim.EnPassantTarget = null;

        if (move.IsCastleKingSide)
        {
            int rank = move.From.Rank;
            sim.MovePiece(new Square(7, rank), new Square(5, rank));
        }
        else if (move.IsCastleQueenSide)
        {
            int rank = move.From.Rank;
            sim.MovePiece(new Square(0, rank), new Square(3, rank));
        }

        if (move.IsEnPassant)
        {
            sim.SetPiece(new Square(move.To.File, move.From.Rank), null);
        }

        sim.MovePiece(move.From, move.To);

        if (move.PromotionPiece.HasValue)
        {
            sim.SetPiece(move.To, new Piece(move.Piece.Color, move.PromotionPiece.Value));
        }

        // Set en passant target
        if (move.Piece.Type == PieceType.Pawn && Math.Abs(move.To.Rank - move.From.Rank) == 2)
        {
            int epRank = (move.From.Rank + move.To.Rank) / 2;
            sim.EnPassantTarget = new Square(move.From.File, epRank);
        }

        // Update castling rights
        if (move.Piece.Type == PieceType.King)
        {
            if (move.Piece.Color == PieceColor.White) { sim.WhiteKingSide = false; sim.WhiteQueenSide = false; }
            else { sim.BlackKingSide = false; sim.BlackQueenSide = false; }
        }
        if (move.Piece.Type == PieceType.Rook)
        {
            if (move.From == new Square(7, 0)) sim.WhiteKingSide = false;
            if (move.From == new Square(0, 0)) sim.WhiteQueenSide = false;
            if (move.From == new Square(7, 7)) sim.BlackKingSide = false;
            if (move.From == new Square(0, 7)) sim.BlackQueenSide = false;
        }
        if (move.To == new Square(7, 0)) sim.WhiteKingSide = false;
        if (move.To == new Square(0, 0)) sim.WhiteQueenSide = false;
        if (move.To == new Square(7, 7)) sim.BlackKingSide = false;
        if (move.To == new Square(0, 7)) sim.BlackQueenSide = false;

        // Halfmove clock
        if (move.Piece.Type == PieceType.Pawn || move.CapturedPiece is not null)
            sim.HalfmoveClock = 0;
        else
            sim.HalfmoveClock = board.HalfmoveClock + 1;

        if (move.Piece.Color == PieceColor.Black)
            sim.FullmoveNumber = board.FullmoveNumber + 1;
        else
            sim.FullmoveNumber = board.FullmoveNumber;

        sim.SwitchTurn();
        return sim;
    }

    public static string GenerateSan(BoardState board, Move move, bool isCheck, bool isMate)
    {
        if (move.IsCastleKingSide) return "O-O";
        if (move.IsCastleQueenSide) return "O-O-O";

        string san = "";
        if (move.Piece.Type == PieceType.Pawn)
        {
            if (move.CapturedPiece is not null || move.IsEnPassant)
                san = $"{(char)('a' + move.From.File)}x";
            san += move.To.ToAlgebraic();
            if (move.PromotionPiece.HasValue)
                san += $"={move.PromotionPiece.Value switch { PieceType.Queen => "Q", PieceType.Rook => "R", PieceType.Bishop => "B", PieceType.Knight => "N", _ => "?" }}";
        }
        else
        {
            string pieceChar = move.Piece.Type switch
            {
                PieceType.Knight => "N", PieceType.Bishop => "B",
                PieceType.Rook => "R", PieceType.Queen => "Q",
                PieceType.King => "K", _ => ""
            };
            san = pieceChar;

            // Disambiguation for pieces
            bool needFile = false, needRank = false;
            foreach (var (sq, p) in board.GetAllPieces())
            {
                if (p == move.Piece && sq != move.From && move.Piece.Type != PieceType.King)
                {
                    var legal = GetLegalMoves(board, move.Piece.Color)
                        .Where(m => m.To == move.To && m.From != move.From);
                    if (legal.Any())
                    {
                        if (sq.File != move.From.File) needFile = true;
                        else if (sq.Rank != move.From.Rank) needRank = true;
                        else { needFile = true; needRank = true; }
                    }
                }
            }
            if (needFile) san += (char)('a' + move.From.File);
            if (needRank) san += (move.From.Rank + 1).ToString();

            if (move.CapturedPiece is not null) san += "x";
            san += move.To.ToAlgebraic();
        }

        if (isMate) san += "#";
        else if (isCheck) san += "+";

        return san;
    }
}
