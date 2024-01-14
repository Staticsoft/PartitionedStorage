using Staticsoft.PartitionedStorage.Abstractions;
using Staticsoft.PartitionedStorage.Filters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Staticsoft.PartitionedStorage.Files;

public class FilePartition<TData> : Partition<TData>
    where TData : new()
{
    const int _4KB = 4 * 1024;

    readonly ItemSerializer Serializer;
    readonly DirectoryInfo Folder;
    readonly Encoding Encoding = Encoding.UTF8;

    string DirectoryPath
        => Folder.FullName;

    public FilePartition(ItemSerializer serializer, string path)
    {
        Serializer = serializer;
        Folder = Directory.CreateDirectory(path);
    }

    public Task<Item<TData>[]> Scan(ScanOptions options)
        => Task.WhenAll(
            Folder
                .GetFiles()
                .Sort(options.Order, file => file.Name)
                .ApplyFilters(file => file.Name, options)
                .Select(file => Get(file.Name))
                .ToArray()
        );

    public Task<Item<TData>> Get(string fileName)
        => Try.Return(() => GetItem(fileName))
            .On<FileNotFoundException>(_ => new PartitionedStorageItemNotFoundException(fileName, Folder.Name))
            .Result();

    public Task<string> Save(Item<TData> item)
    {
        var data = Serializer.Serialize(item.Data);
        return item.HasVersion()
            ? Save(item.Id, data, item.Version)
            : Save(item.Id, data);
    }

    public Task Remove(string fileName)
    {
        var path = Path.Combine(DirectoryPath, fileName);
        if (File.Exists(path)) File.Delete(path);

        return Task.CompletedTask;
    }

    async Task<Item<TData>> GetItem(string fileName)
    {
        using var stream = new FileStream(GetFilePath(fileName), FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        var bytes = await ReadFile(stream);
        return new Item<TData>
        {
            Id = fileName,
            Data = Serializer.Deserialize<TData>(Encoding.GetString(bytes)),
            Version = GetVersion(bytes)
        };
    }

    Task<string> Save(string fileName, string data)
        => Try.Return(() => CreateFile(fileName, data))
                .On<IOException>(_ => new PartitionedStorageItemAlreadyExistsException(fileName, Folder.Name))
                .Result();

    Task<string> Save(string fileName, string data, string version)
        => Try.Return(() => UpdateFile(fileName, data, version))
                .On<FileNotFoundException>(_ => new PartitionedStorageItemNotFoundException(fileName, Folder.Name))
                .On<IOException>(_ => new PartitionedStorageItemVersionMismatchException(fileName, version))
                .Result();

    async Task<string> CreateFile(string fileName, string value)
    {
        using var stream = new FileStream(GetFilePath(fileName), FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None);
        return await WriteFile(stream, value);
    }

    async Task<string> UpdateFile(string fileName, string value, string previousVersion)
    {
        using var stream = new FileStream(GetFilePath(fileName), FileMode.Open, FileAccess.ReadWrite, FileShare.None);

        var version = await GetVersion(stream);
        if (version != previousVersion) throw new PartitionedStorageItemVersionMismatchException(fileName, previousVersion);

        return await WriteFile(stream, value);
    }

    async Task<string> WriteFile(FileStream stream, string value)
    {
        var bytes = Encoding.GetBytes(value);
        await WriteFile(stream, bytes);
        return GetVersion(bytes);
    }

    static async Task WriteFile(FileStream stream, byte[] bytes)
    {
        stream.Seek(0, SeekOrigin.Begin);
        foreach (var chunk in GetChunks(bytes))
        {
            await stream.WriteAsync(chunk.AsMemory(0, chunk.Length));
        }
        stream.SetLength(bytes.Length);
    }

    static IEnumerable<byte[]> GetChunks(byte[] bytes)
    {
        var position = 0;
        while (position != bytes.Length)
        {
            var nextPosition = Math.Min(position + _4KB, bytes.Length);
            yield return bytes[position..nextPosition];
            position = nextPosition;
        }
    }

    string GetFilePath(string fileName)
        => Path.Combine(DirectoryPath, fileName);

    async Task<string> GetVersion(FileStream stream)
    {
        var previousContents = await ReadFile(stream);
        return GetVersion(previousContents);
    }

    static async Task<byte[]> ReadFile(FileStream stream)
    {
        var bytes = new List<byte>();
        while (bytes.Count != stream.Length)
        {
            var buffer = new byte[Math.Min(_4KB, stream.Length - bytes.Count)];
            await stream.ReadAsync(buffer.AsMemory());
            bytes.AddRange(buffer);
        }
        return bytes.ToArray();
    }

    string GetVersion(byte[] file)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(file);
        return string.Join(string.Empty, bytes.Select(b => b.ToString("x2")));
    }
}
