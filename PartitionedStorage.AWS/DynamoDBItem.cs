using Amazon.DynamoDBv2.DataModel;

namespace Staticsoft.PartitionedStorage.AWS;

[DynamoDBTable("PartitionedStorage")]
public class DynamoDBItem
{
	[DynamoDBHashKey]
	public string PartitionKey { get; init; }

	[DynamoDBRangeKey]
	public string SortKey { get; init; }

	public string Data { get; init; }

	public string Version { get; init; }
}
