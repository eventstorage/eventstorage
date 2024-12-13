namespace EventStorage.Projections;

public enum CheckpointType
{
    Projection = 0,
    Kafka = 1,
    RabbitMQ = 2
}
public record Checkpoint(long MaxSeq, long Seq, CheckpointType Type);