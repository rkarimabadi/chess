namespace Chess.Application.DTOs;

public sealed record PagedResult<T>(List<T> Items, int TotalCount, int Page, int PageSize);

public sealed record SuccessResponse(bool Success);
public sealed record ApiResponse<T>(bool Success, T? Data, string? Error);
