using Chess.Application.Common;
using Chess.Application.DTOs;
using Chess.Application.Ports;
using Chess.Domain.Entities;
using Chess.Domain.Interfaces;

namespace Chess.Application.UseCases.User;

public sealed class ChangePassword : UseCaseBase<(Guid UserId, ChangePasswordRequest Request), SuccessResponse>
{
    private readonly IPasswordHasher _passwordHasher;

    public ChangePassword(IUnitOfWork uow, IPasswordHasher passwordHasher) : base(uow)
    {
        _passwordHasher = passwordHasher;
    }

    public override async Task<SuccessResponse> ExecuteAsync((Guid UserId, ChangePasswordRequest Request) request, CancellationToken ct = default)
    {
        var user = await UoW.Users.GetByIdAsync(request.UserId);
        if (user is null)
            throw new InvalidOperationException("User not found");

        if (!_passwordHasher.Verify(request.Request.OldPassword, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid current password");

        user.UpdatePasswordHash(_passwordHasher.Hash(request.Request.NewPassword));
        UoW.Users.Update(user);
        await UoW.SaveChangesAsync(ct);

        return new SuccessResponse(true);
    }
}
