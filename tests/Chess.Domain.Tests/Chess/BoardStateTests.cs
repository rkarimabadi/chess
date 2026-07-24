using Chess.Domain.ValueObjects;

namespace Chess.Domain.Tests.Chess;

public class BoardStateTests
{
    [Fact]
    public void Initial_ShouldHaveCorrectStartingPosition()
    {
        var board = BoardState.Initial();

        // White pieces - back rank
        Assert.Equal(PieceType.Rook, board.GetPiece(Square.Parse("a1"))!.Type);
        Assert.Equal(PieceType.Knight, board.GetPiece(Square.Parse("b1"))!.Type);
        Assert.Equal(PieceType.Bishop, board.GetPiece(Square.Parse("c1"))!.Type);
        Assert.Equal(PieceType.Queen, board.GetPiece(Square.Parse("d1"))!.Type);
        Assert.Equal(PieceType.King, board.GetPiece(Square.Parse("e1"))!.Type);
        Assert.Equal(PieceColor.White, board.GetPiece(Square.Parse("e1"))!.Color);

        // Black pieces
        Assert.Equal(PieceColor.Black, board.GetPiece(Square.Parse("e8"))!.Color);
        Assert.Equal(PieceType.King, board.GetPiece(Square.Parse("e8"))!.Type);

        // Pawns
        for (int file = 0; file < 8; file++)
        {
            var whitePawn = Square.Parse($"{(char)('a' + file)}2");
            var blackPawn = Square.Parse($"{(char)('a' + file)}7");
            Assert.Equal(PieceType.Pawn, board.GetPiece(whitePawn)!.Type);
            Assert.Equal(PieceColor.White, board.GetPiece(whitePawn)!.Color);
            Assert.Equal(PieceType.Pawn, board.GetPiece(blackPawn)!.Type);
            Assert.Equal(PieceColor.Black, board.GetPiece(blackPawn)!.Color);
        }

        // Empty squares
        Assert.Null(board.GetPiece(Square.Parse("e4")));
        Assert.Null(board.GetPiece(Square.Parse("d5")));
    }

    [Fact]
    public void Initial_ShouldHaveWhiteTurn()
    {
        var board = BoardState.Initial();
        Assert.Equal(PieceColor.White, board.CurrentTurn);
    }

    [Fact]
    public void MovePiece_ShouldMoveCorrectly()
    {
        var board = BoardState.Initial();
        var from = Square.Parse("e2");
        var to = Square.Parse("e4");

        var piece = board.GetPiece(from);
        Assert.NotNull(piece);
        Assert.Null(board.GetPiece(to));

        board.MovePiece(from, to);

        Assert.Null(board.GetPiece(from));
        Assert.NotNull(board.GetPiece(to));
        Assert.Equal(PieceType.Pawn, board.GetPiece(to)!.Type);
    }

    [Fact]
    public void SwitchTurn_ShouldAlternateTurns()
    {
        var board = BoardState.Initial();
        Assert.Equal(PieceColor.White, board.CurrentTurn);

        board.SwitchTurn();
        Assert.Equal(PieceColor.Black, board.CurrentTurn);

        board.SwitchTurn();
        Assert.Equal(PieceColor.White, board.CurrentTurn);
    }

    [Fact]
    public void SetPiece_ShouldPlacePieceCorrectly()
    {
        var board = BoardState.Initial();
        var sq = Square.Parse("e4");
        var piece = new Piece(PieceColor.White, PieceType.Queen);

        board.SetPiece(sq, piece);

        var placed = board.GetPiece(sq);
        Assert.NotNull(placed);
        Assert.Equal(PieceType.Queen, placed.Type);
        Assert.Equal(PieceColor.White, placed.Color);
    }

    [Fact]
    public void ToFen_And_FromFen_ShouldBeRoundTrip()
    {
        var board = BoardState.Initial();
        var fen = board.ToFen();

        var restored = BoardState.FromFen(fen);

        Assert.Equal(board.CurrentTurn, restored.CurrentTurn);
        for (int rank = 0; rank < 8; rank++)
        {
            for (int file = 0; file < 8; file++)
            {
                var sq = new Square(file, rank);
                var original = board.GetPiece(sq);
                var restoredPiece = restored.GetPiece(sq);
                if (original is null)
                    Assert.Null(restoredPiece);
                else
                {
                    Assert.NotNull(restoredPiece);
                    Assert.Equal(original.Color, restoredPiece.Color);
                    Assert.Equal(original.Type, restoredPiece.Type);
                }
            }
        }
    }

    [Fact]
    public void FromFen_StandardPosition_ShouldParseCorrectly()
    {
        var fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
        var board = BoardState.FromFen(fen);

        Assert.Equal(PieceColor.White, board.CurrentTurn);
        Assert.Equal(PieceType.Rook, board.GetPiece(Square.Parse("a8"))!.Type);
        Assert.Equal(PieceColor.Black, board.GetPiece(Square.Parse("a8"))!.Color);
        Assert.Equal(PieceType.King, board.GetPiece(Square.Parse("e1"))!.Type);
        Assert.Equal(PieceColor.White, board.GetPiece(Square.Parse("e1"))!.Color);
    }
}
