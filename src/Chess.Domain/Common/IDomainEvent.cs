namespace Chess.Domain.Common;

public interface IDomainEvent
{
    DateTime OccurredAt { get; }
}
