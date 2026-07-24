using System.Text;

namespace Chess.Domain.ValueObjects;

public sealed class BoardState
{
    private readonly Piece?[,] _squares = new Piece?[8, 8];

    public PieceColor CurrentTurn { get; internal set; }
    public bool WhiteKingSide { get; internal set; } = true;
    public bool WhiteQueenSide { get; internal set; } = true;
    public bool BlackKingSide { get; internal set; } = true;
    public bool BlackQueenSide { get; internal set; } = true;
    public Square? EnPassantTarget { get; internal set; }
    public int HalfmoveClock { get; internal set; }
    public int FullmoveNumber { get; internal set; } = 1;
    public List<string> PositionHistory { get; } = new();

    public Piece? GetPiece(Square sq) => _squares[sq.Rank, sq.File];
    public void SetPiece(Square sq, Piece? piece) => _squares[sq.Rank, sq.File] = piece;

    public void MovePiece(Square from, Square to)
    {
        _squares[to.Rank, to.File] = _squares[from.Rank, from.File];
        _squares[from.Rank, from.File] = null;
    }

    public void SwitchTurn() => CurrentTurn = CurrentTurn == PieceColor.White ? PieceColor.Black : PieceColor.White;

    public IEnumerable<(Square Square, Piece Piece)> GetAllPieces()
    {
        for (int rank = 0; rank < 8; rank++)
            for (int file = 0; file < 8; file++)
            {
                var sq = new Square(file, rank);
                var piece = _squares[rank, file];
                if (piece is not null) yield return (sq, piece);
            }
    }

    public string ToFen()
    {
        var sb = new StringBuilder();
        for (int rank = 7; rank >= 0; rank--)
        {
            int empty = 0;
            for (int file = 0; file < 8; file++)
            {
                var piece = _squares[rank, file];
                if (piece is null) { empty++; continue; }
                if (empty > 0) { sb.Append(empty); empty = 0; }
                sb.Append(piece.ToChar());
            }
            if (empty > 0) sb.Append(empty);
            if (rank > 0) sb.Append('/');
        }
        sb.Append(' '); sb.Append(CurrentTurn.ToFenChar()); sb.Append(' ');
        var castling = "";
        if (WhiteKingSide) castling += "K";
        if (WhiteQueenSide) castling += "Q";
        if (BlackKingSide) castling += "k";
        if (BlackQueenSide) castling += "q";
        sb.Append(string.IsNullOrEmpty(castling) ? "-" : castling);
        sb.Append(' '); sb.Append(EnPassantTarget?.ToAlgebraic() ?? "-");
        sb.Append(' '); sb.Append(HalfmoveClock);
        sb.Append(' '); sb.Append(FullmoveNumber);
        return sb.ToString();
    }

    public static BoardState FromFen(string fen)
    {
        var parts = fen.Split(' ');
        var board = new BoardState();
        var ranks = parts[0].Split('/');
        for (int rank = 7; rank >= 0; rank--)
        {
            int file = 0;
            foreach (char c in ranks[7 - rank])
            {
                if (char.IsDigit(c)) { file += c - '0'; continue; }
                var piece = Piece.FromChar(c);
                if (piece is not null) board._squares[rank, file] = piece;
                file++;
            }
        }
        board.CurrentTurn = parts[1] == "w" ? PieceColor.White : PieceColor.Black;
        var castling = parts[2];
        board.WhiteKingSide = castling.Contains('K');
        board.WhiteQueenSide = castling.Contains('Q');
        board.BlackKingSide = castling.Contains('k');
        board.BlackQueenSide = castling.Contains('q');
        board.EnPassantTarget = parts[3] == "-" ? null : Square.Parse(parts[3]);
        board.HalfmoveClock = int.Parse(parts[4]);
        board.FullmoveNumber = int.Parse(parts[5]);
        return board;
    }

    public static BoardState Initial() => FromFen("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");

    public BoardState Clone()
    {
        var clone = new BoardState();
        for (int r = 0; r < 8; r++)
            for (int f = 0; f < 8; f++)
                clone._squares[r, f] = _squares[r, f];
        clone.CurrentTurn = CurrentTurn;
        clone.WhiteKingSide = WhiteKingSide; clone.WhiteQueenSide = WhiteQueenSide;
        clone.BlackKingSide = BlackKingSide; clone.BlackQueenSide = BlackQueenSide;
        clone.EnPassantTarget = EnPassantTarget;
        clone.HalfmoveClock = HalfmoveClock; clone.FullmoveNumber = FullmoveNumber;
        return clone;
    }
}
