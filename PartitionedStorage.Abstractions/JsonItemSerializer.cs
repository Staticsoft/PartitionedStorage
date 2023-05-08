using System.Text.Json;

namespace Staticsoft.PartitionedStorage.Abstractions;

public class JsonItemSerializer : ItemSerializer
{
    public string Serialize<T>(T item)
        => JsonSerializer.Serialize(item);

    public T Deserialize<T>(string serialized)
        => JsonSerializer.Deserialize<T>(serialized);
}
