namespace Chess.Domain.ValueObjects;

public sealed record Square(int File, int Rank)
{
    public bool IsValid => File is >= 0 and < 8 && Rank is >= 0 and < 8;
    public string ToAlgebraic() => $"{(char)('a' + File)}{Rank + 1}";
    public static Square Parse(string algebraic) => new(algebraic[0] - 'a', algebraic[1] - '1');
    public static bool TryParse(string algebraic, out Square square)
    {
        square = new Square(0, 0);
        if (algebraic.Length != 2) return false;
        int file = algebraic[0] - 'a'; int rank = algebraic[1] - '1';
        if (file is < 0 or >= 8 || rank is < 0 or >= 8) return false;
        square = new Square(file, rank); return true;
    }
}
