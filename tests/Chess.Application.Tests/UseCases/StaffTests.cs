using Chess.Application.Common.Authorization;
using Chess.Application.DTOs;
using Chess.Application.Ports;
using Chess.Application.Services;
using Chess.Application.UseCases.Staff;
using Chess.Domain.Entities;
using Moq;

namespace Chess.Application.Tests.UseCases;

public class GetStaffDashboardTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IPermissionChecker> _permissions = new();
    private readonly Mock<IGameStateManager> _stateManager = new();
    private readonly Mock<IMatchmakingService> _matchmaking = new();
    private readonly Mock<IReportRepository> _reports = new();
    private readonly Mock<ISanctionRepository> _sanctions = new();
    private readonly Mock<IGameRepository> _games = new();
    private readonly GetStaffDashboard _sut;

    public GetStaffDashboardTests()
    {
        _uow.Setup(u => u.Reports).Returns(_reports.Object);
        _uow.Setup(u => u.Sanctions).Returns(_sanctions.Object);
        _uow.Setup(u => u.Games).Returns(_games.Object);
        _stateManager.Setup(s => s.GetActiveCount()).Returns(5);
        _reports.Setup(r => r.GetOpenReportsAsync(1, 1)).ReturnsAsync(new List<PlayerReport>());
        _sanctions.Setup(s => s.GetRecentBansCountAsync(7)).ReturnsAsync(2);
        _games.Setup(g => g.GetActivePlayerCountAsync()).ReturnsAsync(10);
        _matchmaking.Setup(m => m.GetQueueLength(null)).Returns(3);
        _sut = new GetStaffDashboard(_uow.Object, _permissions.Object, _stateManager.Object, _matchmaking.Object);
    }

    [Fact]
    public async Task HappyPath_Staff_ShouldReturnDashboard()
    {
        var staffId = Guid.NewGuid();
        _permissions.Setup(p => p.IsStaff(staffId)).Returns(true);

        var result = await _sut.ExecuteAsync(staffId);

        Assert.NotNull(result);
        Assert.Equal(5, result.ActiveGames);
        Assert.Equal(10, result.OnlineUsers);
        Assert.Equal(3, result.QueueLength);
        Assert.Equal(2, result.RecentBans);
    }

    [Fact]
    public async Task NonStaff_ShouldThrow()
    {
        var userId = Guid.NewGuid();
        _permissions.Setup(p => p.IsStaff(userId)).Returns(false);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _sut.ExecuteAsync(userId));
    }
}

public class ApplySanctionTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IPermissionChecker> _permissions = new();
    private readonly Mock<ISanctionRepository> _sanctions = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IAuditRepository> _audit = new();
    private readonly ApplySanction _sut;

    public ApplySanctionTests()
    {
        _uow.Setup(u => u.Sanctions).Returns(_sanctions.Object);
        _uow.Setup(u => u.Users).Returns(_users.Object);
        _uow.Setup(u => u.Audit).Returns(_audit.Object);
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);
        _permissions.Setup(p => p.CanBan(It.IsAny<Guid>())).Returns(true);
        _permissions.Setup(p => p.CanPermBan(It.IsAny<Guid>())).Returns(false);
        _sanctions.Setup(s => s.AddAsync(It.IsAny<UserSanction>())).Returns(Task.CompletedTask);
        _audit.Setup(a => a.AddAsync(It.IsAny<StaffAuditLog>())).Returns(Task.CompletedTask);
        _sut = new ApplySanction(_uow.Object, _permissions.Object);
    }

    [Fact]
    public async Task HappyPath_Warn_ShouldCreateSanction()
    {
        var staffId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var target = User.Create("target", "t@test.com", "hashed");
        _users.Setup(u => u.GetByIdAsync(targetId)).ReturnsAsync(target);

        var result = await _sut.ExecuteAsync(new ApplySanctionRequest(staffId, targetId, "Warn", "Bad behavior", null));

        Assert.NotEqual(Guid.Empty, result.SanctionId);
        _sanctions.Verify(s => s.AddAsync(It.IsAny<UserSanction>()), Times.Once);
    }

    [Fact]
    public async Task HappyPath_TempBan_ShouldCreateSanction()
    {
        var staffId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var target = User.Create("target", "t@test.com", "hashed");
        _users.Setup(u => u.GetByIdAsync(targetId)).ReturnsAsync(target);

        var result = await _sut.ExecuteAsync(new ApplySanctionRequest(staffId, targetId, "TempBan", "Spam", 7));

        Assert.NotEqual(Guid.Empty, result.SanctionId);
        Assert.NotNull(result.EndsAt);
    }

    [Fact]
    public async Task NonStaff_ShouldThrow()
    {
        _permissions.Setup(p => p.CanBan(It.IsAny<Guid>())).Returns(false);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _sut.ExecuteAsync(new ApplySanctionRequest(Guid.NewGuid(), Guid.NewGuid(), "Warn", "reason", null)));
    }

    [Fact]
    public async Task TargetNotFound_ShouldThrow()
    {
        _users.Setup(u => u.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.ExecuteAsync(new ApplySanctionRequest(Guid.NewGuid(), Guid.NewGuid(), "Warn", "reason", null)));
    }
}

public class AssignRoleTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IPermissionChecker> _permissions = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IAuditRepository> _audit = new();
    private readonly AssignRole _sut;

    public AssignRoleTests()
    {
        _uow.Setup(u => u.Users).Returns(_users.Object);
        _uow.Setup(u => u.Audit).Returns(_audit.Object);
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);
        _permissions.Setup(p => p.CanManageRoles(It.IsAny<Guid>())).Returns(true);
        _audit.Setup(a => a.AddAsync(It.IsAny<StaffAuditLog>())).Returns(Task.CompletedTask);
        _sut = new AssignRole(_uow.Object, _permissions.Object);
    }

    [Fact]
    public async Task HappyPath_ShouldAssignRole()
    {
        var adminId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var target = User.Create("target", "t@test.com", "hashed");
        _users.Setup(u => u.GetByIdAsync(targetId)).ReturnsAsync(target);

        var result = await _sut.ExecuteAsync((adminId, new AssignRoleRequest(targetId, "Moderator")));

        Assert.True(result.Success);
        _users.Verify(u => u.Update(target), Times.Once);
    }

    [Fact]
    public async Task NonAdmin_ShouldThrow()
    {
        _permissions.Setup(p => p.CanManageRoles(It.IsAny<Guid>())).Returns(false);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _sut.ExecuteAsync((Guid.NewGuid(), new AssignRoleRequest(Guid.NewGuid(), "Admin"))));
    }

    [Fact]
    public async Task InvalidRole_ShouldThrow()
    {
        var adminId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var target = User.Create("target", "t@test.com", "hashed");
        _users.Setup(u => u.GetByIdAsync(targetId)).ReturnsAsync(target);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.ExecuteAsync((adminId, new AssignRoleRequest(targetId, "SuperAdmin"))));
    }
}
