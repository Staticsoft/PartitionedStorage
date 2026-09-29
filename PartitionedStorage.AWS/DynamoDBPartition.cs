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
		if (options.MaxItems <= 0 || IsEmptyRange(options)) return [];

		var filter = GetScanConditions(PartitionName, options);
		var items = await Client.FromQueryAsync<DynamoDBItem>(new QueryOperationConfig
		{
			Limit = GetScanLimit(options),
			Filter = filter,
			BackwardSearch = options.Order == ScanOrder.Descending,
			ConsistentRead = true
		}).GetNextSetAsync();
		return items.Select(ToItem).ToArray();
	}

	public async Task<Item<TData>> Get(string id)
	{
		var item = await Client.LoadAsync<DynamoDBItem>(PartitionName, id, new LoadConfig { ConsistentRead = true });
		if (item == null)
		{
			throw new PartitionedStorageItemNotFoundException(id, PartitionName);
		}
		return ToItem(item);
	}

	public async Task<string> Save(Item<TData> item)
	{
		var version = $"{Guid.NewGuid()}";
		var document = Client.ToDocument(ToDynamoDBItem(item, version));
		var condition = item.HasVersion() ? HasVersion(item.Version) : DoesNotExist();
		try
		{
			await Client.GetTargetTable<DynamoDBItem>().PutItemAsync(document, new PutItemOperationConfig { ConditionalExpression = condition });
			return version;
		}
		catch (ConditionalCheckFailedException)
		{
			if (item.HasVersion()) throw new PartitionedStorageItemVersionMismatchException(item.Id, item.Version);

			throw new PartitionedStorageItemAlreadyExistsException(item.Id, PartitionName);
		}
	}

	public Task Remove(string id)
		=> Client.DeleteAsync<DynamoDBItem>(PartitionName, id, new DynamoDBOperationConfig { SkipVersionCheck = true });

	static Expression DoesNotExist() => new()
	{
		ExpressionStatement = "attribute_not_exists(#sortKey)",
		ExpressionAttributeNames = new() { ["#sortKey"] = nameof(DynamoDBItem.SortKey) }
	};

	static Expression HasVersion(string version) => long.TryParse(version, out _)
		? new()
		{
			ExpressionStatement = "#version = :version OR #version = :numericVersion",
			ExpressionAttributeNames = new() { ["#version"] = nameof(DynamoDBItem.Version) },
			ExpressionAttributeValues = new() { [":version"] = version, [":numericVersion"] = new Primitive(version, saveAsNumeric: true) }
		}
		: new()
		{
			ExpressionStatement = "#version = :version",
			ExpressionAttributeNames = new() { ["#version"] = nameof(DynamoDBItem.Version) },
			ExpressionAttributeValues = new() { [":version"] = version }
		};

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
		_ when HasBothBounds(options)
			=> CreateSortKeyScanConditions(ScanOperator.Between, options.FromItem, options.ToItem),
		_ when !string.IsNullOrEmpty(options.FromItem)
			=> CreateSortKeyScanConditions(ScanOperator.GreaterThanOrEqual, options.FromItem),
		_ when !string.IsNullOrEmpty(options.ToItem)
			=> CreateSortKeyScanConditions(ScanOperator.LessThanOrEqual, options.ToItem),
		_ => Array.Empty<(string, ScanOperator, DynamoDBEntry[])>()
	};

	static (string, ScanOperator, DynamoDBEntry[])[] CreateSortKeyScanConditions(ScanOperator scanOperator, params DynamoDBEntry[] values)
		=> new[] { (nameof(DynamoDBItem.SortKey), scanOperator, values) };

	static bool IsEmptyRange(ScanOptions options)
		=> HasBothBounds(options) && string.CompareOrdinal(options.FromItem, options.ToItem) > 0;

	static bool HasBothBounds(ScanOptions options)
		=> !string.IsNullOrEmpty(options.FromItem) && !string.IsNullOrEmpty(options.ToItem);

	static int GetScanLimit(ScanOptions options)
		=> Math.Min(options.MaxItems, MaxScanItems);

	Item<TData> ToItem(DynamoDBItem item)
		=> new Item<TData>
		{
			Id = item.SortKey,
			Data = Serializer.Deserialize<TData>(item.Data),
			Version = item.Version
		};

	DynamoDBItem ToDynamoDBItem(Item<TData> item, string version) => new DynamoDBItem
	{
		Data = Serializer.Serialize(item.Data),
		PartitionKey = PartitionName,
		SortKey = item.Id,
		Version = version
	};
}
