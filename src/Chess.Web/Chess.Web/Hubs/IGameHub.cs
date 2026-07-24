using Chess.Application.DTOs;

namespace Chess.Web.Hubs;

public interface IGameHub
{
    Task GameStateChanged(GameStateDto state);
    Task MoveRejected(string reason, GameStateDto newState);
    Task CheckDetected(string checkedSide);
    Task GameFinished(GameResultDto result);
    Task DrawOffered(Guid offeredBy);
    Task DrawResponded(bool accepted, Guid respondedById, string? reason);
    Task OpponentDisconnected(int secondsLeft);
    Task OpponentReconnected();
    Task PresetMessage(Guid senderId, string messageKey);
    Task RematchOffered(Guid offeredBy);
    Task RematchAccepted(string newGameId);
    Task QueueJoined(string queueId, int estimatedWaitSeconds);
    Task QueueLeft();
    Task MatchFound(string roomId, Guid opponentId, string timeControl);
    Task RoomReady(string roomId);
    Task OpponentJoinedRoom(string roomId, Guid opponentId);
    Task GuestLeft(string roomId);
    Task RoomClosed(string roomId);
    Task RoomListUpdated();

    // Player actions
    Task JoinGame(Guid gameId);
    Task MakeMove(string gameId, string from, string to, string? promotion);
    Task OfferDraw(string gameId);
    Task RespondDraw(string gameId, bool accept);
    Task Resign(string gameId);
    Task SendPresetMessage(string gameId, string messageKey);
    Task ProposeRematch(string gameId);
    Task AcceptRematch(string gameId, string rematchToken);
    Task SendPromotionChoice(string gameId, string choice);
    Task JoinMatchmakingQueue(string timeControl, bool isRated);
    Task LeaveMatchmakingQueue();
    Task JoinRoomLive(string roomId);
    Task SubmitReport(Guid targetUserId, string reason, Guid? gameId, string? note);

    // Room List
    Task JoinRoomList();
    Task LeaveRoomList();

    // Spectator
    Task JoinAsSpectator(Guid gameId);
    Task LeaveSpectator(Guid gameId);
    Task SpectatorGameState(GameStateDto state);
    Task SpectatorJoined(Guid spectatorId);
    Task LeftSpectator(Guid spectatorId);
}
