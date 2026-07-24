using Chess.Application.Common;
using Chess.Application.DTOs;
using Chess.Application.Ports;
using Chess.Domain.Entities;

namespace Chess.Application.UseCases.Auth;

public sealed class DeleteAccount : UseCaseBase<DeleteAccountRequest, SuccessResponse>
{
    public DeleteAccount(IUnitOfWork uow) : base(uow) { }

    public override async Task<SuccessResponse> ExecuteAsync(DeleteAccountRequest request, CancellationToken ct = default)
    {
        if (request.Confirmation != "DELETE")
            throw new InvalidOperationException("Confirmation text must be 'DELETE'");

        var user = await UoW.Users.GetByIdAsync(request.UserId);
        if (user is null)
            throw new InvalidOperationException("User not found");

        user.MarkDeleted();
        user.UpdateUsername("[deleted]");
        user.UpdateEmail($"deleted-{user.Id}@deleted.local");
        user.UpdatePasswordHash("");
        UoW.Users.Update(user);
        await UoW.SaveChangesAsync(ct);

        return new SuccessResponse(true);
    }
}
