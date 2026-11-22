using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using SevaSetu.Application.Common;

namespace SevaSetu.Infrastructure.Storage;

public class PhotoStore(IConfiguration config, IWebHostEnvironment env) : IPhotoStore
{
    public async Task<string> SaveAsync(Stream content, string contentType, CancellationToken ct)
    {
        var key = $"{Guid.NewGuid():N}{Extension(contentType)}";
        var conn = config["Blob:ConnectionString"];
        var containerName = config["Blob:Container"] ?? "sevasetu-photos";
        if (!string.IsNullOrWhiteSpace(conn))
        {
            var container = new BlobContainerClient(conn, containerName);
            await container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: ct);
            var blob = container.GetBlobClient(key);
            await blob.UploadAsync(content, new BlobHttpHeaders { ContentType = contentType }, cancellationToken: ct);
            return key;
        }
        var dir = Path.Combine(env.ContentRootPath, "App_Data", "photos");
        Directory.CreateDirectory(dir);
        await using var file = File.Create(Path.Combine(dir, key));
        await content.CopyToAsync(file, ct);
        return key;
    }

    public async Task<(Stream Content, string ContentType)?> OpenAsync(string key, CancellationToken ct)
    {
        if (key.Contains("..") || key.Contains('/') || key.Contains('\\'))
            return null;
        var conn = config["Blob:ConnectionString"];
        var containerName = config["Blob:Container"] ?? "sevasetu-photos";
        if (!string.IsNullOrWhiteSpace(conn))
        {
            var blob = new BlobContainerClient(conn, containerName).GetBlobClient(key);
            if (!await blob.ExistsAsync(ct)) return null;
            var props = await blob.GetPropertiesAsync(cancellationToken: ct);
            var ms = new MemoryStream();
            await blob.DownloadToAsync(ms, ct);
            ms.Position = 0;
            return (ms, props.Value.ContentType ?? "application/octet-stream");
        }
        var path = Path.Combine(env.ContentRootPath, "App_Data", "photos", key);
        if (!File.Exists(path)) return null;
        Stream stream = File.OpenRead(path);
        return (stream, GuessType(key));
    }

    private static string Extension(string contentType) => contentType switch
    {
        "image/png" => ".png",
        "image/webp" => ".webp",
        _ => ".jpg"
    };

    private static string GuessType(string key) => Path.GetExtension(key).ToLowerInvariant() switch
    {
        ".png" => "image/png",
        ".webp" => "image/webp",
        _ => "image/jpeg"
    };
}
