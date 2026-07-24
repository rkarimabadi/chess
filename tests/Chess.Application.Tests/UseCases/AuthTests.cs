using Chess.Application.DTOs;
using Chess.Application.Ports;
using Chess.Application.UseCases.Auth;
using Chess.Domain.Entities;
using Chess.Domain.Interfaces;
using Moq;

namespace Chess.Application.Tests.UseCases;

public class RegisterUserTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly RegisterUser _sut;

    public RegisterUserTests()
    {
        _uow.Setup(u => u.Users).Returns(_users.Object);
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);
        _hasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("hashed_password");
        _sut = new RegisterUser(_uow.Object, _hasher.Object);
    }

    [Fact]
    public async Task HappyPath_ShouldCreateUser()
    {
        _users.Setup(u => u.UsernameExistsAsync("testuser")).ReturnsAsync(false);
        _users.Setup(u => u.EmailExistsAsync("test@test.com")).ReturnsAsync(false);
        _users.Setup(u => u.AddAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

        var result = await _sut.ExecuteAsync(new RegisterRequest("testuser", "test@test.com", "password123"));

        Assert.NotEqual(Guid.Empty, result.UserId);
        Assert.Equal("testuser", result.Username);
        _users.Verify(u => u.AddAsync(It.IsAny<User>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task DuplicateUsername_ShouldThrow()
    {
        _users.Setup(u => u.UsernameExistsAsync("existing")).ReturnsAsync(true);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.ExecuteAsync(new RegisterRequest("existing", "new@test.com", "password")));
    }

    [Fact]
    public async Task DuplicateEmail_ShouldThrow()
    {
        _users.Setup(u => u.UsernameExistsAsync("newuser")).ReturnsAsync(false);
        _users.Setup(u => u.EmailExistsAsync("existing@test.com")).ReturnsAsync(true);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.ExecuteAsync(new RegisterRequest("newuser", "existing@test.com", "password")));
    }
}

public class LoginUserTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly LoginUser _sut;

    public LoginUserTests()
    {
        _uow.Setup(u => u.Users).Returns(_users.Object);
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);
        _sut = new LoginUser(_uow.Object, _hasher.Object);
    }

    [Fact]
    public async Task HappyPath_ShouldReturnAuthResponse()
    {
        var user = User.Create("testuser", "test@test.com", "hashed");
        _users.Setup(u => u.GetByUsernameAsync("testuser")).ReturnsAsync(user);
        _hasher.Setup(h => h.Verify("password", "hashed")).Returns(true);

        var result = await _sut.ExecuteAsync(new LoginRequest("testuser", "password"));

        Assert.Equal(user.Id, result.UserId);
        Assert.Equal("testuser", result.Username);
    }

    [Fact]
    public async Task InvalidUsername_ShouldThrow()
    {
        _users.Setup(u => u.GetByUsernameAsync("nonexistent")).ReturnsAsync((User?)null);
        _users.Setup(u => u.GetByEmailAsync("nonexistent")).ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _sut.ExecuteAsync(new LoginRequest("nonexistent", "password")));
    }

    [Fact]
    public async Task WrongPassword_ShouldThrow()
    {
        var user = User.Create("testuser", "test@test.com", "hashed");
        _users.Setup(u => u.GetByUsernameAsync("testuser")).ReturnsAsync(user);
        _hasher.Setup(h => h.Verify("wrongpassword", "hashed")).Returns(false);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _sut.ExecuteAsync(new LoginRequest("testuser", "wrongpassword")));
    }
}

public class DeactivateAccountTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly DeactivateAccount _sut;

    public DeactivateAccountTests()
    {
        _uow.Setup(u => u.Users).Returns(_users.Object);
        _uow.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);
        _sut = new DeactivateAccount(_uow.Object);
    }

    [Fact]
    public async Task HappyPath_ShouldDeactivateUser()
    {
        var user = User.Create("testuser", "test@test.com", "hashed");
        _users.Setup(u => u.GetByIdAsync(user.Id)).ReturnsAsync(user);

        var result = await _sut.ExecuteAsync(new DeactivateAccountRequest(user.Id));

        Assert.True(result.Success);
        _users.Verify(u => u.Update(user), Times.Once);
    }

    [Fact]
    public async Task NonexistentUser_ShouldThrow()
    {
        _users.Setup(u => u.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.ExecuteAsync(new DeactivateAccountRequest(Guid.NewGuid())));
    }
}
