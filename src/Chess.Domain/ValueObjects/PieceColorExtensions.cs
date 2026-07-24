namespace Chess.Domain.ValueObjects;

public static class PieceColorExtensions
{
    public static PieceColor Opposite(this PieceColor color) => color == PieceColor.White ? PieceColor.Black : PieceColor.White;
    public static string ToFenChar(this PieceColor color) => color == PieceColor.White ? "w" : "b";
    public static string DisplayName(this PieceColor color) => color == PieceColor.White ? "سفید" : "سیاه";
}
