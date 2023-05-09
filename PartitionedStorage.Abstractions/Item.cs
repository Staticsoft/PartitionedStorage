namespace Staticsoft.PartitionedStorage.Abstractions;

public record Item<TData>
{
    public string Id { get; init; }
    public string Version { get; init; }
    public TData Data { get; init; }
}
