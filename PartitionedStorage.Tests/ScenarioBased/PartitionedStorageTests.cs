using Staticsoft.PartitionedStorage.Abstractions;
using Staticsoft.Testing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Staticsoft.PartitionedStorage.Tests.ScenarioBased;

public abstract class PartitionedStorageTests : TestBase<Partitions>, IAsyncLifetime
{
	const string NonExistingPartitionName = "NonExistingPartition";
	const string NonExistingItem = "NonExistingItem";
	const string AlternativePartitionName = "AlternativePartition";
	const string AlternativeItemName = "AlternativeItem";
	const string ItemName = "Item";

	readonly TestItem Item = new()
	{
		BoolProperty = true,
		DateTimeProperty = DateTime.UtcNow,
		DoubleProperty = Math.PI,
		GuidProperty = Guid.NewGuid(),
		IntProperty = 42,
		LongProperty = long.MaxValue,
		StringProperty = "Magic string!",
		CustomTypeProperty = new TestItem
		{
			BoolProperty = false,
			DateTimeProperty = DateTime.MaxValue,
			DoubleProperty = Math.E,
			GuidProperty = Guid.NewGuid(),
			IntProperty = 8080,
			LongProperty = long.MinValue,
			StringProperty = "Even more magical string!",
		},
	};

	readonly TestItem EmptyItem = new();

	Partition<TestItem> NonExistingPartition
		=> SUT.Get<TestItem>(NonExistingPartitionName);

	Partition<TestItem> AlternativePartition
		=> SUT.Get<TestItem>(AlternativePartitionName);

	Partition<TestItem> Partition
		=> SUT.Get<TestItem>();

	public Task InitializeAsync()
		=> Task.WhenAll(new[] { Partition, AlternativePartition }.Select(DeleteItemsFromPartition));

	public Task DisposeAsync()
		=> Task.CompletedTask;

	[Fact]
	public async Task ThrowsNotFoundExceptionWhenGettingItemFromNonExistingPartition()
	{
		await Assert.ThrowsAsync<PartitionedStorageItemNotFoundException>(() => NonExistingPartition.Get(NonExistingItem));
	}

	[Fact]
	public async Task ThrowsNotFoundExceptionWhenGettingNonExistingItem()
	{
		await Assert.ThrowsAsync<PartitionedStorageItemNotFoundException>(() => Partition.Get(NonExistingItem));
	}

	[Fact]
	public async Task ReturnsEmptyArrayWhenScanningEmptyPartition()
	{
		var items = await Partition.Scan();
		Assert.Empty(items);
	}

	[Fact]
	public async Task CanGetItem()
	{
		var version = await Partition.Save(ItemName, Item);
		var item = await Partition.Get(ItemName);
		var expected = new Item<TestItem>
		{
			Id = ItemName,
			Data = Item,
			Version = version,
		};
		Assert.Equal(expected, item);
	}

	[Fact]
	public async Task ReturnsSingleItemWhenItemIsCreated()
	{
		await Partition.Save(ItemName, Item);
		var items = await Partition.Scan();
		Assert.Single(items);
		Assert.Equal(ItemName, items[0].Id);
	}

	[Fact]
	public async Task ThrowsNotFoundExceptionWhenGettingItemFromDifferentPartition()
	{
		await AlternativePartition.Save(ItemName, Item);
		await Assert.ThrowsAsync<PartitionedStorageItemNotFoundException>(() => Partition.Get(ItemName));
	}

	[Fact]
	public async Task ThrowsNotFoundExceptionWhenGettingItemUsingDifferentItemId()
	{
		await Partition.Save(AlternativeItemName, Item);
		await Assert.ThrowsAsync<PartitionedStorageItemNotFoundException>(() => Partition.Get(ItemName));
	}

	[Fact]
	public async Task CanDeleteNonExistingItem()
	{
		await Partition.Remove(ItemName);
	}

	[Fact]
	public async Task CanDeleteItem()
	{
		await Partition.Save(ItemName, Item);
		await Partition.Remove(ItemName);
		await Assert.ThrowsAsync<PartitionedStorageItemNotFoundException>(() => Partition.Get(ItemName));
	}

	[Fact]
	public async Task CanListAllItems()
	{
		var items = await CreateAndSaveItems(3);
		var retrieved = await Partition.Scan();
		Assert.Equal(items.Select(item => item.Data), retrieved.Select(item => item.Data));
		Assert.Equal(items.Select(item => item.Id), retrieved.Select(item => item.Id));
	}

	[Fact]
	public async Task CanListSpecificAmountOfItems()
	{
		var items = await CreateAndSaveItems(4);
		var retrieved = await Partition.Scan(new ScanOptions { MaxItems = 3 });
		Assert.Equal(items.Select(item => item.Data).Take(3), retrieved.Select(item => item.Data));
		Assert.Equal(items.Select(item => item.Id).Take(3), retrieved.Select(item => item.Id));
	}

	[Fact]
	public async Task FiltersItemsByIdRange()
	{
		await Partition.Save(ItemName, Item);

		var optionsFilterOut = new ScanOptions[]
		{
			new() { FromItem = "J", ToItem = "Z" },
			new() { FromItem = "J" },
			new() { FromItem = "A", ToItem = "I" },
			new() { ToItem = "I" }
		};
		foreach (var option in optionsFilterOut)
		{
			var items = await Partition.Scan(option);
			Assert.Empty(items);
		}

		var optionsKeep = new ScanOptions[]
		{
			new() { FromItem = "I", ToItem = "Z" },
			new() { FromItem = "I" },
			new() { ToItem = "Z" }
		};
		foreach (var option in optionsKeep)
		{
			var items = await Partition.Scan(option);
			Assert.Single(items);
		}
	}

	[Fact]
	public async Task ScansItemsInAscendingOrder()
	{
		await Partition.Save(ItemName, Item);
		await Partition.Save(AlternativeItemName, Item);

		var scanAscending = new ScanOptions() { MaxItems = 1 };
		var scannedAscending = await Partition.Scan(scanAscending);
		Assert.Equal(AlternativeItemName, scannedAscending.Single().Id);
	}

	[Fact]
	public async Task ScansItemsInDescendingOrder()
	{
		await Partition.Save(ItemName, Item);
		await Partition.Save(AlternativeItemName, Item);

		var scanDescending = new ScanOptions() { MaxItems = 1, Order = ScanOrder.Descending };
		var scannedDescending = await Partition.Scan(scanDescending);
		Assert.Equal(ItemName, scannedDescending.Single().Id);
	}

	[Fact]
	public async Task ThrowsAlreadyExistsExceptionWhenCreatingItemTwice()
	{
		await Partition.Save(ItemName, Item);
		await Assert.ThrowsAsync<PartitionedStorageItemAlreadyExistsException>(() => Partition.Save(ItemName, Item));
	}

	[Fact]
	public async Task CanUpdateItem()
	{
		var version = await Partition.Save(ItemName, Item);
		var updated = Item with { StringProperty = "Updated!" };
		await Partition.Save(ItemName, updated, version);
		var item = await Partition.Get(ItemName);
		Assert.Equal(updated, item.Data);
	}

	[Fact]
	public async Task UpdateReturnsNewVersionEvenIfDataIsTheSame()
	{
		var version = await Partition.Save(ItemName, Item);
		var newVersion = await Partition.Save(ItemName, Item, version);
		Assert.NotEqual(version, newVersion);
	}

	[Fact]
	public async Task ThrowsVersionMismatchExceptionWhenUpdatingWithInvalidVersion()
	{
		var version = await Partition.Save(ItemName, Item);
		await Assert.ThrowsAsync<PartitionedStorageItemVersionMismatchException>(() => Task.WhenAll(
			Partition.Save(ItemName, EmptyItem, version),
			Partition.Save(ItemName, EmptyItem, version))
		);
	}

	[Fact]
	public async Task ThrowsVersionMismatchExceptionWhenUpdatingNonExistentItem()
	{
		await Assert.ThrowsAsync<PartitionedStorageItemVersionMismatchException>(() => Partition.Save(ItemName, Item, "0"));
	}

	[Fact]
	public async Task HandlesConcurrentUpdates()
	{
		await Partition.Save(ItemName, Item);

		var tasks = Enumerable.Range(0, 100).Select(async i =>
		{
			var saved = false;
			while (!saved)
			{
				try
				{
					var item = await Partition.Get(ItemName);
					var data = item.Data with
					{
						IntProperty = i
					};
					await Partition.Save(item.Id, data, item.Version);
					saved = true;
				}
				catch (PartitionedStorageItemVersionMismatchException)
				{
					saved = false;
				}
			}
		});

		await Task.WhenAll(tasks);
	}

	static async Task DeleteItemsFromPartition<T>(Partition<T> partition)
		where T : new()
	{
		var items = await partition.Scan();
		await Task.WhenAll(items.Select(item => partition.Remove(item.Id)));
	}

	async Task<IEnumerable<Item<TestItem>>> CreateAndSaveItems(int count)
	{
		var items = Enumerable.Range(0, count).Select(i => new Item<TestItem>
		{
			Id = $"Item{i}",
			Data = Item with { StringProperty = $"Item{i}" }
		});
		await Task.WhenAll(items.Select(item => Partition.Save(item)));
		return items;
	}
}
