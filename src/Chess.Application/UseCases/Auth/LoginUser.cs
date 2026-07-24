using Chess.Application.Common;
using Chess.Application.DTOs;
using Chess.Application.Ports;
using Chess.Domain.Interfaces;

namespace Chess.Application.UseCases.Auth;

public sealed class LoginUser : UseCaseBase<LoginRequest, AuthResponse>
{
    private readonly IPasswordHasher _passwordHasher;

    public LoginUser(IUnitOfWork uow, IPasswordHasher passwordHasher) : base(uow)
    {
        _passwordHasher = passwordHasher;
    }

    public override async Task<AuthResponse> ExecuteAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await UoW.Users.GetByUsernameAsync(request.Login)
                   ?? await UoW.Users.GetByEmailAsync(request.Login);

        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid credentials");

        user.SetLastLogin(DateTime.UtcNow);
        UoW.Users.Update(user);
        await UoW.SaveChangesAsync(ct);

        return new AuthResponse(user.Id, user.Username, user.Rating, user.Role.ToString());
    }
}
