namespace Chess.Domain.Themes;

public sealed class ClassicBoardSkin : IBoardSkin
{
    public string Name => "کلاسیک";
    public string GetLightSquareCssVar() => "var(--color-board-light)";
    public string GetDarkSquareCssVar() => "var(--color-board-dark)";
    public bool ShowCoordinates => true;
    public string CoordinateStyle => "inline";
}
