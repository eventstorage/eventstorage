namespace EventStorage.Projections;

public enum CheckpointType
{
    Projection = 0,
    Kafka = 1,
    RabbitMQ = 2
}
public record Checkpoint(
    string Subscription,
    long Seq,
    long MaxSeq,
    CheckpointType Type = CheckpointType.Projection);