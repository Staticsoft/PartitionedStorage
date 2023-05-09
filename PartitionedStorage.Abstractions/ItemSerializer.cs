namespace Staticsoft.PartitionedStorage.Abstractions;

public interface ItemSerializer
{
    string Serialize<T>(T item);

    T Deserialize<T>(string serialized);
}
