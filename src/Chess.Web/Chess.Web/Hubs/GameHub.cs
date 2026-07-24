using System.Collections.Concurrent;
using System.Security.Claims;
using Chess.Application.Ports;
using Chess.Application.Services;
using Chess.Application.DTOs;
using Chess.Domain.Chess;
using Chess.Domain.Chess.Rules;
using Chess.Domain.Entities;
using Chess.Domain.ValueObjects;
using Chess.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Chess.Web.Hubs;

public class GameHub : Hub<IGameHub>
{
    private readonly IGameStateManager _stateManager;
    private readonly IRuleSet _ruleSet;
    private readonly IClockService _clockService;
    private readonly IRatingService _ratingService;
    private readonly IUnitOfWork _uow;
    private readonly IMatchmakingService _matchmaking;

    // Simple in-memory queue for matching (server-side)
    private static readonly ConcurrentDictionary<Guid, QueueTicket> _queue = new();

    public GameHub(
        IGameStateManager stateManager,
        IRuleSet ruleSet,
        IClockService clockService,
        IRatingService ratingService,
        IUnitOfWork uow,
        IMatchmakingService matchmaking)
    {
        _stateManager = stateManager;
        _ruleSet = ruleSet;
        _clockService = clockService;
        _ratingService = ratingService;
        _uow = uow;
        _matchmaking = matchmaking;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        // Remove from queue if disconnecting while queued
        _queue.TryRemove(userId, out _);

        // Check if user is in any open rooms and handle cleanup
        try
        {
            var openRooms = await _uow.Rooms.GetOpenRoomsAsync(1, 100);
            foreach (var room in openRooms)
            {
                if (room.HostId == userId)
                {
                    room.Close();
                    _uow.Rooms.Update(room);
                    await _uow.SaveChangesAsync();
                    await Clients.Group($"room:{room.Id}").RoomClosed(room.Id.ToString());
                    await BroadcastRoomListUpdate();
                }
                else if (room.GuestId == userId)
                {
                    room.LeaveGuest();
                    _uow.Rooms.Update(room);
                    await _uow.SaveChangesAsync();
                    await Clients.Group($"room:{room.Id}").GuestLeft(room.Id.ToString());
                    await BroadcastRoomListUpdate();
                }
            }
        }
        catch { /* Best-effort room cleanup on disconnect */ }

        var state = await FindActiveGameStateForUser(userId);
        if (state != null)
        {
            var color = GetColor(state, userId);
            if (color == PieceColor.White)
            {
                state.WhiteConnected = false;
                state.WhiteDisconnectedAt = DateTime.UtcNow;
            }
            else
            {
                state.BlackConnected = false;
                state.BlackDisconnectedAt = DateTime.UtcNow;
            }
            await _stateManager.UpsertAsync(state.GameId, state);
            await Clients.Group(GetGameGroupId(state.GameId)).OpponentDisconnected(LiveGameState.ReconnectTimeoutSeconds);
        }
        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinGame(Guid gameId)
    {
        var userId = GetUserId();
        var state = await _stateManager.GetAsync(gameId);
        if (state == null)
        {
            var game = await _uow.Games.GetByIdAsync(gameId);
            if (game == null) return;

            state = new LiveGameState
            {
                GameId = gameId,
                Board = BoardState.FromFen(game.CurrentFen),
                CurrentTurn = PieceColor.White,
                WhiteTimeMs = game.WhiteTimeRemainingMs,
                BlackTimeMs = game.BlackTimeRemainingMs,
                PositionHistory = new List<string> { game.CurrentFen }
            };
            await _stateManager.UpsertAsync(gameId, state);
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, GetGameGroupId(gameId));
        Context.Items["GameId"] = gameId;

        var color = GetColor(state, userId);
        if (color == PieceColor.White)
        {
            state.WhiteConnected = true;
            state.WhiteDisconnectedAt = null;
        }
        else
        {
            state.BlackConnected = true;
            state.BlackDisconnectedAt = null;
        }
        await _stateManager.UpsertAsync(gameId, state);

        await Clients.Caller.GameStateChanged(MapToDto(state, color));

        // Notify opponent that this player reconnected
        var opponentId = GetOpponentId(state);
        await Clients.User(opponentId.ToString()).OpponentReconnected();
    }

    public async Task MakeMove(string gameId, string from, string to, string? promotion)
    {
        var state = await _stateManager.GetAsync(Guid.Parse(gameId));
        if (state == null) return;

        var fromSq = Square.Parse(from);
        var toSq = Square.Parse(to);
        PieceType? promo = promotion switch
        {
            "Q" or "Queen" => PieceType.Queen,
            "R" or "Rook" => PieceType.Rook,
            "B" or "Bishop" => PieceType.Bishop,
            "N" or "Knight" => PieceType.Knight,
            _ => null
        };

        var result = _ruleSet.ValidateMove(state.Board, fromSq, toSq, promo);

        if (result.Status == MoveResultStatus.Illegal)
        {
            var color = GetColor(state, GetUserId());
            await Clients.Caller.MoveRejected(result.Reason ?? "حرکت غیرمجاز", MapToDto(state, color));
            return;
        }

        var fenBefore = state.Board.ToFen();
        var sim = MoveGenerator.SimulateMove(state.Board, new Chess.Domain.Chess.Move(fromSq, toSq,
            state.Board.GetPiece(fromSq)!, state.Board.GetPiece(toSq), false, false, false, promo));
        var fenAfter = sim.ToFen();

        var moveRecord = MoveRecord.Create(
            Guid.Parse(gameId),
            state.MoveHistory.Count + 1,
            new Chess.Domain.Chess.Move(fromSq, toSq, state.Board.GetPiece(fromSq)!, state.Board.GetPiece(toSq),
                false, false, false, promo),
            result.SanNotation ?? "",
            fenBefore,
            fenAfter,
            result.Status == MoveResultStatus.Check,
            result.Status == MoveResultStatus.Checkmate);

        state.MoveHistory.Add(moveRecord);
        await _uow.Moves.AddAsync(moveRecord);
        await _uow.SaveChangesAsync();
        state.Board = sim;
        state.PositionHistory.Add(fenAfter);
        state.CurrentTurn = state.CurrentTurn == PieceColor.White ? PieceColor.Black : PieceColor.White;

        await _stateManager.UpsertAsync(Guid.Parse(gameId), state);

        // Send personalized DTO to each player (different CallerColor)
        var game = _uow.Games.GetByIdAsync(Guid.Parse(gameId)).Result;
        if (game != null)
        {
            var whiteDto = MapToDto(state, PieceColor.White);
            var blackDto = MapToDto(state, PieceColor.Black);
            await Clients.User(game.WhitePlayerId.ToString()).GameStateChanged(whiteDto);
            await Clients.User(game.BlackPlayerId.ToString()).GameStateChanged(blackDto);
        }
        // Spectators get a neutral DTO (CallerColor doesn't matter for them)
        var spectatorDto = MapToDto(state, PieceColor.White);
        await Clients.Group(GetSpectatorGroupId(Guid.Parse(gameId))).SpectatorGameState(spectatorDto);

        if (result.Status == MoveResultStatus.Checkmate)
        {
            var winner = state.CurrentTurn == PieceColor.White ? GameResult.BlackWins : GameResult.WhiteWins;
            await FinishGameAsync(Guid.Parse(gameId), state, winner, ResultReason.Checkmate);
        }
        else if (result.Status == MoveResultStatus.Stalemate)
        {
            await FinishGameAsync(Guid.Parse(gameId), state, GameResult.Draw, ResultReason.Stalemate);
        }
        else if (result.Status == MoveResultStatus.Check)
        {
            await Clients.Group(GetGameGroupId(Guid.Parse(gameId))).CheckDetected(state.CurrentTurn.ToString());
        }
    }

    public async Task OfferDraw(string gameId)
    {
        var state = await _stateManager.GetAsync(Guid.Parse(gameId));
        if (state == null) return;

        // Validate: game must be active
        var game = await _uow.Games.GetByIdAsync(Guid.Parse(gameId));
        if (game == null || game.Status != GameStatus.Active) return;

        // Validate: cannot offer draw if one is already pending
        if (state.DrawOfferPending)
        {
            await Clients.Caller.DrawResponded(false, GetUserId(), "پیشنهاد تساوی در انتظار پاسخ است");
            return;
        }

        // Validate: cannot offer draw on opponent's turn
        var myColor = GetColor(state, GetUserId());
        if (state.CurrentTurn != myColor)
        {
            await Clients.Caller.DrawResponded(false, GetUserId(), "فقط در نوبت خودتان می‌توانید تساوی پیشنهاد دهید");
            return;
        }

        state.DrawOfferPending = true;
        await _stateManager.UpsertAsync(Guid.Parse(gameId), state);

        var opponentId = GetOpponentId(state);
        await Clients.User(opponentId.ToString()).DrawOffered(GetUserId());
    }

    public async Task RespondDraw(string gameId, bool accept)
    {
        var state = await _stateManager.GetAsync(Guid.Parse(gameId));
        if (state == null) return;

        state.DrawOfferPending = false;
        await _stateManager.UpsertAsync(Guid.Parse(gameId), state);

        if (accept)
        {
            await FinishGameAsync(Guid.Parse(gameId), state, GameResult.Draw, ResultReason.Agreement);
        }
        else
        {
            await Clients.Group(GetGameGroupId(Guid.Parse(gameId))).DrawResponded(false, GetUserId(), "پیشنهاد تساوی رد شد");
        }
    }

    public async Task Resign(string gameId)
    {
        var state = await _stateManager.GetAsync(Guid.Parse(gameId));
        if (state == null) return;

        var myColor = GetColor(state, GetUserId());
        var result = myColor == PieceColor.White ? GameResult.BlackWins : GameResult.WhiteWins;
        await FinishGameAsync(Guid.Parse(gameId), state, result, ResultReason.Resignation);
    }

    public async Task SendPresetMessage(string gameId, string messageKey)
    {
        await Clients.Group(GetGameGroupId(Guid.Parse(gameId))).PresetMessage(GetUserId(), messageKey);
    }

    public async Task ProposeRematch(string gameId)
    {
        await Clients.Group(GetGameGroupId(Guid.Parse(gameId))).RematchOffered(GetUserId());
    }

    public async Task AcceptRematch(string gameId, string rematchToken)
    {
        var state = await _stateManager.GetAsync(Guid.Parse(gameId));
        if (state == null) return;

        var oldGame = await _uow.Games.GetByIdAsync(Guid.Parse(gameId));
        if (oldGame == null) return;

        var newGame = Game.Create(
            oldGame.BlackPlayerId, oldGame.WhitePlayerId,
            oldGame.BaseTimeSeconds, oldGame.IncrementSeconds, oldGame.IsRated);

        await _uow.Games.AddAsync(newGame);
        await _uow.SaveChangesAsync();

        var newState = new LiveGameState
        {
            GameId = newGame.Id,
            Board = BoardState.Initial(),
            CurrentTurn = PieceColor.White,
            WhiteTimeMs = newGame.WhiteTimeRemainingMs,
            BlackTimeMs = newGame.BlackTimeRemainingMs,
            PositionHistory = new List<string> { BoardState.Initial().ToFen() }
        };
        await _stateManager.UpsertAsync(newGame.Id, newState);

        await Clients.Group(GetGameGroupId(Guid.Parse(gameId))).RematchAccepted(newGame.Id.ToString());
    }

    public Task SendPromotionChoice(string gameId, string choice)
    {
        // Handle promotion choice
        return Task.CompletedTask;
    }

    public async Task JoinAsSpectator(Guid gameId)
    {
        var userId = GetUserId();
        var spectatorGroup = GetSpectatorGroupId(gameId);

        await Groups.AddToGroupAsync(Context.ConnectionId, spectatorGroup);
        Context.Items["SpectatingGameId"] = gameId;

        // Send current game state to the spectator
        var state = await _stateManager.GetAsync(gameId);
        if (state != null)
        {
            await Clients.Caller.GameStateChanged(MapToDto(state, PieceColor.White));
        }

        // Notify players that a spectator joined
        await Clients.Group(GetGameGroupId(gameId)).SpectatorJoined(userId);
    }

    public async Task LeaveSpectator(Guid gameId)
    {
        var userId = GetUserId();
        var spectatorGroup = GetSpectatorGroupId(gameId);

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, spectatorGroup);
        await Clients.Group(GetGameGroupId(gameId)).LeftSpectator(userId);
    }

    public async Task JoinMatchmakingQueue(string timeControl, bool isRated)
    {
        var userId = GetUserId();
        var user = await _uow.Users.GetByIdAsync(userId);
        if (user == null) return;

        // Add to queue
        var ticket = new QueueTicket(userId, user.Rating, timeControl, isRated, DateTime.UtcNow);
        _queue[userId] = ticket;

        await Groups.AddToGroupAsync(Context.ConnectionId, $"matchmaking:{userId}");
        await Clients.Caller.QueueJoined(userId.ToString(), 30);

        // Try to find a match immediately
        await TryMatchPlayersAsync(userId, user.Rating, timeControl, isRated);
    }

    public async Task LeaveMatchmakingQueue()
    {
        var userId = GetUserId();
        _queue.TryRemove(userId, out _);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"matchmaking:{userId}");
        await Clients.Caller.QueueLeft();
    }

    private async Task TryMatchPlayersAsync(Guid requesterId, int requesterRating, string timeControl, bool isRated)
    {
        // Find a matching opponent
        QueueTicket? bestMatch = null;
        int bestWindow = int.MaxValue;

        foreach (var (uid, ticket) in _queue)
        {
            if (uid == requesterId) continue;
            if (ticket.TimeControl != timeControl || ticket.IsRated != isRated) continue;

            int ratingDiff = Math.Abs(ticket.Rating - requesterRating);
            if (ratingDiff <= bestWindow && ratingDiff <= 400)
            {
                bestWindow = ratingDiff;
                bestMatch = ticket;
            }
        }

        if (bestMatch == null) return;

        // Remove both from queue
        _queue.TryRemove(requesterId, out _);
        _queue.TryRemove(bestMatch.UserId, out _);

        // Create game
        var whiteId = Random.Shared.Next(2) == 0 ? requesterId : bestMatch.UserId;
        var blackId = whiteId == requesterId ? bestMatch.UserId : requesterId;

        int baseTime = ParseBaseTime(timeControl);
        int increment = ParseIncrement(timeControl);

        var game = Game.Create(whiteId, blackId, baseTime, increment, isRated);
        await _uow.Games.AddAsync(game);
        await _uow.SaveChangesAsync();

        // Create live state
        var liveState = new LiveGameState
        {
            GameId = game.Id,
            Board = BoardState.Initial(),
            CurrentTurn = PieceColor.White,
            WhiteTimeMs = baseTime * 1000L,
            BlackTimeMs = baseTime * 1000L,
            PositionHistory = new List<string> { BoardState.Initial().ToFen() }
        };
        await _stateManager.UpsertAsync(game.Id, liveState);

        // Notify both players
        await Clients.User(requesterId.ToString()).MatchFound(game.Id.ToString(), bestMatch.UserId, timeControl);
        await Clients.User(bestMatch.UserId.ToString()).MatchFound(game.Id.ToString(), requesterId, timeControl);
    }

    private int ParseBaseTime(string timeControl)
    {
        return timeControl.ToLowerInvariant() switch
        {
            "bullet" => 60,
            "blitz" => 180,
            "rapid" => 600,
            "classic" => 3600,
            "untimed" => 0,
            _ when timeControl.Contains('+') => int.Parse(timeControl.Split('+')[0]),
            _ => 300
        };
    }

    private int ParseIncrement(string timeControl)
    {
        if (timeControl.Contains('+'))
        {
            var parts = timeControl.Split('+');
            return parts.Length > 1 ? int.Parse(parts[1]) : 0;
        }
        return timeControl.ToLowerInvariant() switch
        {
            "blitz" => 2,
            "rapid" => 5,
            _ => 0
        };
    }

    public async Task JoinRoomList()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "room-list");
    }

    public async Task LeaveRoomList()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "room-list");
    }

    private async Task BroadcastRoomListUpdate()
    {
        await Clients.Group("room-list").RoomListUpdated();
    }

    public async Task JoinRoomLive(string roomId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"room:{roomId}");
        var userId = GetUserId();
        await Clients.Group($"room:{roomId}").OpponentJoinedRoom(roomId, userId);
    }

    public async Task LeaveRoomLive(string roomId)
    {
        var userId = GetUserId();
        var room = await _uow.Rooms.GetByIdAsync(Guid.Parse(roomId));
        if (room != null)
        {
            if (room.HostId == userId)
            {
                room.Close();
                _uow.Rooms.Update(room);
                await _uow.SaveChangesAsync();
                await Clients.Group($"room:{roomId}").RoomClosed(roomId);
                await BroadcastRoomListUpdate();
            }
            else if (room.GuestId == userId)
            {
                room.LeaveGuest();
                _uow.Rooms.Update(room);
                await _uow.SaveChangesAsync();
                await Clients.Group($"room:{roomId}").GuestLeft(roomId);
                await BroadcastRoomListUpdate();
            }
        }
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"room:{roomId}");
    }

    public async Task ReadyInRoom(string roomId)
    {
        var userId = GetUserId();
        var room = await _uow.Rooms.GetByIdAsync(Guid.Parse(roomId));
        if (room == null || room.Status != RoomStatus.Waiting) return;
        if (room.HostId != userId && room.GuestId != userId) return;

        if (room.HostId == userId)
            room.ReadyHost();
        else
            room.ReadyGuest();

        _uow.Rooms.Update(room);

        if (room.BothReady)
        {
            var game = Chess.Domain.Entities.Game.Create(
                room.HostId,
                room.GuestId!.Value,
                room.BaseTimeSeconds,
                room.IncrementSeconds,
                room.IsRated);

            await _uow.Games.AddAsync(game);
            room.Close();
            await _uow.SaveChangesAsync();

            var timeControl = FormatTimeControl(room.BaseTimeSeconds, room.IncrementSeconds);
            await Clients.User(room.HostId.ToString()).MatchFound(game.Id.ToString(), room.GuestId.Value, timeControl);
            await Clients.User(room.GuestId.Value.ToString()).MatchFound(game.Id.ToString(), room.HostId, timeControl);
        }
        else
        {
            await _uow.SaveChangesAsync();
            await Clients.Group($"room:{roomId}").RoomReady(roomId);
        }
    }

    private static string FormatTimeControl(int baseSeconds, int increment)
    {
        if (increment > 0) return $"{baseSeconds}+{increment}";
        return baseSeconds switch
        {
            60 => "bullet",
            180 => "blitz",
            600 => "rapid",
            3600 => "classic",
            _ => baseSeconds.ToString()
        };
    }

    public async Task SubmitReport(Guid targetUserId, string reason, Guid? gameId, string? note)
    {
        var reporterId = GetUserId();
        var report = Chess.Domain.Entities.PlayerReport.Create(reporterId, targetUserId,
            Enum.Parse<Chess.Domain.ValueObjects.ReportReason>(reason), gameId, note);
        await _uow.Reports.AddAsync(report);
        await _uow.SaveChangesAsync();
    }

    private async Task FinishGameAsync(Guid gameId, LiveGameState state, GameResult result, ResultReason reason)
    {
        var game = await _uow.Games.GetByIdAsync(gameId);
        if (game == null) return;

        game.Finish(result, reason);

        if (game.IsRated)
        {
            var whiteUser = await _uow.Users.GetByIdAsync(game.WhitePlayerId);
            var blackUser = await _uow.Users.GetByIdAsync(game.BlackPlayerId);
            if (whiteUser != null && blackUser != null)
            {
                var rating = _ratingService.Calculate(whiteUser.Rating, blackUser.Rating, result, true);
                whiteUser.SetRating(rating.WhiteNewRating);
                blackUser.SetRating(rating.BlackNewRating);
                whiteUser.IncrementGamesPlayed();
                blackUser.IncrementGamesPlayed();
            }
        }

        await _uow.SaveChangesAsync();
        await _stateManager.RemoveAsync(gameId);

        var gameResultDto = new GameResultDto
        {
            GameId = gameId,
            Result = result.ToString(),
            Reason = reason.ToString()
        };
        await Clients.Group(GetGameGroupId(gameId)).GameFinished(gameResultDto);
        await Clients.Group(GetSpectatorGroupId(gameId)).GameFinished(gameResultDto);
    }

    private string GetGameGroupId(Guid gameId) => gameId.ToString();
    private string GetSpectatorGroupId(Guid gameId) => $"spectators:{gameId}";

    private Guid GetUserId()
    {
        var claim = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        return claim != null ? Guid.Parse(claim) : Guid.Empty;
    }

    private PieceColor GetColor(LiveGameState state, Guid userId)
    {
        var game = _uow.Games.GetByIdAsync(state.GameId).Result;
        if (game == null) return PieceColor.White;
        return game.WhitePlayerId == userId ? PieceColor.White : PieceColor.Black;
    }

    private Guid GetOpponentId(LiveGameState state)
    {
        var game = _uow.Games.GetByIdAsync(state.GameId).Result;
        if (game == null) return Guid.Empty;
        return game.WhitePlayerId == GetUserId() ? game.BlackPlayerId : game.WhitePlayerId;
    }

    private async Task<LiveGameState?> FindActiveGameStateForUser(Guid userId)
    {
        if (_stateManager is InMemoryGameStateManager mgr)
        {
            foreach (var state in mgr.GetAllActive())
            {
                var game = await _uow.Games.GetByIdAsync(state.GameId);
                if (game != null && (game.WhitePlayerId == userId || game.BlackPlayerId == userId))
                    return state;
            }
        }
        return null;
    }

    private GameStateDto MapToDto(LiveGameState state, PieceColor callerColor)
    {
        var game = _uow.Games.GetByIdAsync(state.GameId).Result;
        var whiteUser = game != null ? _uow.Users.GetByIdAsync(game.WhitePlayerId).Result : null;
        var blackUser = game != null ? _uow.Users.GetByIdAsync(game.BlackPlayerId).Result : null;

        string? lastFrom = null, lastTo = null;
        if (state.MoveHistory.Count > 0)
        {
            var lastMove = state.MoveHistory[^1];
            lastFrom = lastMove.From.ToAlgebraic();
            lastTo = lastMove.To.ToAlgebraic();
        }

        var capturedByWhite = new List<string>();
        var capturedByBlack = new List<string>();
        foreach (var m in state.MoveHistory)
        {
            if (m.CapturedPiece != null)
            {
                var charVal = m.CapturedPiece.ToChar();
                if (m.Piece.Color == PieceColor.White)
                    capturedByWhite.Add(charVal.ToString());
                else
                    capturedByBlack.Add(charVal.ToString());
            }
        }

        return new GameStateDto
        {
            GameId = state.GameId,
            Status = "Active",
            IsRated = game?.IsRated ?? false,
            Variant = game?.Variant ?? "Classic",
            TimeControl = new TimeControlDto(game?.BaseTimeSeconds ?? 300, game?.IncrementSeconds ?? 0),
            White = new PlayerDto(game?.WhitePlayerId ?? Guid.Empty, whiteUser?.Username ?? "", whiteUser?.Rating ?? 1200),
            Black = new PlayerDto(game?.BlackPlayerId ?? Guid.Empty, blackUser?.Username ?? "", blackUser?.Rating ?? 1200),
            CurrentTurn = state.CurrentTurn.ToString(),
            CallerColor = callerColor.ToString(),
            BoardFen = state.Board.ToFen(),
            WhiteTimeMs = state.WhiteTimeMs,
            BlackTimeMs = state.BlackTimeMs,
            MoveCount = state.MoveHistory.Count,
            Moves = state.MoveHistory.Select(m => m.SanNotation).ToList(),
            DrawOfferPending = state.DrawOfferPending,
            LastMoveFrom = lastFrom,
            LastMoveTo = lastTo,
            Material = new MaterialDto(capturedByWhite, capturedByBlack)
        };
    }

    private record QueueTicket(Guid UserId, int Rating, string TimeControl, bool IsRated, DateTime QueuedAt);
}
