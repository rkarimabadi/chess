using Chess.Application.Ports;
using Chess.Infrastructure.Data;

namespace Chess.Infrastructure.Repositories;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly ChessDbContext _db;

    public UnitOfWork(
        ChessDbContext db,
        IUserRepository users,
        IGameRepository games,
        IMoveRepository moves,
        IRoomRepository rooms,
        IRatingRepository ratings,
        IReportRepository reports,
        ISanctionRepository sanctions,
        IAuditRepository audit,
        IFriendshipRepository friendships,
        IUserBlockRepository userBlocks,
        IStaffNoteRepository staffNotes)
    {
        _db = db;
        Users = users;
        Games = games;
        Moves = moves;
        Rooms = rooms;
        Ratings = ratings;
        Reports = reports;
        Sanctions = sanctions;
        Audit = audit;
        Friendships = friendships;
        UserBlocks = userBlocks;
        StaffNotes = staffNotes;
    }

    public IUserRepository Users { get; }
    public IGameRepository Games { get; }
    public IMoveRepository Moves { get; }
    public IRoomRepository Rooms { get; }
    public IRatingRepository Ratings { get; }
    public IReportRepository Reports { get; }
    public ISanctionRepository Sanctions { get; }
    public IAuditRepository Audit { get; }
    public IFriendshipRepository Friendships { get; }
    public IUserBlockRepository UserBlocks { get; }
    public IStaffNoteRepository StaffNotes { get; }

    public Task<int> SaveChangesAsync(CancellationToken ct = default) =>
        _db.SaveChangesAsync(ct);
}
