namespace Chess.Application.Services;

public interface IMetricsService
{
    void IncrementCounter(string name, Dictionary<string, string>? tags = null);
    void RecordGauge(string name, double value, Dictionary<string, string>? tags = null);
    void RecordDuration(string name, TimeSpan duration, Dictionary<string, string>? tags = null);
}
