using System.Diagnostics.Metrics;
using Chess.Application.Services;

namespace Chess.Infrastructure.Services;

public sealed class OtelMetricsService : IMetricsService
{
    private readonly Meter _meter;
    private readonly Counter<long> _gamesCreated;
    private readonly Counter<long> _gamesFinished;
    private readonly Counter<long> _movesMade;
    private readonly Counter<long> _matchmakingAttempts;
    private readonly Counter<long> _matchmakingSuccessful;
    private readonly UpDownCounter<int> _onlineUsers;
    private readonly Histogram<double> _gameDuration;

    public OtelMetricsService()
    {
        _meter = new Meter("Chess.Platform", "1.0");
        _gamesCreated = _meter.CreateCounter<long>("chess.games.created");
        _gamesFinished = _meter.CreateCounter<long>("chess.games.finished");
        _movesMade = _meter.CreateCounter<long>("chess.moves.made");
        _matchmakingAttempts = _meter.CreateCounter<long>("chess.matchmaking.attempts");
        _matchmakingSuccessful = _meter.CreateCounter<long>("chess.matchmaking.successful");
        _onlineUsers = _meter.CreateUpDownCounter<int>("chess.users.online");
        _gameDuration = _meter.CreateHistogram<double>("chess.game.duration", unit: "s");
    }

    public void IncrementCounter(string name, Dictionary<string, string>? tags = null)
    {
        var tagList = tags?.Select(t => new KeyValuePair<string, object?>(t.Key, t.Value)).ToList();
        switch (name)
        {
            case "games.created": _gamesCreated.Add(1, tagList?.ToArray() ?? []); break;
            case "games.finished": _gamesFinished.Add(1, tagList?.ToArray() ?? []); break;
            case "moves.made": _movesMade.Add(1, tagList?.ToArray() ?? []); break;
            case "matchmaking.attempts": _matchmakingAttempts.Add(1, tagList?.ToArray() ?? []); break;
            case "matchmaking.successful": _matchmakingSuccessful.Add(1, tagList?.ToArray() ?? []); break;
            case "users.online": _onlineUsers.Add(1, tagList?.ToArray() ?? []); break;
            case "users.offline": _onlineUsers.Add(-1, tagList?.ToArray() ?? []); break;
        }
    }

    public void RecordGauge(string name, double value, Dictionary<string, string>? tags = null)
    {
    }

    public void RecordDuration(string name, TimeSpan duration, Dictionary<string, string>? tags = null)
    {
        if (name == "game.duration")
            _gameDuration.Record(duration.TotalSeconds);
    }

    public void RecordGameCreated() => _gamesCreated.Add(1);
    public void RecordGameFinished(string result) => _gamesFinished.Add(1, new KeyValuePair<string, object?>("result", result));
    public void RecordMove() => _movesMade.Add(1);
    public void RecordMatchmakingAttempt() => _matchmakingAttempts.Add(1);
    public void RecordMatchmakingSuccess() => _matchmakingSuccessful.Add(1);
    public void UserConnected() => _onlineUsers.Add(1);
    public void UserDisconnected() => _onlineUsers.Add(-1);
    public void RecordGameDuration(double seconds) => _gameDuration.Record(seconds);
}
