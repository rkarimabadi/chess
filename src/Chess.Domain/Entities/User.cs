using Chess.Domain.Common;
using Chess.Domain.ValueObjects;

namespace Chess.Domain.Entities;

public sealed class User : AggregateRoot
{
    public string Username { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public int Rating { get; private set; } = 1200;
    public int GamesPlayed { get; private set; }
    public UserRole Role { get; private set; } = UserRole.User;
    public UserStatus Status { get; private set; } = UserStatus.Active;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; private set; }
    public bool PresetMessagesMuted { get; private set; }
    public DateTime? PresetMessagesMuteEndsAt { get; private set; }

    private User() { }

    public static User Create(string username, string email, string passwordHash)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            Email = email,
            PasswordHash = passwordHash,
            Rating = 1200,
            Role = UserRole.User,
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void SetRating(int newRating) => Rating = newRating;
    public void IncrementGamesPlayed() => GamesPlayed++;
    public void SetRole(UserRole role) => Role = role;
    public void SetLastLogin(DateTime time) => LastLoginAt = time;
    public void UpdatePasswordHash(string hash) => PasswordHash = hash;
    public void UpdateUsername(string username) => Username = username;
    public void UpdateEmail(string email) => Email = email;
    public void Ban() => Status = UserStatus.Banned;
    public void Unban() => Status = UserStatus.Active;
    public void MarkDeleted() => Status = UserStatus.Deleted;
    public void MutePresetsUntil(DateTime until) { PresetMessagesMuted = true; PresetMessagesMuteEndsAt = until; }
    public void UnmutePresets() { PresetMessagesMuted = false; PresetMessagesMuteEndsAt = null; }
    public bool IsMuted => PresetMessagesMuted && PresetMessagesMuteEndsAt.HasValue && PresetMessagesMuteEndsAt.Value > DateTime.UtcNow;
}
