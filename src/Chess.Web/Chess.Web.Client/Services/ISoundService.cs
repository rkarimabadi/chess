namespace Chess.Web.Client.Services;

public interface ISoundService
{
    Task PlayMoveAsync();
    Task PlayCaptureAsync();
    Task PlayCheckAsync();
    Task PlayGameOverAsync();
    Task SetMutedAsync(bool muted);
    Task<bool> IsMutedAsync();
}
