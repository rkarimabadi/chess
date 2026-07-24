using Microsoft.JSInterop;

namespace Chess.Web.Client.Services;

public class SoundService : ISoundService
{
    private readonly IJSRuntime _js;
    private bool _isMuted;

    public SoundService(IJSRuntime js) => _js = js;

    public async Task PlayMoveAsync()
    {
        if (_isMuted) return;
        await _js.InvokeVoidAsync("audio.playSound", "sounds/move.mp3");
    }

    public async Task PlayCaptureAsync()
    {
        if (_isMuted) return;
        await _js.InvokeVoidAsync("audio.playSound", "sounds/capture.mp3");
    }

    public async Task PlayCheckAsync()
    {
        if (_isMuted) return;
        await _js.InvokeVoidAsync("audio.playSound", "sounds/check.mp3");
    }

    public async Task PlayGameOverAsync()
    {
        if (_isMuted) return;
        await _js.InvokeVoidAsync("audio.playSound", "sounds/gameover.mp3");
    }

    public async Task SetMutedAsync(bool muted)
    {
        _isMuted = muted;
        await _js.InvokeVoidAsync("audio.setMuted", muted);
    }

    public Task<bool> IsMutedAsync()
    {
        return Task.FromResult(_isMuted);
    }
}
