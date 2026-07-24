using Chess.Application.Common;
using Chess.Application.DTOs;
using Chess.Application.Ports;
using Chess.Domain.Entities;

namespace Chess.Application.UseCases.Auth;

public sealed class DeactivateAccount : UseCaseBase<DeactivateAccountRequest, SuccessResponse>
{
    public DeactivateAccount(IUnitOfWork uow) : base(uow) { }

    public override async Task<SuccessResponse> ExecuteAsync(DeactivateAccountRequest request, CancellationToken ct = default)
    {
        var user = await UoW.Users.GetByIdAsync(request.UserId);
        if (user is null)
            throw new InvalidOperationException("User not found");

        user.MarkDeleted();
        UoW.Users.Update(user);
        await UoW.SaveChangesAsync(ct);

        return new SuccessResponse(true);
    }
}
