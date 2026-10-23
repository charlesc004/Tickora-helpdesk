using Tickora.Application;

namespace Tickora.Infrastructure.Storage;

public sealed class LocalFileStorage(string root) : IFileStorage
{
    private string PathFor(string key)
    {
        if (!Guid.TryParseExact(key, "N", out _)) throw new ArgumentException("Invalid storage identifier.");
        return Path.Combine(root, key);
    }
    public async Task<string> WriteAsync(Stream stream, CancellationToken ct)
    {
        Directory.CreateDirectory(root);
        var key = Guid.NewGuid().ToString("N");
        await using var file = new FileStream(PathFor(key), FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true);
        await stream.CopyToAsync(file, ct);
        return key;
    }
    public Task<Stream> ReadAsync(string key, CancellationToken ct) => Task.FromResult<Stream>(new FileStream(PathFor(key), FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true));
    public Task DeleteAsync(string key, CancellationToken ct) { File.Delete(PathFor(key)); return Task.CompletedTask; }
}
