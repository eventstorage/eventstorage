using EventStorage.Events;

namespace EventStorage.Benchmarks.Events;


public record OrderConfirmed() : SourcedEvent;