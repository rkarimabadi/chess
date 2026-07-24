using Chess.Application.Services;
using Microsoft.Extensions.Logging;

namespace Chess.Infrastructure.Services;

public sealed class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;

    public EmailService(ILogger<EmailService> logger)
    {
        _logger = logger;
    }

    public Task SendRecoveryEmailAsync(string email, string token)
    {
        _logger.LogInformation("Password recovery email sent to {Email} with token {Token}", email, token);
        return Task.CompletedTask;
    }

    public Task SendEmailVerificationAsync(string email, string verificationToken)
    {
        _logger.LogInformation("Email verification sent to {Email} with token {Token}", email, verificationToken);
        return Task.CompletedTask;
    }
}
