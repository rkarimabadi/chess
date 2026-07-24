using Chess.Application.Ports;
using Chess.Domain.Entities;
using Chess.Domain.ValueObjects;
using Chess.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Chess.Infrastructure.Repositories;

public sealed class ReportRepository : IReportRepository
{
    private readonly ChessDbContext _db;

    public ReportRepository(ChessDbContext db) => _db = db;

    public async Task<PlayerReport?> GetByIdAsync(Guid id) =>
        await _db.PlayerReports.FindAsync(id);

    public async Task<IReadOnlyList<PlayerReport>> GetOpenReportsAsync(int page, int pageSize) =>
        await _db.PlayerReports
            .Where(r => r.Status == ReportStatus.Open || r.Status == ReportStatus.InReview)
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

    public async Task<IReadOnlyList<PlayerReport>> GetByStatusAsync(ReportStatus status, int page, int pageSize) =>
        await _db.PlayerReports
            .Where(r => r.Status == status)
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

    public async Task<IReadOnlyList<PlayerReport>> GetByTargetUserIdAsync(Guid userId) =>
        await _db.PlayerReports
            .Where(r => r.TargetUserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

    public async Task AddAsync(PlayerReport report) =>
        await _db.PlayerReports.AddAsync(report);

    public void Update(PlayerReport report) =>
        _db.PlayerReports.Update(report);
}
