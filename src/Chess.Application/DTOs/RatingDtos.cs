namespace Chess.Application.DTOs;

public sealed record RatingChangeDto(int OldRating, int NewRating, int Delta);
