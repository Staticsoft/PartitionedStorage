# PartitionedStorage - Desired State

## Overview
A flexible storage abstraction that provides partitioned data storage capabilities with support for optimistic concurrency control through versioning. The library enables storing typed data in isolated partitions with multiple backend implementations including in-memory, file system, and DynamoDB.

## Interfaces

### Partitions
Factory interface for accessing typed storage partitions.

**Methods**:
```csharp
public interface Partitions
{
    /// <summary>
    /// Gets a partition for storing items of the specified type.
    /// </summary>
    /// <typeparam name="TData">The type of data to store in the partition.</typeparam>
    /// <param name="partitionName">The name of the partition.</param>
    /// <returns>A partition instance for the specified type and name.</returns>
    Partition<TData> Get<TData>(string partitionName)
        where TData : new();
}
```

### Partition<TData>
Represents a single partition that can store items of type `TData` with versioning support.

**Methods**:
```csharp
public interface Partition<TData>
    where TData : new()
{
    /// <summary>
    /// Retrieves an item from the partition by its identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the item.</param>
    /// <returns>The item with the specified identifier.</returns>
    /// <exception cref="PartitionedStorageItemNotFoundException">
    /// Thrown when the item does not exist in the partition.
    /// </exception>
    Task<Item<TData>> Get(string id);
    
    /// <summary>
    /// Saves an item to the partition. Creates a new item if it doesn't exist,
    /// or updates an existing item if a version is provided.
    /// </summary>
    /// <param name="item">The item to save.</param>
    /// <returns>The version identifier of the saved item.</returns>
    /// <exception cref="PartitionedStorageItemAlreadyExistsException">
    /// Thrown when attempting to create an item that already exists.
    /// </exception>
    /// <exception cref="PartitionedStorageItemVersionMismatchException">
    /// Thrown when the provided version doesn't match the current version.
    /// </exception>
    Task<string> Save(Item<TData> item);
    
    /// <summary>
    /// Scans the partition and returns items matching the specified options.
    /// </summary>
    /// <param name="options">Options for filtering and ordering the scan results.</param>
    /// <returns>An array of items matching the scan criteria.</returns>
    Task<Item<TData>[]> Scan(ScanOptions options);
    
    /// <summary>
    /// Removes an item from the partition.
    /// </summary>
    /// <param name="id">The unique identifier of the item to remove.</param>
    Task Remove(string id);
}
```

**Exceptions**:
- `PartitionedStorageItemNotFoundException` - Thrown when an item or partition does not exist
- `PartitionedStorageItemAlreadyExistsException` - Thrown when attempting to create a duplicate item
- `PartitionedStorageItemVersionMismatchException` - Thrown when a version conflict occurs during update

## Data Types

### Item<TData>
Represents an item stored in a partition with versioning support.

```csharp
public record Item<TData>
{
    public string Id { get; init; }
    public string Version { get; init; }
    public TData Data { get; init; }
}
```

### ScanOptions
Configuration options for scanning and filtering items in a partition.

```csharp
public class ScanOptions
{
    public int MaxItems { get; init; } = int.MaxValue;
    public string FromItem { get; init; } = string.Empty;
    public string ToItem { get; init; } = string.Empty;
    public ScanOrder Order { get; init; } = ScanOrder.Ascending;
}
```

### ScanOrder
Enumeration of possible scan ordering directions.

```csharp
public enum ScanOrder
{
    Ascending,
    Descending
}
```

## Providers

### Memory Implementation
**Package**: `PartitionedStorage.Memory`

**Purpose**: In-memory implementation for testing and development

**Characteristics**:
- Stores data in memory using concurrent dictionaries
- No persistence between application restarts
- Supports all operations including versioning and concurrency control
- Ideal for unit testing and development

**Options**: None required

**Environment Variables**: None required

**Registration**:
```csharp
services
    .AddSingleton<Partitions, MemoryPartitions>()
    .AddSingleton<ItemSerializer, JsonItemSerializer>();
```

### File Implementation
**Package**: `PartitionedStorage.Files`

**Purpose**: File system-based persistent storage

**Characteristics**:
- Persists data to the file system as JSON files
- Each partition stored in a separate directory
- Supports all operations with file-based versioning
- Suitable for local development and small-scale deployments

**Options**:
```csharp
public class FilePartitionedStorageOptions
{
    public required string DataDirectory { get; init; }
}
```

**Environment Variables**: None required (configured via options)

**Registration**:
```csharp
services
    .AddSingleton<Partitions, FilePartitions>()
    .AddSingleton<ItemSerializer, JsonItemSerializer>()
    .AddSingleton(new FilePartitionedStorageOptions
    {
        DataDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Data")
    });
```

### DynamoDB Implementation
**Package**: `PartitionedStorage.AWS`

**Purpose**: Amazon DynamoDB cloud storage backend

**Characteristics**:
- Uses AWS DynamoDB for scalable cloud storage
- Requires AWS credentials and region configuration
- Supports all operations with DynamoDB's native versioning
- Configurable table name prefix for multi-tenant scenarios
- Production-ready with automatic scaling

**Options**:
```csharp
public class DynamoDBPartitionedStorageOptions
{
    public required string AccessKeyId { get; init; }
    public required string SecretAccessKey { get; init; }
    public required string Region { get; init; }
    public string TableNamePrefix { get; init; } = string.Empty;
}
```

**Environment Variables**:
- `PartitionedStorageAccessKeyId` - AWS access key ID for authentication
- `PartitionedStorageSecretAccessKey` - AWS secret access key for authentication
- `PartitionedStorageRegion` - AWS region name (e.g., us-east-1)

**Registration**:
```csharp
services
    .AddSingleton<Partitions, DynamoDBPartitions>()
    .AddSingleton<ItemSerializer, JsonItemSerializer>()
    .AddSingleton(new DynamoDBPartitionedStorageOptions
    {
        AccessKeyId = EnvVariable("PartitionedStorageAccessKeyId"),
        SecretAccessKey = EnvVariable("PartitionedStorageSecretAccessKey"),
        Region = EnvVariable("PartitionedStorageRegion"),
        TableNamePrefix = "MyApp"
    });

static string EnvVariable(string key)
    => Environment.GetEnvironmentVariable(key)
    ?? throw new ArgumentNullException($"Environment variable '{key}' is not set");
```

## Test Scenarios

Test scenarios are ordered by increasing complexity, following the test ordering strategy.

### Level 1: Exception Tests (No State Changes)

#### Scenario: Get item from non-existing partition throws NotFoundException
**Given** the system is empty  
**When** I try to get an item from a non-existing partition  
**Then** a `PartitionedStorageItemNotFoundException` is thrown

#### Scenario: Get non-existing item throws NotFoundException
**Given** a partition exists  
**When** I try to get an item with ID "non-existing-id"  
**Then** a `PartitionedStorageItemNotFoundException` is thrown

### Level 2: Read-Only Operations on Empty System

#### Scenario: Scan returns empty array when partition is empty
**Given** a partition exists with no items  
**When** I scan the partition  
**Then** an empty array is returned

### Level 3: Single Create + Verify

#### Scenario: Create item and verify it exists
**Given** a partition exists  
**When** I save an item with ID "Item" and test data  
**Then** the item is created with a version identifier  
**And** I can retrieve the item by its ID  
**And** the item has the correct ID, data, and version

#### Scenario: Scan returns single item after creation
**Given** a partition exists  
**When** I save an item  
**Then** scanning the partition returns exactly one item  
**And** the item matches the created item

#### Scenario: Cannot get item from different partition
**Given** I have saved an item in partition "Alternative"  
**When** I try to get the item from the default partition  
**Then** a `PartitionedStorageItemNotFoundException` is thrown

#### Scenario: Cannot get item using different item ID
**Given** I have saved an item with ID "AlternativeItem"  
**When** I try to get an item with ID "Item"  
**Then** a `PartitionedStorageItemNotFoundException` is thrown

### Level 4: Create + Delete Cycle

#### Scenario: Delete non-existing item succeeds silently
**Given** a partition exists  
**When** I remove an item with ID "Item"  
**Then** no exception is thrown

#### Scenario: Delete created item
**Given** I have saved an item  
**When** I remove the item  
**Then** the item no longer exists  
**And** attempting to get it throws `PartitionedStorageItemNotFoundException`

### Level 5: Multiple Items

#### Scenario: Create and scan multiple items
**Given** a partition exists  
**When** I save three items with IDs "Item0", "Item1", "Item2"  
**Then** scanning the partition returns exactly three items  
**And** all three items are present with correct IDs and data

#### Scenario: Scan with MaxItems limit
**Given** I have saved four items  
**When** I scan with MaxItems set to 3  
**Then** exactly three items are returned  
**And** the items are the first three in order

#### Scenario: Filter items by ID range using FromItem and ToItem
**Given** I have saved an item with ID "Item"  
**When** I scan with FromItem "J" and ToItem "Z"  
**Then** no items are returned  
**When** I scan with FromItem "I" and ToItem "Z"  
**Then** the item is returned

#### Scenario: Scan items in ascending order
**Given** I have saved items "AlternativeItem" and "Item"  
**When** I scan with MaxItems 1 and Order Ascending  
**Then** "AlternativeItem" is returned first

#### Scenario: Scan items in descending order
**Given** I have saved items "AlternativeItem" and "Item"  
**When** I scan with MaxItems 1 and Order Descending  
**Then** "Item" is returned first

### Level 6: Update Operations

#### Scenario: Create item twice throws AlreadyExistsException
**Given** I have saved an item with ID "Item"  
**When** I try to save another item with the same ID without a version  
**Then** a `PartitionedStorageItemAlreadyExistsException` is thrown

#### Scenario: Update item with correct version
**Given** I have saved an item  
**When** I retrieve the item and update it with its current version  
**Then** the update succeeds  
**And** retrieving the item returns the updated data

### Level 7: Advanced Scenarios

#### Scenario: Update with invalid version throws VersionMismatchException
**Given** I have saved an item  
**When** I try to update the item twice concurrently with the same version  
**Then** a `PartitionedStorageItemVersionMismatchException` is thrown for one of the updates

#### Scenario: Handle concurrent updates with retry logic
**Given** I have saved an item  
**When** 100 concurrent updates are attempted with retry logic  
**Then** all updates eventually succeed  
**And** the final state reflects one of the updates  
**And** no data corruption occurs
