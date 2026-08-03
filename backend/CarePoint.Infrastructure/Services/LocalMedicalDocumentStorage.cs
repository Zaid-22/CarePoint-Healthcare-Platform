using CarePoint.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace CarePoint.Infrastructure.Services;

public sealed class LocalMedicalDocumentStorage : IMedicalDocumentStorage
{
    private readonly string _rootPath;

    public LocalMedicalDocumentStorage(IConfiguration configuration)
    {
        var configuredPath = configuration["MedicalDocuments:StoragePath"] ??
            Path.Combine("App_Data", "medical-documents");
        _rootPath = Path.GetFullPath(Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.Combine(AppContext.BaseDirectory, configuredPath));
        Directory.CreateDirectory(_rootPath);
    }

    public async Task<string> SaveAsync(
        Stream content, string fileExtension, CancellationToken cancellationToken = default)
    {
        var extension = fileExtension.ToLowerInvariant();
        var storageKey = Path.Combine(DateTime.UtcNow.ToString("yyyy"),
            DateTime.UtcNow.ToString("MM"), $"{Guid.NewGuid():N}{extension}");
        var path = ResolvePath(storageKey);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        await using var output = new FileStream(
            path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true);
        await content.CopyToAsync(output, cancellationToken);
        return storageKey.Replace(Path.DirectorySeparatorChar, '/');
    }

    public Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var path = ResolvePath(storageKey);
        if (!File.Exists(path))
            throw new FileNotFoundException("The stored medical document could not be found.", path);
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
        var normalizedKey = storageKey.Replace('/', Path.DirectorySeparatorChar);
        var path = Path.GetFullPath(Path.Combine(_rootPath, normalizedKey));
        var rootPrefix = _rootPath.EndsWith(Path.DirectorySeparatorChar)
            ? _rootPath
            : _rootPath + Path.DirectorySeparatorChar;
        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        if (!path.StartsWith(rootPrefix, comparison))
            throw new InvalidOperationException("Invalid medical-document storage key.");
        return path;
    }
}
