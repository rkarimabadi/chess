using Chess.Application.Common;
using Chess.Application.DTOs;
using Chess.Application.Ports;
using Chess.Domain.Entities;
using Chess.Domain.Interfaces;

namespace Chess.Application.UseCases.Auth;

public sealed class RegisterUser : UseCaseBase<RegisterRequest, AuthResponse>
{
    private readonly IPasswordHasher _passwordHasher;

    public RegisterUser(IUnitOfWork uow, IPasswordHasher passwordHasher) : base(uow)
    {
        _passwordHasher = passwordHasher;
    }

    public override async Task<AuthResponse> ExecuteAsync(RegisterRequest request, CancellationToken ct = default)
    {
        if (await UoW.Users.UsernameExistsAsync(request.Username))
            throw new InvalidOperationException("Username already exists");

        if (await UoW.Users.EmailExistsAsync(request.Email))
            throw new InvalidOperationException("Email already exists");

        var user = Chess.Domain.Entities.User.Create(request.Username, request.Email, _passwordHasher.Hash(request.Password));

        await UoW.Users.AddAsync(user);
        await UoW.SaveChangesAsync(ct);

        return new AuthResponse(user.Id, user.Username, user.Rating, user.Role.ToString());
    }
}
