# Partitioned Storage
A flexible storage abstraction that provides partitioned data storage capabilities with multiple backend implementations including in-memory, file system, and DynamoDB.

## Core Interfaces

### Partitions
The main entry point for accessing storage partitions.

```csharp
public interface Partitions
{
    Partition<TData> Get<TData>(string partitionName)
        where TData : new();
}
```

### Partition<TData>
Represents a single partition that can store items of type `TData`.

```csharp
public interface Partition<TData>
    where TData : new()
{
    Task<Item<TData>> Get(string id);
    Task<string> Save(Item<TData> item);
    Task<Item<TData>[]> Scan(ScanOptions options);
    Task Remove(string id);
}
```

## Features

### Data Operations
- **Get**: Retrieve a single item by ID
- **Save**: Create or update an item
- **Scan**: Query multiple items with filtering options
- **Remove**: Delete an item

### Implementations
1. **Memory Storage** (`MemoryPartitions`)
   - In-memory storage suitable for testing and temporary data
   - No persistence between application restarts

2. **File Storage** (`FilePartitions`)
   - Persists data to the file system
   - Requires configuration via `FilePartitionedStorageOptions`

3. **DynamoDB Storage** (`DynamoDBPartitions`)
   - Amazon DynamoDB backend implementation
   - Requires AWS credentials and region configuration
   - Configurable table name prefix

## Configuration

### DynamoDB Configuration
Required environment variables:
- `PartitionedStorageAccessKeyId`: AWS access key ID
- `PartitionedStorageSecretAccessKey`: AWS secret access key
- `PartitionedStorageRegion`: AWS region name

### Service Registration
```csharp
// Memory Storage
services.AddSingleton<Partitions, MemoryPartitions>()
        .AddSingleton<ItemSerializer, JsonItemSerializer>();

// File Storage
services.AddSingleton<Partitions, FilePartitions>()
        .AddSingleton<ItemSerializer, JsonItemSerializer>()
        .AddSingleton<FilePartitionedStorageOptions, CustomOptions>();

// DynamoDB Storage
services.AddSingleton<Partitions, DynamoDBPartitions>()
        .AddSingleton<ItemSerializer, JsonItemSerializer>()
        .AddSingleton(new DynamoDBPartitionedStorageOptions() 
        { 
            TableNamePrefix = "YourPrefix" 
        });
```

## Error Handling

The implementation throws several specific exceptions:

- `PartitionedStorageItemNotFoundException`: Item or partition not found
- `PartitionedStorageItemAlreadyExistsException`: Attempted to create a duplicate item
- `PartitionedStorageItemVersionMismatchException`: Concurrent modification detected

## Scanning and Filtering

The `ScanOptions` class provides various filtering capabilities:

- `MaxItems`: Limit the number of returned items
- `FromItem`: Start scanning from this item ID
- `ToItem`: Stop scanning at this item ID
- `Order`: Specify scan direction (Ascending/Descending)

Example:
```csharp
var options = new ScanOptions 
{
    MaxItems = 10,
    FromItem = "A",
    ToItem = "Z",
    Order = ScanOrder.Ascending
};
```

## Version Control
Items support optimistic concurrency control through versioning:
- Each save operation returns a version string
- Updates must include the current version
- Concurrent modifications are detected and prevented
