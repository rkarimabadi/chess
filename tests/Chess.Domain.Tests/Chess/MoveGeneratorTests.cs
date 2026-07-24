using Chess.Domain.Chess;
using Chess.Domain.ValueObjects;

namespace Chess.Domain.Tests.Chess;

public class MoveGeneratorTests
{
    [Fact]
    public void InitialPosition_WhitePawns_ShouldHaveCorrectMoves()
    {
        var board = BoardState.Initial();
        var moves = MoveGenerator.GetLegalMoves(board, PieceColor.White);

        // Each pawn should have 1 or 2 moves (forward 1 + optionally forward 2)
        var pawnMoves = moves.Where(m => m.Piece.Type == PieceType.Pawn).ToList();
        Assert.Equal(16, pawnMoves.Count); // 8 pawns × 2 moves each

        // e2 pawn should have 2 moves (e3, e4)
        var e2Moves = pawnMoves.Where(m => m.From == Square.Parse("e2")).ToList();
        Assert.Equal(2, e2Moves.Count);
    }

    [Fact]
    public void InitialPosition_Knight_ShouldHave2Moves()
    {
        var board = BoardState.Initial();
        var moves = MoveGenerator.GetLegalMoves(board, PieceColor.White);

        var knightMoves = moves.Where(m => m.Piece.Type == PieceType.Knight).ToList();
        // b1 knight: a3, c3; g1 knight: f3, h3
        Assert.Equal(4, knightMoves.Count);
    }

    [Fact]
    public void InitialPosition_Bishop_ShouldHave0Moves()
    {
        var board = BoardState.Initial();
        var moves = MoveGenerator.GetLegalMoves(board, PieceColor.White);

        var bishopMoves = moves.Where(m => m.Piece.Type == PieceType.Bishop).ToList();
        Assert.Empty(bishopMoves);
    }

    [Fact]
    public void InitialPosition_Queen_ShouldHave0Moves()
    {
        var board = BoardState.Initial();
        var moves = MoveGenerator.GetLegalMoves(board, PieceColor.White);
        var queenMoves = moves.Where(m => m.Piece.Type == PieceType.Queen).ToList();
        Assert.Empty(queenMoves);
    }

    [Fact]
    public void InitialPosition_King_ShouldHave0Moves()
    {
        var board = BoardState.Initial();
        var moves = MoveGenerator.GetLegalMoves(board, PieceColor.White);
        var kingMoves = moves.Where(m => m.Piece.Type == PieceType.King).ToList();
        Assert.Empty(kingMoves);
    }

    [Fact]
    public void Pawn_CaptureMove_ShouldBeLegal()
    {
        var board = BoardState.Initial();
        // Set up a capture scenario: white pawn on e4, black pawn on d5
        board.SetPiece(Square.Parse("e4"), new Piece(PieceColor.White, PieceType.Pawn));
        board.SetPiece(Square.Parse("d5"), new Piece(PieceColor.Black, PieceType.Pawn));
        board.SetPiece(Square.Parse("e2"), null); // Remove original pawn

        var moves = MoveGenerator.GetLegalMoves(board, PieceColor.White);
        var e4Moves = moves.Where(m => m.From == Square.Parse("e4")).ToList();

        // Should be able to capture d5 or advance to e5
        Assert.Contains(e4Moves, m => m.To == Square.Parse("d5"));
        Assert.Contains(e4Moves, m => m.To == Square.Parse("e5"));
    }

    [Fact]
    public void IsKingInCheck_WhenInCheck_ShouldReturnTrue()
    {
        // Set up a position where white king is in check
        // Simple scenario: white king on e1, black queen on d1 (adjacent, attacking)
        var board = BoardState.Initial();
        // Clear everything except kings
        for (int r = 0; r < 8; r++)
            for (int f = 0; f < 8; f++)
                board.SetPiece(new Square(f, r), null);

        board.SetPiece(Square.Parse("e1"), new Piece(PieceColor.White, PieceType.King));
        board.SetPiece(Square.Parse("e8"), new Piece(PieceColor.Black, PieceType.King));
        board.SetPiece(Square.Parse("d1"), new Piece(PieceColor.Black, PieceType.Queen));

        var inCheck = MoveGenerator.IsKingInCheck(board, PieceColor.White);
        Assert.True(inCheck);
    }

    [Fact]
    public void IsKingInCheck_WhenNotInCheck_ShouldReturnFalse()
    {
        var board = BoardState.Initial();
        var inCheck = MoveGenerator.IsKingInCheck(board, PieceColor.White);
        Assert.False(inCheck);
    }
}
