using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Staticsoft.PartitionedStorage.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Staticsoft.PartitionedStorage.AWS;

public class DynamoDBPartition<TData> : Partition<TData>
    where TData : new()
{
    readonly ItemSerializer Serializer;
    readonly DynamoDBContext Client;
    readonly string PartitionName;

    const int MaxScanItems = 1000;

    public DynamoDBPartition(ItemSerializer serializer, DynamoDBContext client, string partitionName)
    {
        Serializer = serializer;
        Client = client;
        PartitionName = partitionName;
    }

    public async Task<Item<TData>[]> Scan(ScanOptions options)
    {
        var filter = GetScanConditions(PartitionName, options);
        var items = await Client.FromQueryAsync<DynamoDBItem>(new QueryOperationConfig
        {
            Limit = GetScanLimit(options),
            Filter = filter,
            BackwardSearch = options.Order == ScanOrder.Descending
        }).GetNextSetAsync();
        return items.Select(ToItem).ToArray();
    }

    public async Task<Item<TData>> Get(string id)
    {
        var item = await Client.LoadAsync<DynamoDBItem>(PartitionName, id);
        if (item == null)
        {
            throw new PartitionedStorageItemNotFoundException(id, PartitionName);
        }
        return ToItem(item);
    }

    public async Task<string> Save(Item<TData> item)
    {
        var dynamoDBItem = ToDynamoDBItem(item);
        try
        {
            await Client.SaveAsync(dynamoDBItem);
            return $"{dynamoDBItem.Version}";
        }
        catch (ConditionalCheckFailedException)
        {
            if (item.HasVersion()) throw new PartitionedStorageItemVersionMismatchException(item.Id, item.Version);

            throw new PartitionedStorageItemAlreadyExistsException(item.Id, PartitionName);
        }
    }

    public Task Remove(string id)
        => Client.DeleteAsync<DynamoDBItem>(PartitionName, id, new DynamoDBOperationConfig { SkipVersionCheck = true });

    static QueryFilter GetScanConditions(string partitionName, ScanOptions options)
    {
        var conditions = GetPartitionKeyFilter(partitionName);
        var additionalConditions = GetAdditionalConditions(options);
        foreach (var condition in additionalConditions)
        {
            var (attributeName, scanOperator, values) = condition;
            conditions.AddCondition(attributeName, scanOperator, values);
        }
        return conditions;
    }

    static QueryFilter GetPartitionKeyFilter(string partitionName)
    {
        var filter = new QueryFilter();
        filter.AddCondition(nameof(DynamoDBItem.PartitionKey), new Condition
        {
            ComparisonOperator = ComparisonOperator.EQ,
            AttributeValueList = new List<AttributeValue> { new(partitionName) }
        });
        return filter;
    }

    static (string, ScanOperator, DynamoDBEntry[])[] GetAdditionalConditions(ScanOptions options) => options switch
    {
        _ when !string.IsNullOrEmpty(options.FromItem) && !string.IsNullOrEmpty(options.ToItem)
            => CreateSortKeyScanConditions(ScanOperator.Between, options.FromItem, options.ToItem),
        _ when !string.IsNullOrEmpty(options.FromItem)
            => CreateSortKeyScanConditions(ScanOperator.GreaterThanOrEqual, options.FromItem),
        _ when !string.IsNullOrEmpty(options.ToItem)
            => CreateSortKeyScanConditions(ScanOperator.LessThan, options.ToItem),
        _ => Array.Empty<(string, ScanOperator, DynamoDBEntry[])>()
    };

    static (string, ScanOperator, DynamoDBEntry[])[] CreateSortKeyScanConditions(ScanOperator scanOperator, params DynamoDBEntry[] values)
        => new[] { (nameof(DynamoDBItem.SortKey), scanOperator, values) };

    static int GetScanLimit(ScanOptions options)
        => Math.Min(options.MaxItems, MaxScanItems);

    Item<TData> ToItem(DynamoDBItem item)
        => new Item<TData>
        {
            Id = item.SortKey,
            Data = Serializer.Deserialize<TData>(item.Data),
            Version = $"{item.Version.Value}"
        };

    DynamoDBItem ToDynamoDBItem(Item<TData> item) => new DynamoDBItem
    {
        Data = Serializer.Serialize(item.Data),
        PartitionKey = PartitionName,
        SortKey = item.Id,
        Version = item.HasVersion() ? Convert.ToInt32(item.Version) : null
    };
}
