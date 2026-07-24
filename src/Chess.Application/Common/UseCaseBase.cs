using Chess.Application.Ports;

namespace Chess.Application.Common;

public abstract class UseCaseBase<TRequest, TResponse> : IUseCase<TRequest, TResponse>
{
    protected readonly IUnitOfWork UoW;

    protected UseCaseBase(IUnitOfWork uow) => UoW = uow;

    public abstract Task<TResponse> ExecuteAsync(TRequest request, CancellationToken ct);
}
