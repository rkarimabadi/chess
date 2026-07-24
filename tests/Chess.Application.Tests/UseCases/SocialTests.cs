using Chess.Application.Common.Authorization;
using Chess.Application.DTOs;
using Chess.Application.Ports;
using Chess.Application.Services;
using Chess.Application.UseCases.Social;
using Chess.Domain.Entities;
using Moq;

namespace Chess.Application.Tests.UseCases;

public class SendPresetMessageTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IGameRepository> _games = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IMetricsService> _metrics = new();
    private readonly SendPresetMessage _sut;

    public SendPresetMessageTests()
    {
        _uow.Setup(u => u.Games).Returns(_games.Object);
        _uow.Setup(u => u.Users).Returns(_users.Object);
        _sut = new SendPresetMessage(_uow.Object, _metrics.Object);
    }

    [Fact]
    public async Task HappyPath_ShouldSend()
    {
        var userId = Guid.NewGuid();
        var gameId = Guid.NewGuid();
        var game = Game.Create(userId, Guid.NewGuid(), 180, 2, true);
        _games.Setup(g => g.GetByIdAsync(gameId)).ReturnsAsync(game);

        var user = User.Create("player", "p@test.com", "hashed");
        _users.Setup(u => u.GetByIdAsync(userId)).ReturnsAsync(user);

        var result = await _sut.ExecuteAsync((userId, gameId, "good_game"));

        Assert.True(result.Success);
        _metrics.Verify(m => m.IncrementCounter("preset_message_sent", It.IsAny<Dictionary<string, string>>()), Times.Once);
    }

    [Fact]
    public async Task GameNotFound_ShouldThrow()
    {
        _games.Setup(g => g.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Game?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.ExecuteAsync((Guid.NewGuid(), Guid.NewGuid(), "good_game")));
    }

    [Fact]
    public async Task NotAPlayer_ShouldThrow()
    {
        var gameId = Guid.NewGuid();
        var game = Game.Create(Guid.NewGuid(), Guid.NewGuid(), 180, 2, true);
        _games.Setup(g => g.GetByIdAsync(gameId)).ReturnsAsync(game);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _sut.ExecuteAsync((Guid.NewGuid(), gameId, "good_game")));
    }

    [Fact]
    public async Task MutedUser_ShouldThrow()
    {
        var userId = Guid.NewGuid();
        var gameId = Guid.NewGuid();
        var game = Game.Create(userId, Guid.NewGuid(), 180, 2, true);
        _games.Setup(g => g.GetByIdAsync(gameId)).ReturnsAsync(game);

        var user = User.Create("player", "p@test.com", "hashed");
        user.MutePresetsUntil(DateTime.UtcNow.AddMinutes(30));
        _users.Setup(u => u.GetByIdAsync(userId)).ReturnsAsync(user);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.ExecuteAsync((userId, gameId, "good_game")));
    }

    [Fact]
    public async Task InvalidMessageKey_ShouldThrow()
    {
        var userId = Guid.NewGuid();
        var gameId = Guid.NewGuid();
        var game = Game.Create(userId, Guid.NewGuid(), 180, 2, true);
        _games.Setup(g => g.GetByIdAsync(gameId)).ReturnsAsync(game);

        var user = User.Create("player", "p@test.com", "hashed");
        _users.Setup(u => u.GetByIdAsync(userId)).ReturnsAsync(user);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.ExecuteAsync((userId, gameId, "invalid_key")));
    }
}

public class SubmitReportTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IReportRepository> _reports = new();
    private readonly Mock<IPermissionChecker> _permissions = new();
    private readonly SubmitReport _sut;

    public SubmitReportTests()
    {
        _uow.Setup(u => u.Users).Returns(_users.Object);
        _uow.Setup(u => u.Reports).Returns(_reports.Object);
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);
        _permissions.Setup(p => p.IsUser(It.IsAny<Guid>())).Returns(true);
        _sut = new SubmitReport(_uow.Object, _permissions.Object);
    }

    [Fact]
    public async Task HappyPath_ShouldCreateReport()
    {
        var reporterId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var reporter = User.Create("reporter", "r@test.com", "hashed");
        var target = User.Create("target", "t@test.com", "hashed");
        _users.Setup(u => u.GetByIdAsync(reporterId)).ReturnsAsync(reporter);
        _users.Setup(u => u.GetByIdAsync(targetId)).ReturnsAsync(target);
        _reports.Setup(r => r.AddAsync(It.IsAny<PlayerReport>())).Returns(Task.CompletedTask);

        var result = await _sut.ExecuteAsync(new SubmitReportRequest(reporterId, targetId, "IntentionalAbandon", null, null));

        Assert.NotEqual(Guid.Empty, result.ReportId);
        _reports.Verify(r => r.AddAsync(It.IsAny<PlayerReport>()), Times.Once);
    }

    [Fact]
    public async Task ReportYourself_ShouldThrow()
    {
        var userId = Guid.NewGuid();
        var user = User.Create("user", "u@test.com", "hashed");
        _users.Setup(u => u.GetByIdAsync(userId)).ReturnsAsync(user);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.ExecuteAsync(new SubmitReportRequest(userId, userId, "IntentionalAbandon", null, null)));
    }

    [Fact]
    public async Task TargetNotFound_ShouldThrow()
    {
        var reporterId = Guid.NewGuid();
        var reporter = User.Create("reporter", "r@test.com", "hashed");
        _users.Setup(u => u.GetByIdAsync(reporterId)).ReturnsAsync(reporter);
        _users.Setup(u => u.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.ExecuteAsync(new SubmitReportRequest(reporterId, Guid.NewGuid(), "IntentionalAbandon", null, null)));
    }

    [Fact]
    public async Task InvalidReason_ShouldThrow()
    {
        var reporterId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var reporter = User.Create("reporter", "r@test.com", "hashed");
        var target = User.Create("target", "t@test.com", "hashed");
        _users.Setup(u => u.GetByIdAsync(reporterId)).ReturnsAsync(reporter);
        _users.Setup(u => u.GetByIdAsync(targetId)).ReturnsAsync(target);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.ExecuteAsync(new SubmitReportRequest(reporterId, targetId, "InvalidReason", null, null)));
    }
}
