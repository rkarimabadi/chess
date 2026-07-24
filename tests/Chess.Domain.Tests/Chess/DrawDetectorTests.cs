using Chess.Domain.Chess;
using Chess.Domain.ValueObjects;

namespace Chess.Domain.Tests.Chess;

public class DrawDetectorTests
{
    [Fact]
    public void IsThreefoldRepetition_WhenSamePosition3Times_ShouldReturnTrue()
    {
        var fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
        var history = new List<string> { fen, fen, fen };

        Assert.True(DrawDetector.IsThreefoldRepetition(history));
    }

    [Fact]
    public void IsThreefoldRepetition_WhenDifferentPositions_ShouldReturnFalse()
    {
        var fen1 = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
        var fen2 = "rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b KQkq - 0 1";
        var history = new List<string> { fen1, fen2, fen1 };

        Assert.False(DrawDetector.IsThreefoldRepetition(history));
    }

    [Fact]
    public void IsFiftyMoveRule_When100HalfMoves_ShouldReturnTrue()
    {
        Assert.True(DrawDetector.IsFiftyMoveRule(100));
    }

    [Fact]
    public void IsFiftyMoveRule_WhenLessThan100_ShouldReturnFalse()
    {
        Assert.False(DrawDetector.IsFiftyMoveRule(99));
    }

    [Fact]
    public void IsInsufficientMaterial_KingVsKing_ShouldReturnTrue()
    {
        var board = BoardState.Initial();
        // Clear everything except kings
        for (int r = 0; r < 8; r++)
            for (int f = 0; f < 8; f++)
                board.SetPiece(new Square(f, r), null);

        board.SetPiece(Square.Parse("e1"), new Piece(PieceColor.White, PieceType.King));
        board.SetPiece(Square.Parse("e8"), new Piece(PieceColor.Black, PieceType.King));

        Assert.True(DrawDetector.IsInsufficientMaterial(board));
    }

    [Fact]
    public void IsInsufficientMaterial_KingBishopVsKing_ShouldReturnTrue()
    {
        var board = BoardState.Initial();
        for (int r = 0; r < 8; r++)
            for (int f = 0; f < 8; f++)
                board.SetPiece(new Square(f, r), null);

        board.SetPiece(Square.Parse("e1"), new Piece(PieceColor.White, PieceType.King));
        board.SetPiece(Square.Parse("e8"), new Piece(PieceColor.Black, PieceType.King));
        board.SetPiece(Square.Parse("f1"), new Piece(PieceColor.White, PieceType.Bishop));

        Assert.True(DrawDetector.IsInsufficientMaterial(board));
    }

    [Fact]
    public void IsInsufficientMaterial_KingKnightVsKing_ShouldReturnTrue()
    {
        var board = BoardState.Initial();
        for (int r = 0; r < 8; r++)
            for (int f = 0; f < 8; f++)
                board.SetPiece(new Square(f, r), null);

        board.SetPiece(Square.Parse("e1"), new Piece(PieceColor.White, PieceType.King));
        board.SetPiece(Square.Parse("e8"), new Piece(PieceColor.Black, PieceType.King));
        board.SetPiece(Square.Parse("f1"), new Piece(PieceColor.White, PieceType.Knight));

        Assert.True(DrawDetector.IsInsufficientMaterial(board));
    }

    [Fact]
    public void IsInsufficientMaterial_KingPawnVsKing_ShouldReturnFalse()
    {
        var board = BoardState.Initial();
        for (int r = 0; r < 8; r++)
            for (int f = 0; f < 8; f++)
                board.SetPiece(new Square(f, r), null);

        board.SetPiece(Square.Parse("e1"), new Piece(PieceColor.White, PieceType.King));
        board.SetPiece(Square.Parse("e8"), new Piece(PieceColor.Black, PieceType.King));
        board.SetPiece(Square.Parse("e2"), new Piece(PieceColor.White, PieceType.Pawn));

        Assert.False(DrawDetector.IsInsufficientMaterial(board));
    }
}
