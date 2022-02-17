using Staticsoft.PartitionedStorage.Abstractions;
using Staticsoft.Testing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Staticsoft.PartitionedStorage.Tests
{
    public abstract class PartitionedStorageTests<TSPF> : TestBase<Partitions, TSPF>, IAsyncLifetime
        where TSPF : ServiceProviderFactory, new()
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
        public async Task CanNotGetItemFromNonExistingPartition()
        {
            await Assert.ThrowsAsync<PartitionedStorageItemNotFoundException>(() => NonExistingPartition.Get(NonExistingItem));
        }

        [Fact]
        public async Task CanNotGetNonExistingItem()
        {
            await Assert.ThrowsAsync<PartitionedStorageItemNotFoundException>(() => Partition.Get(NonExistingItem));
        }

        [Fact]
        public async Task CanNotGetItemFromDifferentPartition()
        {
            await AlternativePartition.Save(ItemName, Item);
            await Assert.ThrowsAsync<PartitionedStorageItemNotFoundException>(() => Partition.Get(ItemName));
        }

        [Fact]
        public async Task CanNotGetItemUsingDifferentItemId()
        {
            await Partition.Save(AlternativeItemName, Item);
            await Assert.ThrowsAsync<PartitionedStorageItemNotFoundException>(() => Partition.Get(ItemName));
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
        public async Task CanNotCreateItemTwice()
        {
            await Partition.Save(ItemName, Item);
            await Assert.ThrowsAsync<PartitionedStorageItemAlreadyExistsException>(() => Partition.Save(ItemName, Item));
        }

        [Fact]
        public async Task CanNotUpdateItemUsingInvalidVersion()
        {
            var version = await Partition.Save(ItemName, Item);
            await Assert.ThrowsAsync<PartitionedStorageItemVersionMismatchException>(() => Task.WhenAll(
                Partition.Save(ItemName, EmptyItem, version),
                Partition.Save(ItemName, EmptyItem, version))
            );
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
        public async Task CanFilterOutItemsByName()
        {
            var options = new ScanOptions[]
            {
                new() { FromItem = "J", ToItem = "Z" },
                new() { FromItem = "J" },
                new() { FromItem = "A", ToItem = "I" },
                new() { ToItem = "I" }
            };
            await Partition.Save(ItemName, Item);
            foreach (var option in options)
            {
                var items = await Partition.Scan(option);
                Assert.Empty(items);
            }
        }

        [Fact]
        public async Task KeepsItemsWhichSatisfyFilters()
        {
            var options = new ScanOptions[]
            {
                new() { FromItem = "I", ToItem = "Z" },
                new() { FromItem = "I" },
                new() { ToItem = "Z" }
            };
            await Partition.Save(ItemName, Item);
            foreach (var option in options)
            {
                var items = await Partition.Scan(option);
                Assert.Single(items);
            }
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
}
