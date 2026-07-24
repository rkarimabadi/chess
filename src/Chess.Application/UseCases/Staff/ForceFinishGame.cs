using Chess.Application.Common;
using Chess.Application.Common.Authorization;
using Chess.Application.DTOs;
using Chess.Application.Ports;
using Chess.Application.Services;
using Chess.Domain.Entities;
using Chess.Domain.ValueObjects;

namespace Chess.Application.UseCases.Staff;

public sealed class ForceFinishGame : UseCaseBase<(Guid AdminId, Guid GameId, ForceFinishRequest Request), SuccessResponse>
{
    private readonly IPermissionChecker _permissions;
    private readonly IGameStateManager _stateManager;

    public ForceFinishGame(IUnitOfWork uow, IPermissionChecker permissions, IGameStateManager stateManager) : base(uow)
    {
        _permissions = permissions;
        _stateManager = stateManager;
    }

    public override async Task<SuccessResponse> ExecuteAsync((Guid AdminId, Guid GameId, ForceFinishRequest Request) request, CancellationToken ct = default)
    {
        if (!_permissions.IsAdmin(request.AdminId))
            throw new UnauthorizedAccessException("Admin access required");

        var game = await UoW.Games.GetByIdAsync(request.GameId);
        if (game is null)
            throw new InvalidOperationException("Game not found");

        if (game.Status != GameStatus.Active)
            throw new InvalidOperationException("Game is not active");

        game.Finish(GameResult.Aborted, ResultReason.Abort);
        UoW.Games.Update(game);

        // Remove from live state
        await _stateManager.RemoveAsync(request.GameId);

        // Log audit
        var auditLog = StaffAuditLog.Create(
            request.AdminId,
            "ForceFinishGame",
            "Game",
            request.GameId,
            request.Request.Reason);

        await UoW.Audit.AddAsync(auditLog);
        await UoW.SaveChangesAsync(ct);

        return new SuccessResponse(true);
    }
}
