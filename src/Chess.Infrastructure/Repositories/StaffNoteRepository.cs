using Chess.Application.Ports;
using Chess.Domain.Entities;
using Chess.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Chess.Infrastructure.Repositories;

public sealed class StaffNoteRepository : IStaffNoteRepository
{
    private readonly ChessDbContext _db;

    public StaffNoteRepository(ChessDbContext db) => _db = db;

    public async Task<IReadOnlyList<StaffNote>> GetByUserIdAsync(Guid userId) =>
        await _db.StaffNotes
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

    public async Task AddAsync(StaffNote note) =>
        await _db.StaffNotes.AddAsync(note);
}
