using Chess.Web.Client.Services;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddScoped<GameStateService>();
builder.Services.AddScoped<ThemeService>();
builder.Services.AddScoped<ISoundService, SoundService>();
builder.Services.AddScoped<ILocalStorageService, LocalStorageService>();
builder.Services.AddSingleton<ToastService>();

await builder.Build().RunAsync();
