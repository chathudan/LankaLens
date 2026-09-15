using System.Text.Json.Serialization;

namespace LankaLens.DataBuilder.Acquisition;

internal sealed class NsdiSnapshotManifest
{
    [JsonPropertyName("retrievedDate")]
    public string RetrievedDate { get; set; } = string.Empty;

    [JsonPropertyName("sourceDate")]
    public string? SourceDate { get; set; }

    [JsonPropertyName("acquisitionMechanism")]
    public string AcquisitionMechanism { get; set; } = string.Empty;

    [JsonPropertyName("queryEndpoint")]
    public string QueryEndpoint { get; set; } = string.Empty;

    /// <summary>Generalization tolerance in degrees passed as <c>maxAllowableOffset</c>.</summary>
    [JsonPropertyName("maxAllowableOffset")]
    public double MaxAllowableOffset { get; set; }

    [JsonPropertyName("serviceFeatureCount")]
    public int ServiceFeatureCount { get; set; }

    [JsonPropertyName("downloadedFeatureCount")]
    public int DownloadedFeatureCount { get; set; }

    [JsonPropertyName("combinedSha256")]
    public string CombinedSha256 { get; set; } = string.Empty;

    [JsonPropertyName("combinedByteLength")]
    public long CombinedByteLength { get; set; }

    [JsonPropertyName("pages")]
    public List<NsdiPageEntry> Pages { get; set; } = [];
}

internal sealed class NsdiPageEntry
{
    [JsonPropertyName("fileName")]
    public string FileName { get; set; } = string.Empty;

    [JsonPropertyName("resultOffset")]
    public int ResultOffset { get; set; }

    [JsonPropertyName("featureCount")]
    public int FeatureCount { get; set; }

    [JsonPropertyName("sha256")]
    public string Sha256 { get; set; } = string.Empty;

    [JsonPropertyName("byteLength")]
    public long ByteLength { get; set; }

    [JsonPropertyName("retrievedUtc")]
    public string RetrievedUtc { get; set; } = string.Empty;
}
