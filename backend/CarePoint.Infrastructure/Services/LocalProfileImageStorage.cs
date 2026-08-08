using CarePoint.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace CarePoint.Infrastructure.Services;

public sealed class LocalProfileImageStorage : IProfileImageStorage
{
    private readonly string _rootPath;

    public LocalProfileImageStorage(IConfiguration configuration)
    {
        var configuredPath = configuration["ProfileImages:StoragePath"] ??
            Path.Combine("App_Data", "profile-images");
        _rootPath = Path.GetFullPath(Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.Combine(AppContext.BaseDirectory, configuredPath));
        Directory.CreateDirectory(_rootPath);
    }

    public async Task<string> SaveAsync(
        Stream content, string fileExtension, CancellationToken cancellationToken = default)
    {
        var extension = fileExtension.ToLowerInvariant();
        if (extension is not (".jpg" or ".jpeg" or ".png"))
            throw new InvalidOperationException("Unsupported profile-image extension.");

        var storageKey = $"{Guid.NewGuid():N}{extension}";
        var path = ResolvePath(storageKey);
        await using var output = new FileStream(
            path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true);
        await content.CopyToAsync(output, cancellationToken);
        return storageKey;
    }

    public Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var path = ResolvePath(storageKey);
        if (!File.Exists(path))
            throw new FileNotFoundException("The stored profile image could not be found.", path);
        Stream stream = new FileStream(
            path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var path = ResolvePath(storageKey);
        if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }

    private string ResolvePath(string storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey) || Path.GetFileName(storageKey) != storageKey)
            throw new InvalidOperationException("Invalid profile-image storage key.");

        var path = Path.GetFullPath(Path.Combine(_rootPath, storageKey));
        var rootPrefix = _rootPath.EndsWith(Path.DirectorySeparatorChar)
            ? _rootPath
            : _rootPath + Path.DirectorySeparatorChar;
        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        if (!path.StartsWith(rootPrefix, comparison))
            throw new InvalidOperationException("Invalid profile-image storage key.");
        return path;
    }
}
