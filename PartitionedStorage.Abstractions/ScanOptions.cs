﻿namespace Staticsoft.PartitionedStorage.Abstractions;

public class ScanOptions
{
    public int MaxItems { get; init; } = int.MaxValue;
    public string FromItem { get; init; } = string.Empty;
    public string ToItem { get; init; } = string.Empty;
    public ScanOrder Order { get; init; } = ScanOrder.Ascending;
}

public enum ScanOrder
{
    Ascending,
    Descending
}