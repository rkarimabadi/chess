using Chess.Application.Common;
using Chess.Application.DTOs;
using Chess.Application.Ports;

namespace Chess.Application.UseCases.Auth;

public sealed class RecoverPassword : UseCaseBase<RecoverPasswordRequest, SuccessResponse>
{
    public RecoverPassword(IUnitOfWork uow) : base(uow) { }

    public override async Task<SuccessResponse> ExecuteAsync(RecoverPasswordRequest request, CancellationToken ct = default)
    {
        var user = await UoW.Users.GetByEmailAsync(request.Email);
        if (user is null)
            return new SuccessResponse(false);

        // In a real implementation, this would send a password reset email
        // For now, we just return success
        return new SuccessResponse(true);
    }
}
