using System;

namespace Staticsoft.PartitionedStorage.Abstractions;

public class PartitionedStorageException : Exception
{
    public PartitionedStorageException(string message)
        : base(message) { }

    public PartitionedStorageException(string message, Exception innerException)
        : base(message, innerException) { }
}

public class PartitionedStorageItemNotFoundException : PartitionedStorageException
{
    public PartitionedStorageItemNotFoundException(string itemId, string partitionName)
        : base(GetMessage(itemId, partitionName)) { }

    static string GetMessage(string itemId, string partitionName)
        => $"Unable to find item {itemId} in partition {partitionName}.";
}

public class PartitionedStorageItemAlreadyExistsException : PartitionedStorageException
{
    public PartitionedStorageItemAlreadyExistsException(string itemId, string partitionName)
        : base(GetMessage(itemId, partitionName)) { }

    static string GetMessage(string itemId, string partitionName)
        => $"Item {itemId} already exists in partition {partitionName}.";
}

public class PartitionedStorageItemVersionMismatchException : PartitionedStorageException
{
    public PartitionedStorageItemVersionMismatchException(string itemId, string version)
        : base(GetMessage(itemId, version)) { }

    static string GetMessage(string itemId, string version)
        => $"Unable to update item {itemId}: there is no stored version {version}.";
}
