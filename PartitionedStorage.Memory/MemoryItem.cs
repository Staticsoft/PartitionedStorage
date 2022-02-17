using System;

namespace Staticsoft.PartitionedStorage.Memory
{
    public class MemoryItem
    {
        public string Id { get; init; }

        public string Data { get; init; }

        public string Version { get; init; }

        public override bool Equals(object obj) => obj switch
        {
            MemoryItem item => GetHashCode() == item.GetHashCode(),
            _ => false
        };

        public override int GetHashCode()
            => HashCode.Combine(Version, Id);
    }
}
