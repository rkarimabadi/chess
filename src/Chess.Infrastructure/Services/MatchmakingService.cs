using System.Collections.Concurrent;
using Chess.Application.Services;

namespace Chess.Infrastructure.Services;

public sealed class MatchmakingService : IMatchmakingService
{
    private readonly ConcurrentQueue<MatchTicket> _queue = new();
    private readonly int[] _ratingWindows = [100, 150, 200, 250, 300, 350, 400];

    public void Enqueue(MatchTicket ticket)
    {
        _queue.Enqueue(ticket);
    }

    public bool TryMatch(Guid userId, int rating, string timeControl, bool isRated, out MatchTicket? opponent)
    {
        opponent = null;

        foreach (var window in _ratingWindows)
        {
            var candidates = new List<MatchTicket>();
            var tempQueue = new ConcurrentQueue<MatchTicket>();

            while (_queue.TryDequeue(out var ticket))
            {
                if (ticket.UserId == userId)
                    continue;

                if (ticket.TimeControl == timeControl &&
                    ticket.IsRated == isRated &&
                    Math.Abs(ticket.Rating - rating) <= window)
                {
                    candidates.Add(ticket);
                }
                else
                {
                    tempQueue.Enqueue(ticket);
                }
            }

            while (tempQueue.TryDequeue(out var ticket))
                _queue.Enqueue(ticket);

            if (candidates.Count > 0)
            {
                opponent = candidates[0];
                foreach (var c in candidates.Skip(1))
                    _queue.Enqueue(c);
                return true;
            }
        }

        return false;
    }

    public bool Cancel(Guid userId)
    {
        var tempQueue = new ConcurrentQueue<MatchTicket>();
        var removed = false;

        while (_queue.TryDequeue(out var ticket))
        {
            if (ticket.UserId == userId)
                removed = true;
            else
                tempQueue.Enqueue(ticket);
        }

        while (tempQueue.TryDequeue(out var ticket))
            _queue.Enqueue(ticket);

        return removed;
    }

    public int GetQueueLength(string? timeControl = null)
    {
        if (timeControl is null)
            return _queue.Count;

        return _queue.ToArray().Count(t => t.TimeControl == timeControl);
    }

    public Task<Guid?> TryMatchAsync(Guid userId, int rating, string timeControl, bool isRated)
    {
        var matched = TryMatch(userId, rating, timeControl, isRated, out var opponent);
        return Task.FromResult(matched && opponent != null ? opponent.UserId : (Guid?)null);
    }

    public Task CancelAsync(Guid userId)
    {
        Cancel(userId);
        return Task.CompletedTask;
    }
}
