namespace Chess.Application.Services;

public interface IEmailService
{
    Task SendRecoveryEmailAsync(string email, string token);
    Task SendEmailVerificationAsync(string email, string verificationToken);
}
