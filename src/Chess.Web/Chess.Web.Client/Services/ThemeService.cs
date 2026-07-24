using Microsoft.JSInterop;

namespace Chess.Web.Client.Services;

public class ThemeService
{
    private readonly IJSRuntime _js;

    public ThemeService(IJSRuntime js) => _js = js;

    public async Task<string> GetThemeAsync()
    {
        return await _js.InvokeAsync<string>("themeToggle.getEffectiveTheme");
    }

    public async Task SetThemeAsync(string theme)
    {
        await _js.InvokeVoidAsync("themeToggle.setTheme", theme);
    }

    public async Task<string> ToggleThemeAsync()
    {
        return await _js.InvokeAsync<string>("themeToggle.toggle");
    }
}
