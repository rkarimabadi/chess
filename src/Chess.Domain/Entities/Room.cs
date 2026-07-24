using Chess.Domain.Common;
using Chess.Domain.ValueObjects;

namespace Chess.Domain.Entities;

public sealed class Room : Entity
{
    public Guid HostId { get; private set; }
    public bool IsRated { get; private set; }
    public int BaseTimeSeconds { get; private set; }
    public int IncrementSeconds { get; private set; }
    public RoomStatus Status { get; private set; } = RoomStatus.Waiting;
    public Guid? GuestId { get; private set; }
    public bool HostReady { get; private set; }
    public bool GuestReady { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; private set; }

    private Room() { }

    public static Room Create(Guid hostId, int baseTime, int increment, bool isRated)
    {
        return new Room { Id = Guid.NewGuid(), HostId = hostId, IsRated = isRated, BaseTimeSeconds = baseTime, IncrementSeconds = increment, ExpiresAt = DateTime.UtcNow.AddMinutes(15) };
    }

    public void Join(Guid guestId) { GuestId = guestId; Status = RoomStatus.Waiting; }
    public void ReadyHost() => HostReady = true;
    public void ReadyGuest() => GuestReady = true;
    public bool BothReady => HostReady && GuestReady;
    public void Close() => Status = RoomStatus.Closed;
    public void Expire() => Status = RoomStatus.Expired;
    public void LeaveGuest() { GuestId = null; GuestReady = false; }
}
