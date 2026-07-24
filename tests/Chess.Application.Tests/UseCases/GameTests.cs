using Chess.Application.Common.Authorization;
using Chess.Application.DTOs;
using Chess.Application.Ports;
using Chess.Application.Services;
using Chess.Application.UseCases.Game;
using Chess.Domain.Entities;
using Chess.Domain.ValueObjects;
using Moq;

namespace Chess.Application.Tests.UseCases;

public class CreateRoomTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IRoomRepository> _rooms = new();
    private readonly Mock<IPermissionChecker> _permissions = new();
    private readonly CreateRoom _sut;

    public CreateRoomTests()
    {
        _uow.Setup(u => u.Users).Returns(_users.Object);
        _uow.Setup(u => u.Rooms).Returns(_rooms.Object);
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);
        _permissions.Setup(p => p.IsUser(It.IsAny<Guid>())).Returns(true);
        _sut = new CreateRoom(_uow.Object, _permissions.Object);
    }

    [Fact]
    public async Task HappyPath_Blitz_ShouldCreateRoom()
    {
        var userId = Guid.NewGuid();
        var user = User.Create("host", "host@test.com", "hashed");
        _users.Setup(u => u.GetByIdAsync(userId)).ReturnsAsync(user);
        _rooms.Setup(r => r.AddAsync(It.IsAny<Room>())).Returns(Task.CompletedTask);

        var result = await _sut.ExecuteAsync(new CreateRoomRequest(userId, "blitz", false, null));

        Assert.NotEqual(Guid.Empty, result.RoomId);
        _rooms.Verify(r => r.AddAsync(It.IsAny<Room>()), Times.Once);
    }

    [Fact]
    public async Task HappyPath_Untimed_ShouldCreateRoom()
    {
        var userId = Guid.NewGuid();
        var user = User.Create("host", "host@test.com", "hashed");
        _users.Setup(u => u.GetByIdAsync(userId)).ReturnsAsync(user);

        var result = await _sut.ExecuteAsync(new CreateRoomRequest(userId, "untimed", false, null));

        Assert.NotEqual(Guid.Empty, result.RoomId);
    }

    [Fact]
    public async Task UnauthorizedUser_ShouldThrow()
    {
        _permissions.Setup(p => p.IsUser(It.IsAny<Guid>())).Returns(false);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _sut.ExecuteAsync(new CreateRoomRequest(Guid.NewGuid(), "blitz", false, null)));
    }

    [Fact]
    public async Task InactiveUser_ShouldThrow()
    {
        var userId = Guid.NewGuid();
        var user = User.Create("host", "host@test.com", "hashed");
        user.Ban(); // Make inactive
        _users.Setup(u => u.GetByIdAsync(userId)).ReturnsAsync(user);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.ExecuteAsync(new CreateRoomRequest(userId, "blitz", false, null)));
    }
}

public class MakeMoveTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IGameRepository> _games = new();
    private readonly Mock<IGameStateManager> _stateManager = new();
    private readonly Mock<IClockService> _clockService = new();
    private readonly Mock<Chess.Domain.Chess.Rules.IRuleSet> _ruleSet = new();
    private readonly MakeMove _sut;

    public MakeMoveTests()
    {
        _uow.Setup(u => u.Games).Returns(_games.Object);
        _sut = new MakeMove(_uow.Object, _stateManager.Object, _clockService.Object, _ruleSet.Object);
    }

    [Fact]
    public async Task HappyPath_ValidMove_ShouldSucceed()
    {
        var gameId = Guid.NewGuid();
        var whiteId = Guid.NewGuid();
        var game = CreateActiveGame(gameId, whiteId, Guid.NewGuid());
        _games.Setup(g => g.GetByIdAsync(gameId)).ReturnsAsync(game);

        var state = new LiveGameState
        {
            GameId = gameId,
            Board = BoardState.Initial(),
            CurrentTurn = PieceColor.White,
            WhiteTimeMs = 300000,
            BlackTimeMs = 300000
        };
        _stateManager.Setup(s => s.GetAsync(gameId)).ReturnsAsync(state);

        _ruleSet.Setup(r => r.ValidateMove(state.Board, Square.Parse("e2"), Square.Parse("e4"), null))
            .Returns(new Chess.Domain.Chess.Rules.MoveResult
            {
                Status = Chess.Domain.Chess.Rules.MoveResultStatus.Legal,
                SanNotation = "e4"
            });
        _ruleSet.Setup(r => r.IsCheckmate(It.IsAny<BoardState>())).Returns(false);
        _ruleSet.Setup(r => r.IsStalemate(It.IsAny<BoardState>())).Returns(false);
        _ruleSet.Setup(r => r.IsDrawByRules(It.IsAny<BoardState>(), It.IsAny<IReadOnlyList<string>>())).Returns(false);

        _clockService.Setup(c => c.Tick(It.IsAny<LiveGameState>(), PieceColor.White, It.IsAny<TimeSpan>()))
            .Returns(new ClockState(299000, 300000));
        _clockService.Setup(c => c.ApplyIncrement(It.IsAny<LiveGameState>(), PieceColor.White)).Returns(2000);
        _clockService.Setup(c => c.IsFlagged(It.IsAny<ClockState>(), It.IsAny<PieceColor>())).Returns(false);

        var result = await _sut.ExecuteAsync(new MakeMoveRequest(whiteId, gameId, "e2", "e4", null));

        Assert.Equal("Ok", result.Status);
        Assert.Equal("e4", result.SanNotation);
    }

    [Fact]
    public async Task GameNotFound_ShouldThrow()
    {
        _games.Setup(g => g.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Game?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.ExecuteAsync(new MakeMoveRequest(Guid.NewGuid(), Guid.NewGuid(), "e2", "e4", null)));
    }

    [Fact]
    public async Task GameNotActive_ShouldThrow()
    {
        var gameId = Guid.NewGuid();
        var game = CreateActiveGame(gameId, Guid.NewGuid(), Guid.NewGuid());
        game.Finish(GameResult.WhiteWins, ResultReason.Checkmate);
        _games.Setup(g => g.GetByIdAsync(gameId)).ReturnsAsync(game);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.ExecuteAsync(new MakeMoveRequest(Guid.NewGuid(), gameId, "e2", "e4", null)));
    }

    [Fact]
    public async Task NotAPlayer_ShouldThrow()
    {
        var gameId = Guid.NewGuid();
        var game = CreateActiveGame(gameId, Guid.NewGuid(), Guid.NewGuid());
        _games.Setup(g => g.GetByIdAsync(gameId)).ReturnsAsync(game);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _sut.ExecuteAsync(new MakeMoveRequest(Guid.NewGuid(), gameId, "e2", "e4", null)));
    }

    [Fact]
    public async Task NotYourTurn_ShouldThrow()
    {
        var gameId = Guid.NewGuid();
        var whiteId = Guid.NewGuid();
        var game = CreateActiveGame(gameId, whiteId, Guid.NewGuid());
        _games.Setup(g => g.GetByIdAsync(gameId)).ReturnsAsync(game);

        var state = new LiveGameState
        {
            GameId = gameId,
            Board = BoardState.Initial(),
            CurrentTurn = PieceColor.Black, // Not white's turn
            WhiteTimeMs = 300000,
            BlackTimeMs = 300000
        };
        _stateManager.Setup(s => s.GetAsync(gameId)).ReturnsAsync(state);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.ExecuteAsync(new MakeMoveRequest(whiteId, gameId, "e2", "e4", null)));
    }

    [Fact]
    public async Task IllegalMove_ShouldThrow()
    {
        var gameId = Guid.NewGuid();
        var whiteId = Guid.NewGuid();
        var game = CreateActiveGame(gameId, whiteId, Guid.NewGuid());
        _games.Setup(g => g.GetByIdAsync(gameId)).ReturnsAsync(game);

        var state = new LiveGameState
        {
            GameId = gameId,
            Board = BoardState.Initial(),
            CurrentTurn = PieceColor.White,
            WhiteTimeMs = 300000,
            BlackTimeMs = 300000
        };
        _stateManager.Setup(s => s.GetAsync(gameId)).ReturnsAsync(state);

        _ruleSet.Setup(r => r.ValidateMove(state.Board, Square.Parse("e2"), Square.Parse("e5"), null))
            .Returns(new Chess.Domain.Chess.Rules.MoveResult
            {
                Status = Chess.Domain.Chess.Rules.MoveResultStatus.Illegal,
                Reason = "Illegal move"
            });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.ExecuteAsync(new MakeMoveRequest(whiteId, gameId, "e2", "e5", null)));
    }

    private static Game CreateActiveGame(Guid gameId, Guid whiteId, Guid blackId)
    {
        return Game.Create(whiteId, blackId, 180, 2, true);
    }
}

public class OfferDrawTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IGameRepository> _games = new();
    private readonly Mock<IGameStateManager> _stateManager = new();
    private readonly OfferDraw _sut;

    public OfferDrawTests()
    {
        _uow.Setup(u => u.Games).Returns(_games.Object);
        _sut = new OfferDraw(_uow.Object, _stateManager.Object);
    }

    [Fact]
    public async Task HappyPath_ShouldOfferDraw()
    {
        var gameId = Guid.NewGuid();
        var whiteId = Guid.NewGuid();
        var game = Game.Create(whiteId, Guid.NewGuid(), 180, 2, true);
        _games.Setup(g => g.GetByIdAsync(gameId)).ReturnsAsync(game);

        var state = new LiveGameState { GameId = gameId, DrawOfferPending = false };
        _stateManager.Setup(s => s.GetAsync(gameId)).ReturnsAsync(state);

        var result = await _sut.ExecuteAsync((whiteId, gameId));

        Assert.True(result.Success);
        _stateManager.Verify(s => s.UpsertAsync(gameId, It.IsAny<LiveGameState>()), Times.Once);
    }

    [Fact]
    public async Task GameNotFound_ShouldThrow()
    {
        _games.Setup(g => g.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Game?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.ExecuteAsync((Guid.NewGuid(), Guid.NewGuid())));
    }

    [Fact]
    public async Task DrawAlreadyPending_ShouldThrow()
    {
        var gameId = Guid.NewGuid();
        var whiteId = Guid.NewGuid();
        var game = Game.Create(whiteId, Guid.NewGuid(), 180, 2, true);
        game.OfferDraw(whiteId); // Already pending
        _games.Setup(g => g.GetByIdAsync(gameId)).ReturnsAsync(game);

        var state = new LiveGameState { GameId = gameId, DrawOfferPending = true };
        _stateManager.Setup(s => s.GetAsync(gameId)).ReturnsAsync(state);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.ExecuteAsync((whiteId, gameId)));
    }

    [Fact]
    public async Task NotAPlayer_ShouldThrow()
    {
        var gameId = Guid.NewGuid();
        var game = Game.Create(Guid.NewGuid(), Guid.NewGuid(), 180, 2, true);
        _games.Setup(g => g.GetByIdAsync(gameId)).ReturnsAsync(game);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _sut.ExecuteAsync((Guid.NewGuid(), gameId)));
    }
}

public class ResignGameTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IGameRepository> _games = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IGameStateManager> _stateManager = new();
    private readonly Mock<IRatingService> _ratingService = new();
    private readonly ResignGame _sut;

    public ResignGameTests()
    {
        _uow.Setup(u => u.Games).Returns(_games.Object);
        _uow.Setup(u => u.Users).Returns(_users.Object);
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);
        _ratingService.Setup(r => r.Calculate(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<GameResult>(), It.IsAny<bool>()))
            .Returns(new RatingResult { WhiteOldRating = 1200, WhiteNewRating = 1210, WhiteDelta = 10, BlackOldRating = 1200, BlackNewRating = 1190, BlackDelta = -10 });
        _sut = new ResignGame(_uow.Object, _stateManager.Object, _ratingService.Object);
    }

    [Fact]
    public async Task HappyPath_ShouldResign()
    {
        var gameId = Guid.NewGuid();
        var whiteId = Guid.NewGuid();
        var blackId = Guid.NewGuid();
        var game = Game.Create(whiteId, blackId, 180, 2, true);
        _games.Setup(g => g.GetByIdAsync(gameId)).ReturnsAsync(game);

        var whiteUser = User.Create("white", "w@test.com", "hashed");
        var blackUser = User.Create("black", "b@test.com", "hashed");
        _users.Setup(u => u.GetByIdAsync(whiteId)).ReturnsAsync(whiteUser);
        _users.Setup(u => u.GetByIdAsync(blackId)).ReturnsAsync(blackUser);

        var result = await _sut.ExecuteAsync((whiteId, gameId));

        Assert.Equal("BlackWins", result.Result);
        Assert.Equal("Resignation", result.Reason);
        _games.Verify(g => g.Update(game), Times.Once);
    }

    [Fact]
    public async Task GameNotFound_ShouldThrow()
    {
        _games.Setup(g => g.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Game?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.ExecuteAsync((Guid.NewGuid(), Guid.NewGuid())));
    }

    [Fact]
    public async Task NotAPlayer_ShouldThrow()
    {
        var gameId = Guid.NewGuid();
        var game = Game.Create(Guid.NewGuid(), Guid.NewGuid(), 180, 2, true);
        _games.Setup(g => g.GetByIdAsync(gameId)).ReturnsAsync(game);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _sut.ExecuteAsync((Guid.NewGuid(), gameId)));
    }
}
