using CsCheck;
using Staticsoft.PartitionedStorage.Abstractions;
using Staticsoft.PartitionedStorage.Memory;
using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Staticsoft.PartitionedStorage.Tests.ModelBased;

/// <summary>
/// Model-based tests: random sequences of operations are applied both to a real implementation
/// and to <see cref="MemoryPartitions"/> (the model), and every outcome must match.
/// On failure CsCheck shrinks the sequence to a minimal reproduction and reports its seed.
/// </summary>
public abstract class PartitionedStorageModelBasedTests(ITestOutputHelper output)
{
	static readonly string[] PartitionNames = ["P1", "P2"];

	// Small pools make collisions (same item created twice, stale versions, ...) likely.
	// Ids are chosen so that ordinal and culture-aware ordering differ ("item3" vs "Item10").
	static readonly string[] ItemIds = ["A", "B", "C", "Item1", "Item10", "Item2", "item3"];
	static readonly string[] RangeBounds = ["", "", "A", "B", "Item", "Item1", "Item2", "item", "Z"];

	static readonly Gen<string> GenPartition = Gen.OneOfConst(PartitionNames);
	static readonly Gen<string> GenId = Gen.OneOfConst(ItemIds);

	// Small data domain on purpose: identical content saved twice must not be confused with the same version.
	static readonly Gen<TestItem> GenData =
		Gen.Select(Gen.OneOfConst("x", "y"), Gen.Int[0, 1], (text, number) => new TestItem { StringProperty = text, IntProperty = number });

	static readonly Gen<int> GenVersion = Gen.Int[0, 2];

	static readonly Gen<ScanOptions> GenScanOptions =
		Gen.Select(
			Gen.Frequency((1, Gen.Int[0, 4]), (1, Gen.Const(int.MaxValue))),
			Gen.OneOfConst(RangeBounds),
			Gen.OneOfConst(RangeBounds),
			Gen.Enum<ScanOrder>(),
			(max, from, to, order) => new ScanOptions { MaxItems = max, FromItem = from, ToItem = to, Order = order }
		);

	readonly ITestOutputHelper Output = output;

	protected abstract Partitions CreateActual();

	protected virtual long Iterations => 5000;

	static Gen<(string Partition, string Id, TestItem Data)> GenCreate()
		=> Gen.Select(GenPartition, GenId, GenData);

	static Gen<(string Partition, string Id, TestItem Data, int Version)> GenUpdate()
		=> Gen.Select(GenPartition, GenId, GenData, GenVersion);

	static Gen<(string Partition, string Id)> GenGet()
		=> Gen.Select(GenPartition, GenId);

	static Gen<(string Partition, string Id)> GenRemove()
		=> Gen.Select(GenPartition, GenId);

	static Gen<(string Partition, ScanOptions Options)> GenScan()
		=> Gen.Select(GenPartition, GenScanOptions);

	[Fact]
	public Task BehavesLikeInMemoryModel()
		=> Gen.Const(0).Select(_ => (Harness(CreateActual()), Harness(new MemoryPartitions(new JsonItemSerializer()))))
			.Select(Task.FromResult)
			.SampleModelBasedAsync(
				[
					Operation(
						GenCreate(),
						op => $"Create({op.Partition},{op.Id},{Json(op.Data)})",
						(harness, op) => harness.Create(op.Partition, op.Id, op.Data)),
					Operation(
						GenUpdate(),
						op => $"Update({op.Partition},{op.Id},{Json(op.Data)},versionsBack={op.Version})",
						(harness, op) => harness.Update(op.Partition, op.Id, op.Data, op.Version)),
					Operation(
						GenGet(),
						op => $"Get({op.Partition},{op.Id})",
						(harness, op) => harness.GetItem(op.Partition, op.Id)),
					Operation(
						GenRemove(),
						op => $"Remove({op.Partition},{op.Id})",
						(harness, op) => harness.Remove(op.Partition, op.Id)),
					Operation(
						GenScan(),
						op => $"Scan({op.Partition},max={op.Options.MaxItems},from='{op.Options.FromItem}',to='{op.Options.ToItem}',{op.Options.Order})",
						(harness, op) => harness.Scan(op.Partition, op.Options)),
				],
				equal: StorageHarness.Equal,
				iter: Iterations,
				writeLine: Output.WriteLine
			);

	static StorageHarness Harness(Partitions partitions)
		=> new(partitions, PartitionNames);

	static GenOperationAsync<StorageHarness, StorageHarness> Operation<T>(
		Gen<T> gen,
		Func<T, string> name,
		Func<StorageHarness, T, Task> execute
	)
		=> gen.Operation<StorageHarness, StorageHarness>(name, execute, execute);

	static string Json(TestItem item)
		=> $"{item.StringProperty}{item.IntProperty}";
}
