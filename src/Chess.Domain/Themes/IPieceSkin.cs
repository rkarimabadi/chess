using Chess.Domain.ValueObjects;

namespace Chess.Domain.Themes;

public interface IPieceSkin
{
    string Name { get; }
    string GetSvgPath(Piece piece);
}
