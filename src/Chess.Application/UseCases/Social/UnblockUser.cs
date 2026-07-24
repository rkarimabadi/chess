using Chess.Application.Common;
using Chess.Application.Common.Authorization;
using Chess.Application.DTOs;
using Chess.Application.Ports;

namespace Chess.Application.UseCases.Social;

public sealed class UnblockUser : UseCaseBase<(Guid UserId, Guid TargetUserId), SuccessResponse>
{
    private readonly IPermissionChecker _permissions;

    public UnblockUser(IUnitOfWork uow, IPermissionChecker permissions) : base(uow)
    {
        _permissions = permissions;
    }

    public override async Task<SuccessResponse> ExecuteAsync((Guid UserId, Guid TargetUserId) request, CancellationToken ct = default)
    {
        if (!_permissions.IsUser(request.UserId))
            throw new UnauthorizedAccessException("User not authorized");

        var block = await UoW.UserBlocks.GetAsync(request.UserId, request.TargetUserId);
        if (block is null)
            throw new InvalidOperationException("Block not found");

        UoW.UserBlocks.Remove(block);
        await UoW.SaveChangesAsync(ct);

        return new SuccessResponse(true);
    }
}
