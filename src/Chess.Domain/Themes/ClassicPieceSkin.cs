using Chess.Domain.ValueObjects;

namespace Chess.Domain.Themes;

public sealed class ClassicPieceSkin : IPieceSkin
{
    public string Name => "کلاسیک";
    public string GetSvgPath(Piece piece) => $"/pieces/classic/{piece.Color.ToString().ToLower()}-{piece.Type.ToString().ToLower()}.svg";
}
