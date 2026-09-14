using System.Net;
using Google;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Caching.Distributed;
using GcsObject = Google.Apis.Storage.v1.Data.Object;

namespace ProbHammer.Web.Services;

/// <summary><see cref="IDistributedCache"/> backed by GCS objects under "sessions/{key}" in one
/// shared state bucket (see terraform/cloud-run-iap.tf's google_storage_bucket.state) - replaces
/// AddDistributedMemoryCache so ASP.NET Core Session survives Cloud Run scale-to-zero. Deliberately
/// doesn't enforce <see cref="DistributedCacheEntryOptions"/>' expiration itself (a session should
/// just keep working across a multi-hour gap between turns, the whole reason this exists) - Set and
/// Refresh instead bump the object's CustomTime, and the bucket's own lifecycle rule garbage-collects
/// anything untouched for 14 days. That's cost cleanup, not session expiry.</summary>
public sealed class GoogleCloudStorageDistributedCache(StorageClient client, string bucketName) : IDistributedCache
{
    private const string Prefix = "sessions/";

    public byte[]? Get(string key) => GetAsync(key).GetAwaiter().GetResult();

    public async Task<byte[]?> GetAsync(string key, CancellationToken token = default)
    {
        using var stream = new MemoryStream();
        try
        {
            await client.DownloadObjectAsync(bucketName, ObjectName(key), stream, cancellationToken: token);
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        return stream.ToArray();
    }

    public void Set(string key, byte[] value, DistributedCacheEntryOptions options) =>
        SetAsync(key, value, options).GetAwaiter().GetResult();

    public async Task SetAsync(
        string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
    {
        using var stream = new MemoryStream(value);
        var obj = new GcsObject
        {
            Bucket = bucketName,
            Name = ObjectName(key),
            ContentType = "application/octet-stream",
            CustomTimeDateTimeOffset = DateTimeOffset.UtcNow,
        };
        await client.UploadObjectAsync(obj, stream, cancellationToken: token);
    }

    public void Refresh(string key) => RefreshAsync(key).GetAwaiter().GetResult();

    public async Task RefreshAsync(string key, CancellationToken token = default)
    {
        var obj = new GcsObject
        {
            Bucket = bucketName,
            Name = ObjectName(key),
            CustomTimeDateTimeOffset = DateTimeOffset.UtcNow,
        };
        try
        {
            await client.PatchObjectAsync(obj, cancellationToken: token);
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
        {
            // Nothing to refresh - the entry was never set, or has already been removed.
        }
    }

    public void Remove(string key) => RemoveAsync(key).GetAwaiter().GetResult();

    public async Task RemoveAsync(string key, CancellationToken token = default)
    {
        try
        {
            await client.DeleteObjectAsync(bucketName, ObjectName(key), cancellationToken: token);
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
        {
        }
    }

    private static string ObjectName(string key) => Prefix + key;
}
