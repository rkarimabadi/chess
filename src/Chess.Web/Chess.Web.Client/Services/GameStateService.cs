using Chess.Application.DTOs;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

namespace Chess.Web.Client.Services;

public class GameStateService : IAsyncDisposable
{
    private readonly NavigationManager _nav;
    private HubConnection? _hubConnection;

    public event Action<GameStateDto>? OnGameStateChanged;
    public event Action<string, GameStateDto>? OnMoveRejected;
    public event Action<string>? OnCheckDetected;
    public event Action<GameResultDto>? OnGameFinished;
    public event Action<Guid>? OnDrawOffered;
    public event Action<bool, Guid, string?>? OnDrawResponded;
    public event Action<int>? OnOpponentDisconnected;
    public event Action? OnOpponentReconnected;
    public event Action<Guid, string>? OnPresetMessage;
    public event Action<Guid>? OnRematchOffered;
    public event Action<string>? OnRematchAccepted;

    public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;

    public GameStateService(NavigationManager nav)
    {
        _nav = nav;
    }

    public async Task ConnectAsync()
    {
        _hubConnection = new HubConnectionBuilder()
            .WithUrl(_nav.ToAbsoluteUri("/hubs/game"))
            .WithAutomaticReconnect()
            .Build();

        _hubConnection.On<GameStateDto>("GameStateChanged", dto =>
        {
            OnGameStateChanged?.Invoke(dto);
        });

        _hubConnection.On<string, GameStateDto>("MoveRejected", (reason, dto) =>
        {
            OnMoveRejected?.Invoke(reason, dto);
        });

        _hubConnection.On<string>("CheckDetected", (checkedSide) =>
        {
            OnCheckDetected?.Invoke(checkedSide);
        });

        _hubConnection.On<GameResultDto>("GameFinished", dto =>
        {
            OnGameFinished?.Invoke(dto);
        });

        _hubConnection.On<Guid>("DrawOffered", offeredBy =>
        {
            OnDrawOffered?.Invoke(offeredBy);
        });

        _hubConnection.On<bool, Guid, string?>("DrawResponded", (accepted, respondedById, reason) =>
        {
            OnDrawResponded?.Invoke(accepted, respondedById, reason);
        });

        _hubConnection.On<int>("OpponentDisconnected", secondsLeft =>
        {
            OnOpponentDisconnected?.Invoke(secondsLeft);
        });

        _hubConnection.On("OpponentReconnected", () =>
        {
            OnOpponentReconnected?.Invoke();
        });

        _hubConnection.On<Guid, string>("PresetMessage", (senderId, messageKey) =>
        {
            OnPresetMessage?.Invoke(senderId, messageKey);
        });

        _hubConnection.On<Guid>("RematchOffered", offeredBy =>
        {
            OnRematchOffered?.Invoke(offeredBy);
        });

        _hubConnection.On<string>("RematchAccepted", newGameId =>
        {
            OnRematchAccepted?.Invoke(newGameId);
        });

        await _hubConnection.StartAsync();
    }

    public async Task JoinGameAsync(Guid gameId)
    {
        if (_hubConnection?.State == HubConnectionState.Connected)
            await _hubConnection.SendAsync("JoinGame", gameId);
    }

    public async Task SendMoveAsync(string gameId, string from, string to, string? promotion = null)
    {
        if (_hubConnection?.State == HubConnectionState.Connected)
            await _hubConnection.SendAsync("MakeMove", gameId, from, to, promotion);
    }

    public async Task OfferDrawAsync(string gameId)
    {
        if (_hubConnection?.State == HubConnectionState.Connected)
            await _hubConnection.SendAsync("OfferDraw", gameId);
    }

    public async Task RespondDrawAsync(string gameId, bool accept)
    {
        if (_hubConnection?.State == HubConnectionState.Connected)
            await _hubConnection.SendAsync("RespondDraw", gameId, accept);
    }

    public async Task ResignAsync(string gameId)
    {
        if (_hubConnection?.State == HubConnectionState.Connected)
            await _hubConnection.SendAsync("Resign", gameId);
    }

    public async Task SendPresetMessageAsync(string gameId, string messageKey)
    {
        if (_hubConnection?.State == HubConnectionState.Connected)
            await _hubConnection.SendAsync("SendPresetMessage", gameId, messageKey);
    }

    public async Task ProposeRematchAsync(string gameId)
    {
        if (_hubConnection?.State == HubConnectionState.Connected)
            await _hubConnection.SendAsync("ProposeRematch", gameId);
    }

    public async Task AcceptRematchAsync(string gameId, string rematchToken)
    {
        if (_hubConnection?.State == HubConnectionState.Connected)
            await _hubConnection.SendAsync("AcceptRematch", gameId, rematchToken);
    }

    public async Task DisconnectAsync()
    {
        if (_hubConnection is not null)
        {
            await _hubConnection.DisposeAsync();
            _hubConnection = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
    }
}
