using System;

namespace Staticsoft.PartitionedStorage.Tests;

public record TestItem
{
    public string StringProperty { get; init; }

    public long LongProperty { get; init; }

    public int IntProperty { get; init; }

    public Guid GuidProperty { get; init; }

    public double DoubleProperty { get; init; }

    public DateTime DateTimeProperty { get; init; }

    public bool BoolProperty { get; init; }

    public TestItem CustomTypeProperty { get; init; }
}
