namespace Chess.Domain.ValueObjects;

public sealed record Piece(PieceColor Color, PieceType Type)
{
    public char ToChar() => Color switch
    {
        PieceColor.White => Type switch
        {
            PieceType.King => 'K', PieceType.Queen => 'Q',
            PieceType.Rook => 'R', PieceType.Bishop => 'B',
            PieceType.Knight => 'N', PieceType.Pawn => 'P', _ => '?'
        },
        _ => Type switch
        {
            PieceType.King => 'k', PieceType.Queen => 'q',
            PieceType.Rook => 'r', PieceType.Bishop => 'b',
            PieceType.Knight => 'n', PieceType.Pawn => 'p', _ => '?'
        }
    };

    public static Piece? FromChar(char c) => c switch
    {
        'K' => new(PieceColor.White, PieceType.King), 'Q' => new(PieceColor.White, PieceType.Queen),
        'R' => new(PieceColor.White, PieceType.Rook), 'B' => new(PieceColor.White, PieceType.Bishop),
        'N' => new(PieceColor.White, PieceType.Knight), 'P' => new(PieceColor.White, PieceType.Pawn),
        'k' => new(PieceColor.Black, PieceType.King), 'q' => new(PieceColor.Black, PieceType.Queen),
        'r' => new(PieceColor.Black, PieceType.Rook), 'b' => new(PieceColor.Black, PieceType.Bishop),
        'n' => new(PieceColor.Black, PieceType.Knight), 'p' => new(PieceColor.Black, PieceType.Pawn),
        _ => null
    };
}
