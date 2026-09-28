using Staticsoft.PartitionedStorage.Abstractions;
using Staticsoft.PartitionedStorage.Filters;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Staticsoft.PartitionedStorage.Files;

public class FilePartition<TData>(
	ItemSerializer serializer,
	string path
) : Partition<TData>
	where TData : new()
{
	const int _4KB = 4 * 1024;

	readonly ItemSerializer Serializer = serializer;

	readonly DirectoryInfo DataFolder = Directory.CreateDirectory(Path.Combine(path, "Data"));
	readonly DirectoryInfo VersionsFolder = Directory.CreateDirectory(Path.Combine(path, "Versions"));
	readonly ConcurrentDictionary<string, SemaphoreSlim> Locks = [];
	readonly Encoding Encoding = Encoding.UTF8;

	public Task<Item<TData>[]> Scan(ScanOptions options)
		=> Task.WhenAll(
			DataFolder
				.GetFiles()
				.Sort(options.Order, file => file.Name)
				.ApplyFilters(file => file.Name, options)
				.Select(file => Get(file.Name))
				.ToArray()
		);

	public Task<Item<TData>> Get(string fileName)
		=> Try.Return(() => GetItem(fileName))
			.On<FileNotFoundException>(_ => new PartitionedStorageItemNotFoundException(fileName, DataFolder.Name))
			.Result();

	public Task<string> Save(Item<TData> item)
	{
		var data = Serializer.Serialize(item.Data);
		return item.HasVersion()
			? Save(item.Id, data, item.Version)
			: Save(item.Id, data);
	}

	public async Task Remove(string fileName)
	{
		await Lock(fileName);

		try
		{
			var dataFilePath = GetDataFilePath(fileName);
			if (File.Exists(dataFilePath)) File.Delete(dataFilePath);

			var versionFilePath = GetVersionFilePath(fileName);
			if (File.Exists(versionFilePath)) File.Delete(versionFilePath);
		}
		finally
		{
			Release(fileName);
		}
	}

	async Task<Item<TData>> GetItem(string fileName)
	{
		await Lock(fileName);

		try
		{
			using var stream = new FileStream(GetDataFilePath(fileName), FileMode.Open, FileAccess.Read, FileShare.None);
			var bytes = await ReadFile(stream);
			return new Item<TData>
			{
				Id = fileName,
				Data = Serializer.Deserialize<TData>(Encoding.GetString(bytes)),
				Version = await GetVersion(GetVersionFilePath(fileName))
			};
		}
		finally
		{
			Release(fileName);
		}
	}

	Task<string> Save(string fileName, string data)
		=> Try.Return(() => CreateFile(fileName, data))
				.On<IOException>(_ => new PartitionedStorageItemAlreadyExistsException(fileName, DataFolder.Name))
				.Result();

	Task<string> Save(string fileName, string data, string version)
		=> Try.Return(() => UpdateFile(fileName, data, version))
				.On<FileNotFoundException>(_ => new PartitionedStorageItemVersionMismatchException(fileName, version))
				.On<IOException>(_ => new PartitionedStorageItemVersionMismatchException(fileName, version))
				.Result();

	async Task<string> CreateFile(string fileName, string value)
	{
		await Lock(fileName);

		try
		{
			using var stream = new FileStream(GetDataFilePath(fileName), FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None);
			return await WriteFile(stream, value, GetVersionFilePath(fileName));
		}
		finally
		{
			Release(fileName);
		}
	}

	async Task<string> UpdateFile(string fileName, string value, string previousVersion)
	{
		await Lock(fileName);

		try
		{
			var versionFilePath = GetVersionFilePath(fileName);
			var version = await GetVersion(versionFilePath);

			using var stream = new FileStream(GetDataFilePath(fileName), FileMode.Open, FileAccess.ReadWrite, FileShare.None);

			if (version != previousVersion) throw new PartitionedStorageItemVersionMismatchException(fileName, previousVersion);

			return await WriteFile(stream, value, versionFilePath);
		}
		finally
		{
			Release(fileName);
		}
	}

	async Task<string> WriteFile(FileStream stream, string value, string versionFilePath)
	{
		var bytes = Encoding.GetBytes(value);
		await WriteFile(stream, bytes);

		var newVersion = $"{Guid.NewGuid()}";
		await SaveVersion(versionFilePath, newVersion);
		return newVersion;
	}

	static async Task WriteFile(FileStream stream, byte[] bytes)
	{
		stream.Seek(0, SeekOrigin.Begin);

		await stream.WriteAsync(bytes);

		stream.SetLength(bytes.Length);
	}

	string GetDataFilePath(string fileName)
		=> Path.Combine(DataFolder.FullName, fileName);

	string GetVersionFilePath(string fileName)
		=> Path.Combine(VersionsFolder.FullName, fileName);

	static Task<string> GetVersion(string fileName)
		=> File.ReadAllTextAsync($"{fileName}.version");

	static Task SaveVersion(string fileName, string newVersion)
		=> File.WriteAllTextAsync($"{fileName}.version", newVersion);

	static async Task<byte[]> ReadFile(FileStream stream)
	{
		var bytes = new List<byte>();
		while (bytes.Count != stream.Length)
		{
			var buffer = new byte[Math.Min(_4KB, stream.Length - bytes.Count)];
			await stream.ReadExactlyAsync(buffer.AsMemory());
			bytes.AddRange(buffer);
		}
		return bytes.ToArray();
	}

	Task Lock(string fileName)
		=> FileLock(fileName).WaitAsync();

	void Release(string fileName)
		=> FileLock(fileName).Release();

	SemaphoreSlim FileLock(string fileName)
		=> Locks.GetOrAdd(fileName, (_) => new SemaphoreSlim(initialCount: 1, maxCount: 1));
}
