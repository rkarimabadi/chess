using Chess.Application.Common;
using Chess.Application.DTOs;
using Chess.Application.Ports;
using Chess.Application.Services;

namespace Chess.Application.UseCases.Game;

public sealed class LeaveQueue : UseCaseBase<Guid, SuccessResponse>
{
    private readonly IMatchmakingService _matchmaking;

    public LeaveQueue(IUnitOfWork uow, IMatchmakingService matchmaking) : base(uow)
    {
        _matchmaking = matchmaking;
    }

    public override async Task<SuccessResponse> ExecuteAsync(Guid userId, CancellationToken ct = default)
    {
        await _matchmaking.CancelAsync(userId);
        return new SuccessResponse(true);
    }
}
