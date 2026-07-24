using Chess.Domain.Entities;
using Chess.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Chess.Infrastructure.Data;

public sealed class ChessDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Game> Games => Set<Game>();
    public DbSet<MoveRecord> MoveRecords => Set<MoveRecord>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<RatingChange> RatingChanges => Set<RatingChange>();
    public DbSet<PlayerReport> PlayerReports => Set<PlayerReport>();
    public DbSet<UserSanction> UserSanctions => Set<UserSanction>();
    public DbSet<StaffAuditLog> StaffAuditLogs => Set<StaffAuditLog>();
    public DbSet<Friendship> Friendships => Set<Friendship>();
    public DbSet<UserBlock> UserBlocks => Set<UserBlock>();
    public DbSet<StaffNote> StaffNotes => Set<StaffNote>();

    public ChessDbContext(DbContextOptions<ChessDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── User ──
        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);
            e.HasIndex(u => u.Username).IsUnique();
            e.HasIndex(u => u.Email).IsUnique();
            e.HasIndex(u => u.Rating);
            e.HasIndex(u => u.Role);
            e.Property(u => u.Username).HasMaxLength(20).IsRequired();
            e.Property(u => u.Email).HasMaxLength(256).IsRequired();
            e.Property(u => u.PasswordHash).HasMaxLength(500).IsRequired();
            e.Property(u => u.Role).HasMaxLength(20).HasConversion<string>().IsRequired();
            e.Property(u => u.Status).HasMaxLength(20).HasConversion<string>().IsRequired();
            e.Ignore(u => u.DomainEvents);
        });

        // ── Game ──
        modelBuilder.Entity<Game>(e =>
        {
            e.HasKey(g => g.Id);
            e.HasIndex(g => g.WhitePlayerId);
            e.HasIndex(g => g.BlackPlayerId);
            e.HasIndex(g => g.Status);
            e.HasIndex(g => g.CreatedAt);
            e.Property(g => g.Status).HasMaxLength(20).HasConversion<string>().IsRequired();
            e.Property(g => g.Result).HasMaxLength(20).HasConversion<string>().IsRequired();
            e.Property(g => g.Reason).HasMaxLength(30).HasConversion<string>().IsRequired();
            e.Property(g => g.Variant).HasMaxLength(20).IsRequired();
            e.Property(g => g.CurrentFen).HasMaxLength(200).IsRequired();
            e.Property(g => g.PositionHistoryJson).HasMaxLength(4000).IsRequired();
            e.Ignore(g => g.DomainEvents);
        });

        // ── MoveRecord ──
        modelBuilder.Entity<MoveRecord>(e =>
        {
            e.HasKey(m => m.Id);
            e.HasIndex(m => new { m.GameId, m.MoveNumber });
            e.Property(m => m.GameId).IsRequired();
            e.Property(m => m.MoveNumber).IsRequired();
            e.Property(m => m.SanNotation).HasMaxLength(10).IsRequired();
            e.Property(m => m.FenBefore).HasMaxLength(200).IsRequired();
            e.Property(m => m.FenAfter).HasMaxLength(200).IsRequired();
            e.Property(m => m.Timestamp).IsRequired();

            // Ignore computed properties (no backing field)
            e.Ignore(m => m.FromSquare);
            e.Ignore(m => m.ToSquare);
            e.Ignore(m => m.PieceChar);
            e.Ignore(m => m.CapturedPieceChar);
            e.Ignore(m => m.PromotionPieceChar);

            // Store Square From/To as algebraic notation strings
            e.Property(m => m.From).HasConversion(
                v => v.ToAlgebraic(),
                v => Square.Parse(v)
            ).HasMaxLength(2).IsRequired();
            e.Property(m => m.To).HasConversion(
                v => v.ToAlgebraic(),
                v => Square.Parse(v)
            ).HasMaxLength(2).IsRequired();

            // Store Piece and CapturedPiece as JSON
            e.Property(m => m.Piece).HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<Piece>(v, (System.Text.Json.JsonSerializerOptions?)null)!
            ).HasMaxLength(200);
            e.Property(m => m.CapturedPiece).HasConversion(
                v => v == null ? null : System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => v == null ? null : System.Text.Json.JsonSerializer.Deserialize<Piece>(v, (System.Text.Json.JsonSerializerOptions?)null)
            ).HasMaxLength(200);
        });

        // ── Room ──
        modelBuilder.Entity<Room>(e =>
        {
            e.HasKey(r => r.Id);
            e.HasIndex(r => r.Status);
            e.HasIndex(r => r.ExpiresAt);
            e.Property(r => r.Status).HasMaxLength(20).HasConversion<string>().IsRequired();
        });

        // ── RatingChange ──
        modelBuilder.Entity<RatingChange>(e =>
        {
            e.HasKey(r => r.Id);
            e.HasIndex(r => r.PlayerId);
            e.HasIndex(r => r.GameId);
        });

        // ── PlayerReport ──
        modelBuilder.Entity<PlayerReport>(e =>
        {
            e.HasKey(r => r.Id);
            e.HasIndex(r => r.Status);
            e.HasIndex(r => r.TargetUserId);
            e.HasIndex(r => r.CreatedAt);
            e.Property(r => r.Reason).HasMaxLength(30).HasConversion<string>().IsRequired();
            e.Property(r => r.Note).HasMaxLength(500);
            e.Property(r => r.Status).HasMaxLength(30).HasConversion<string>().IsRequired();
            e.Property(r => r.ResolutionNote).HasMaxLength(500);
        });

        // ── UserSanction ──
        modelBuilder.Entity<UserSanction>(e =>
        {
            e.HasKey(s => s.Id);
            e.HasIndex(s => new { s.UserId, s.IsActive });
            e.HasIndex(s => s.EndsAt);
            e.Property(s => s.Type).HasMaxLength(20).HasConversion<string>().IsRequired();
            e.Property(s => s.Reason).HasMaxLength(500).IsRequired();
        });

        // ── StaffAuditLog ──
        modelBuilder.Entity<StaffAuditLog>(e =>
        {
            e.HasKey(l => l.Id);
            e.HasIndex(l => l.ActorStaffId);
            e.HasIndex(l => new { l.TargetType, l.TargetId });
            e.HasIndex(l => l.CreatedAt);
            e.Property(l => l.ActionType).HasMaxLength(50).IsRequired();
            e.Property(l => l.TargetType).HasMaxLength(30).IsRequired();
            e.Property(l => l.Reason).HasMaxLength(500).IsRequired();
            e.Property(l => l.DetailsJson).HasMaxLength(4000);
        });

        // ── Friendship ──
        modelBuilder.Entity<Friendship>(e =>
        {
            e.HasKey(f => f.Id);
            e.HasIndex(f => new { f.RequesterId, f.AddresseeId }).IsUnique();
            e.HasIndex(f => new { f.AddresseeId, f.Status });
            e.Property(f => f.Status).HasMaxLength(20).HasConversion<string>().IsRequired();
        });

        // ── UserBlock ──
        modelBuilder.Entity<UserBlock>(e =>
        {
            e.HasKey(b => b.Id);
            e.HasIndex(b => new { b.BlockerId, b.BlockedUserId }).IsUnique();
        });

        // ── StaffNote ──
        modelBuilder.Entity<StaffNote>(e =>
        {
            e.HasKey(n => n.Id);
            e.HasIndex(n => n.UserId);
            e.Property(n => n.Body).HasMaxLength(2000).IsRequired();
        });

        // Ignore value objects that EF shouldn't map
        modelBuilder.Ignore<Square>();
        modelBuilder.Ignore<Piece>();
    }
}
