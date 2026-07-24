using Chess.Application.Ports;
using Chess.Domain.Entities;
using Chess.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Chess.Infrastructure.Repositories;

public sealed class AuditRepository : IAuditRepository
{
    private readonly ChessDbContext _db;

    public AuditRepository(ChessDbContext db) => _db = db;

    public async Task AddAsync(StaffAuditLog log) =>
        await _db.StaffAuditLogs.AddAsync(log);

    public async Task<IReadOnlyList<StaffAuditLog>> GetFilteredAsync(
        Guid? staffId, string? actionType, DateTime? from, DateTime to, int page, int pageSize)
    {
        var query = _db.StaffAuditLogs
            .Where(l => l.CreatedAt <= to);

        if (staffId.HasValue)
            query = query.Where(l => l.ActorStaffId == staffId.Value);

        if (!string.IsNullOrEmpty(actionType))
            query = query.Where(l => l.ActionType == actionType);

        if (from.HasValue)
            query = query.Where(l => l.CreatedAt >= from.Value);

        return await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }
}
