namespace Chess.Application.DTOs;

public sealed record RegisterRequest(string Username, string Email, string Password);
public sealed record LoginRequest(string Login, string Password);
public sealed record AuthResponse(Guid UserId, string Username, int Rating, string Role);
public sealed record RecoverPasswordRequest(string Email);
public sealed record ChangePasswordRequest(string OldPassword, string NewPassword);
public sealed record UpdateProfileRequest(string? DisplayName);
public sealed record DeactivateAccountRequest(Guid UserId);
public sealed record DeleteAccountRequest(Guid UserId, string Confirmation);
