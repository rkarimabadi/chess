namespace Chess.Domain.Themes;

public interface IBoardSkin
{
    string Name { get; }
    string GetLightSquareCssVar();
    string GetDarkSquareCssVar();
    bool ShowCoordinates { get; }
    string CoordinateStyle { get; }
}
