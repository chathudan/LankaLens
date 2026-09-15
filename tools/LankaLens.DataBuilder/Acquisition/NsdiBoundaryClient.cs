using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using LankaLens.DataBuilder.Sources;

namespace LankaLens.DataBuilder.Acquisition;

/// <summary>
/// One-shot acquisition of the official NSDI Boundaries Grama Niladhari polygon layer.
/// Caches paginated GeoJSON under data/source/nsdi-boundaries/ and does not re-request
/// pages already on disk.
/// </summary>
internal sealed class NsdiBoundaryClient : IDisposable
{
    public const string ServiceUrl =
        "https://gisapps.nsdi.gov.lk/server/rest/services/Srilanka/Boundaries/MapServer/1";

    public const string UserAgent =
        "LankaLens.DataBuilder/4.0 (official-source snapshot; NSDI GN boundaries)";

    public const string AcquisitionMechanism =
        "Official ArcGIS REST query (GET /query?f=geojson) paginated by resultOffset at maxRecordCount 1000";

    /// <summary>
    /// Geometry generalization in degrees (~5.5 m). Far finer than the positional accuracy of the
    /// underlying satellite-traced boundaries, while keeping the national download near 19 MB.
    /// </summary>
    public const double DefaultMaxAllowableOffset = 0.00005;

    private const int PageSize = 1000;

    private const string OutFields =
        "admin_code,gnd_name,gnd_name_census,gnd_no_census," +
        "province_census_code,district_census_code,ds_division_census_code,year_created";

    private static readonly JsonSerializerOptions ManifestJson = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly HttpClient _http;
    private readonly TimeSpan _pageDelay;
    private readonly TextWriter _log;

    public NsdiBoundaryClient(HttpClient http, TimeSpan pageDelay, TextWriter log)
    {
        _http = http;
        _pageDelay = pageDelay;
        _log = log;
    }

    public static NsdiBoundaryClient Create(TimeSpan pageDelay, TextWriter log)
    {
        var handler = new SocketsHttpHandler
        {
            AutomaticDecompression = DecompressionMethods.All,
            PooledConnectionLifetime = TimeSpan.FromMinutes(5)
        };
        var http = new HttpClient(handler) { Timeout = TimeSpan.FromMinutes(5) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
        return new NsdiBoundaryClient(http, pageDelay, log);
    }

    public static string CacheDirectory(string sourceDirectory) =>
        Path.Combine(sourceDirectory, "nsdi-boundaries", "pages");

    public static string ManifestPath(string sourceDirectory) =>
        Path.Combine(sourceDirectory, "nsdi-boundaries", "manifest.json");

    public async Task<NsdiSnapshotManifest> AcquireNationalSnapshotAsync(
        string sourceDirectory,
        bool forceRefresh,
        double maxAllowableOffset,
        CancellationToken cancellationToken)
    {
        var cacheDir = CacheDirectory(sourceDirectory);
        Directory.CreateDirectory(cacheDir);

        _log.WriteLine("NSDI GN boundary acquisition");
        _log.WriteLine($"  Mechanism: {AcquisitionMechanism}");
        _log.WriteLine($"  Generalization: maxAllowableOffset={maxAllowableOffset.ToString(CultureInfo.InvariantCulture)} degrees");
        _log.WriteLine($"  Cache: {cacheDir}");

        var total = await FetchFeatureCountAsync(cancellationToken).ConfigureAwait(false);
        _log.WriteLine($"  Service feature count: {total}");

        var pages = new List<NsdiPageEntry>();
        var downloaded = 0;
        var firstFetch = true;

        for (var offset = 0; offset < total; offset += PageSize)
        {
            var fileName = $"gn-{offset:D6}.geojson";
            var path = Path.Combine(cacheDir, fileName);
            string payload;
            DateTimeOffset retrievedUtc;

            if (!forceRefresh && File.Exists(path))
            {
                payload = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
                retrievedUtc = File.GetLastWriteTimeUtc(path);
                _log.WriteLine($"    Cache hit {fileName}");
            }
            else
            {
                if (!firstFetch)
                {
                    await Task.Delay(_pageDelay, cancellationToken).ConfigureAwait(false);
                }

                firstFetch = false;
                _log.WriteLine($"    Fetching offset={offset}");
                payload = await FetchPageAsync(offset, maxAllowableOffset, cancellationToken)
                    .ConfigureAwait(false);
                await File.WriteAllTextAsync(path, payload, new UTF8Encoding(false), cancellationToken)
                    .ConfigureAwait(false);
                retrievedUtc = DateTimeOffset.UtcNow;
                _log.WriteLine($"    Saved {fileName} ({payload.Length:N0} bytes)");
            }

            var featureCount = CountFeatures(payload);
            downloaded += featureCount;

            pages.Add(new NsdiPageEntry
            {
                FileName = "pages/" + fileName,
                ResultOffset = offset,
                FeatureCount = featureCount,
                Sha256 = SourceCatalogLoader.ComputeSha256(path),
                ByteLength = new FileInfo(path).Length,
                RetrievedUtc = retrievedUtc.ToString("yyyy-MM-ddTHH:mm:ssZ")
            });
        }

        pages = pages.OrderBy(p => p.FileName, StringComparer.Ordinal).ToList();
        var combined = ComputeCombinedHash(pages);

        var manifest = new NsdiSnapshotManifest
        {
            RetrievedDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            SourceDate = null,
            AcquisitionMechanism = AcquisitionMechanism,
            QueryEndpoint = ServiceUrl + "/query",
            MaxAllowableOffset = maxAllowableOffset,
            ServiceFeatureCount = total,
            DownloadedFeatureCount = downloaded,
            CombinedSha256 = combined.Hash,
            CombinedByteLength = combined.Bytes,
            Pages = pages
        };

        var manifestPath = ManifestPath(sourceDirectory);
        var json = JsonSerializer.Serialize(manifest, ManifestJson) + Environment.NewLine;
        await File.WriteAllTextAsync(manifestPath, json, new UTF8Encoding(false), cancellationToken)
            .ConfigureAwait(false);

        _log.WriteLine($"  Wrote manifest: {manifestPath}");
        _log.WriteLine($"  Features downloaded: {downloaded}/{total}");
        _log.WriteLine($"  Combined SHA-256: {manifest.CombinedSha256}");

        if (downloaded != total)
        {
            throw new InvalidOperationException(
                $"NSDI pagination incomplete: downloaded {downloaded} of {total} features. Re-run with --force.");
        }

        return manifest;
    }

    public static NsdiSnapshotManifest LoadManifest(string sourceDirectory)
    {
        var path = ManifestPath(sourceDirectory);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("NSDI snapshot manifest was not found.", path);
        }

        return JsonSerializer.Deserialize<NsdiSnapshotManifest>(File.ReadAllText(path), ManifestJson)
            ?? throw new InvalidOperationException("NSDI snapshot manifest is empty.");
    }

    /// <summary>
    /// Loads the cached snapshot only when every page is present and hash-clean, mirroring the
    /// MOHA verification gate so a build never silently consumes a mutated cache.
    /// </summary>
    public static bool TryLoadVerifiedSnapshot(
        string sourceDirectory,
        SourceEntry? entry,
        out NsdiSnapshotManifest? manifest,
        out string? error)
    {
        manifest = null;
        error = null;

        var path = ManifestPath(sourceDirectory);
        if (!File.Exists(path))
        {
            error = "NSDI snapshot manifest was not found. Run acquire-geo.";
            return false;
        }

        manifest = LoadManifest(sourceDirectory);
        var root = Path.Combine(sourceDirectory, "nsdi-boundaries");

        foreach (var page in manifest.Pages)
        {
            var pagePath = Path.Combine(root, page.FileName.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(pagePath))
            {
                error = $"NSDI page '{page.FileName}' is missing from {root}.";
                return false;
            }

            var actual = SourceCatalogLoader.ComputeSha256(pagePath);
            if (!string.Equals(actual, page.Sha256, StringComparison.OrdinalIgnoreCase))
            {
                error = $"SHA-256 mismatch for NSDI page '{page.FileName}'.";
                return false;
            }
        }

        var combined = ComputeCombinedHash(manifest.Pages);
        if (!string.Equals(combined.Hash, manifest.CombinedSha256, StringComparison.OrdinalIgnoreCase))
        {
            error = "NSDI combined SHA-256 does not match the manifest.";
            return false;
        }

        if (entry is not null
            && !string.IsNullOrWhiteSpace(entry.Sha256)
            && !string.Equals(combined.Hash, entry.Sha256, StringComparison.OrdinalIgnoreCase))
        {
            error = $"SHA-256 mismatch for NSDI snapshot. Expected {entry.Sha256}, got {combined.Hash}.";
            return false;
        }

        return true;
    }

    private async Task<int> FetchFeatureCountAsync(CancellationToken cancellationToken)
    {
        var url = $"{ServiceUrl}/query?where=1%3D1&returnCountOnly=true&f=json";
        var json = await GetWithRetryAsync(url, cancellationToken).ConfigureAwait(false);
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("count", out var count))
        {
            throw new InvalidOperationException("NSDI count query did not return a 'count' property.");
        }

        return count.GetInt32();
    }

    private async Task<string> FetchPageAsync(
        int offset,
        double maxAllowableOffset,
        CancellationToken cancellationToken)
    {
        var url = $"{ServiceUrl}/query"
            + "?where=1%3D1"
            + $"&outFields={Uri.EscapeDataString(OutFields)}"
            + "&returnGeometry=true"
            + "&outSR=4326"
            + $"&maxAllowableOffset={maxAllowableOffset.ToString(CultureInfo.InvariantCulture)}"
            + $"&resultOffset={offset}"
            + $"&resultRecordCount={PageSize}"
            + "&f=geojson";

        var payload = await GetWithRetryAsync(url, cancellationToken).ConfigureAwait(false);

        // An ArcGIS error surfaces as HTTP 200 with an {"error":{...}} body.
        if (payload.Contains("\"error\"", StringComparison.Ordinal)
            && !payload.Contains("\"features\"", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"NSDI query failed at offset {offset}: {payload}");
        }

        return payload;
    }

    private async Task<string> GetWithRetryAsync(string url, CancellationToken cancellationToken)
    {
        Exception? last = null;
        for (var attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                using var response = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);
                if ((int)response.StatusCode >= 500)
                {
                    last = new HttpRequestException($"NSDI returned {(int)response.StatusCode}.");
                    await Task.Delay(TimeSpan.FromSeconds(attempt * 2), cancellationToken).ConfigureAwait(false);
                    continue;
                }

                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                last = ex;
                await Task.Delay(TimeSpan.FromSeconds(attempt * 2), cancellationToken).ConfigureAwait(false);
            }
        }

        throw new InvalidOperationException("NSDI request failed after retries.", last);
    }

    private static int CountFeatures(string geoJson)
    {
        using var doc = JsonDocument.Parse(geoJson);
        return doc.RootElement.TryGetProperty("features", out var features)
            && features.ValueKind == JsonValueKind.Array
            ? features.GetArrayLength()
            : 0;
    }

    internal static (string Hash, long Bytes) ComputeCombinedHash(IReadOnlyList<NsdiPageEntry> pages)
    {
        var ordered = pages.OrderBy(p => p.FileName, StringComparer.Ordinal).ToList();
        var payload = string.Join('\n', ordered.Select(p => $"{p.FileName}:{p.Sha256}:{p.ByteLength}"));
        var bytes = ordered.Sum(p => p.ByteLength);
        var hash = Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
        return (hash, bytes);
    }

    public void Dispose() => _http.Dispose();
}
