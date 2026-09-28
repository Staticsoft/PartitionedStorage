using Staticsoft.PartitionedStorage.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Staticsoft.PartitionedStorage.Tests.ModelBased;

/// <summary>
/// Wraps a <see cref="Partitions"/> implementation so that it can be driven by model-based tests.
/// Every operation records a normalized outcome (result or exception type) in <see cref="Log"/>,
/// so that two harnesses can be compared regardless of how each implementation represents versions.
/// </summary>
public class StorageHarness(Partitions partitions, IReadOnlyCollection<string> partitionNames)
{
	public const string BogusVersion = "999999";

	static readonly JsonSerializerOptions DataFormat = new() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault };

	readonly Partitions Partitions = partitions;
	readonly IReadOnlyCollection<string> PartitionNames = partitionNames;
	readonly Dictionary<string, List<string>> ObservedVersions = [];

	public List<string> Log { get; } = [];

	public Task Create(string partition, string id, TestItem data)
		=> Record($"Create({partition},{id})", async () =>
		{
			var version = await Get(partition).Save(id, data);
			Versions(partition, id).Add(version);
			return "Ok";
		});

	/// <param name="versionsBack">
	/// Which previously observed version to send: 0 is the latest version returned by Save for this item,
	/// 1 is the one before it, and so on. When there is no such version, <see cref="BogusVersion"/> is sent.
	/// </param>
	public Task Update(string partition, string id, TestItem data, int versionsBack)
		=> Record($"Update({partition},{id},{versionsBack})", async () =>
		{
			var versions = Versions(partition, id);
			var version = versionsBack < versions.Count ? versions[^(versionsBack + 1)] : BogusVersion;
			var saved = await Get(partition).Save(id, data, version);
			versions.Add(saved);
			return "Ok";
		});

	public Task GetItem(string partition, string id)
		=> Record($"Get({partition},{id})", async () => Describe(partition, await Get(partition).Get(id)));

	public Task Remove(string partition, string id)
		=> Record($"Remove({partition},{id})", async () =>
		{
			await Get(partition).Remove(id);
			return "Ok";
		});

	public Task Scan(string partition, ScanOptions options)
		=> Record($"Scan({partition},{Describe(options)})", async () => Describe(partition, await Get(partition).Scan(options)));

	public async Task<string> Snapshot()
	{
		var partitions = await Task.WhenAll(PartitionNames.Order().Select(async name =>
			$"{name}: {Describe(name, await Get(name).Scan())}"
		));
		return string.Join(Environment.NewLine, partitions);
	}

	public override string ToString()
		=> string.Join(Environment.NewLine, Log.Append("-- contents --").Append(Snapshot().GetAwaiter().GetResult()));

	public static bool Equal(StorageHarness actual, StorageHarness model)
		=> actual.Log.SequenceEqual(model.Log)
		&& actual.Snapshot().GetAwaiter().GetResult() == model.Snapshot().GetAwaiter().GetResult();

	async Task Record(string operation, Func<Task<string>> execute)
	{
		string outcome;
		try
		{
			outcome = await execute();
		}
		catch (Exception exception)
		{
			outcome = exception.GetType().Name;
		}
		Log.Add($"{operation} => {outcome}");
	}

	Partition<TestItem> Get(string partition)
		=> Partitions.Get<TestItem>(partition);

	List<string> Versions(string partition, string id)
	{
		var key = $"{partition}/{id}";
		if (!ObservedVersions.TryGetValue(key, out var versions))
		{
			versions = [];
			ObservedVersions[key] = versions;
		}
		return versions;
	}

	string Describe(string partition, Item<TestItem>[] items)
		=> $"[{string.Join(", ", items.Select(item => Describe(partition, item)))}]";

	// Versions are opaque and implementation-specific, so they are described by their position
	// in the history of versions returned by Save for this item (v0 = latest, v? = never returned by Save).
	string Describe(string partition, Item<TestItem> item)
	{
		var versions = Versions(partition, item.Id);
		var index = versions.LastIndexOf(item.Version);
		var version = index < 0 ? "v?" : $"v{versions.Count - 1 - index}";
		return $"{item.Id}={JsonSerializer.Serialize(item.Data, DataFormat)}@{version}";
	}

	static string Describe(ScanOptions options)
		=> $"max={(options.MaxItems == int.MaxValue ? "all" : options.MaxItems)},from='{options.FromItem}',to='{options.ToItem}',{options.Order}";
}
