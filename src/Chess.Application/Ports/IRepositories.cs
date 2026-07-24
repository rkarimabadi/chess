using Chess.Domain.Entities;
using Chess.Domain.ValueObjects;

namespace Chess.Application.Ports;

public interface IUnitOfWork
{
    IUserRepository Users { get; }
    IGameRepository Games { get; }
    IMoveRepository Moves { get; }
    IRoomRepository Rooms { get; }
    IRatingRepository Ratings { get; }
    IReportRepository Reports { get; }
    ISanctionRepository Sanctions { get; }
    IAuditRepository Audit { get; }
    IFriendshipRepository Friendships { get; }
    IUserBlockRepository UserBlocks { get; }
    IStaffNoteRepository StaffNotes { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetByEmailAsync(string email);
    Task<bool> UsernameExistsAsync(string username);
    Task<bool> EmailExistsAsync(string email);
    Task<IReadOnlyList<User>> SearchAsync(string query, int page, int pageSize);
    Task AddAsync(User user);
    void Update(User user);
}

public interface IGameRepository
{
    Task<Game?> GetByIdAsync(Guid id);
    Task AddAsync(Game game);
    Task<IReadOnlyList<Game>> GetUserHistoryAsync(Guid userId, int page, int pageSize);
    Task<int> GetUserHistoryCountAsync(Guid userId);
    Task<IReadOnlyList<Game>> GetActiveGamesAsync();
    Task<IReadOnlyList<Game>> GetUserActiveGamesAsync(Guid userId);
    Task<int> GetActivePlayerCountAsync();
    Task<IReadOnlyList<Game>> GetSpectatableGamesAsync(int page, int pageSize);
    void Update(Game game);
}

public interface IMoveRepository
{
    Task<IReadOnlyList<MoveRecord>> GetByGameIdAsync(Guid gameId);
    Task AddAsync(MoveRecord move);
}

public interface IRoomRepository
{
    Task<Room?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<Room>> GetOpenRoomsAsync(int page, int pageSize);
    Task AddAsync(Room room);
    Task<int> CleanupExpiredAsync();
    void Update(Room room);
}

public interface IRatingRepository
{
    Task<IReadOnlyList<RatingChange>> GetByPlayerIdAsync(Guid playerId, int page, int pageSize);
    Task AddAsync(RatingChange ratingChange);
}

public interface IReportRepository
{
    Task<PlayerReport?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<PlayerReport>> GetOpenReportsAsync(int page, int pageSize);
    Task<IReadOnlyList<PlayerReport>> GetByStatusAsync(ReportStatus status, int page, int pageSize);
    Task<IReadOnlyList<PlayerReport>> GetByTargetUserIdAsync(Guid userId);
    Task AddAsync(PlayerReport report);
    void Update(PlayerReport report);
}

public interface ISanctionRepository
{
    Task<IReadOnlyList<UserSanction>> GetActiveByUserIdAsync(Guid userId);
    Task<UserSanction?> GetByIdAsync(Guid id);
    Task AddAsync(UserSanction sanction);
    void Update(UserSanction sanction);
    Task<int> ExpireStaleBansAsync();
    Task<int> GetRecentBansCountAsync(int days);
}

public interface IAuditRepository
{
    Task AddAsync(StaffAuditLog log);
    Task<IReadOnlyList<StaffAuditLog>> GetFilteredAsync(Guid? staffId, string? actionType, DateTime? from, DateTime to, int page, int pageSize);
}

public interface IFriendshipRepository
{
    Task<Friendship?> GetByIdAsync(Guid id);
    Task<Friendship?> GetBetweenAsync(Guid requesterId, Guid addresseeId);
    Task<IReadOnlyList<Friendship>> GetFriendsOfAsync(Guid userId);
    Task<IReadOnlyList<Friendship>> GetPendingRequestsForAsync(Guid userId);
    Task AddAsync(Friendship friendship);
    void Update(Friendship friendship);
}

public interface IUserBlockRepository
{
    Task<UserBlock?> GetAsync(Guid blockerId, Guid blockedUserId);
    Task<bool> IsBlockedAsync(Guid userId1, Guid userId2);
    Task<IReadOnlyList<UserBlock>> GetBlockedByAsync(Guid userId);
    Task AddAsync(UserBlock block);
    void Remove(UserBlock block);
}

public interface IStaffNoteRepository
{
    Task<IReadOnlyList<StaffNote>> GetByUserIdAsync(Guid userId);
    Task AddAsync(StaffNote note);
}
