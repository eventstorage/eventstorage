using EventStorage.Events;

namespace EventStorage.Benchmarks.Projections;

public record OrderConfirmed() : SourcedEvent;