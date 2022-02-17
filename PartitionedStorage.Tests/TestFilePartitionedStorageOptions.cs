using Staticsoft.PartitionedStorage.Files;
using System.IO;

namespace Staticsoft.PartitionedStorage.Tests
{
    public class TestFilePartitionedStorageOptions : FilePartitionedStorageOptions
    {
        readonly string StoragePath;

        public TestFilePartitionedStorageOptions()
            => StoragePath = Directory.CreateDirectory("PartitionedStorage").FullName;

        public string PartitionedStoragePath
            => StoragePath;
    }
}