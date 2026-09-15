using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using LankaLens.DataBuilder.Acquisition;
using LankaLens.DataBuilder.Joining;
using LankaLens.DataBuilder.Models;

namespace LankaLens.DataBuilder.Reporting;

/// <summary>
/// Writes the geometry coverage summary and the machine-readable unresolved-coordinate gap list.
/// </summary>
internal static class GeoCoverageReportWriter
{
    private static readonly JsonSerializerOptions GapJson = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static void WriteMarkdown(
        GeoJoinResult join,
        NsdiSnapshotManifest manifest,
        CanonicalDataset dcs,
        string outputPath)
    {
        var s = join.Summary;
        var sb = new StringBuilder();

        sb.Append("# Grama Niladhari coordinate coverage\n\n");
        sb.Append("Derived from the NSDI Boundaries Grama Niladhari polygon layer, attributed to DCS codes.\n\n");

        sb.Append("## Source snapshot\n\n");
        sb.Append($"- Endpoint: `{manifest.QueryEndpoint}`\n");
        sb.Append($"- Retrieved: {manifest.RetrievedDate}\n");
        sb.Append($"- Generalization: `maxAllowableOffset={manifest.MaxAllowableOffset.ToString(CultureInfo.InvariantCulture)}` degrees\n");
        sb.Append($"- Combined SHA-256: `{manifest.CombinedSha256}`\n");
        sb.Append($"- Features in service: {manifest.ServiceFeatureCount:N0}\n\n");

        var coverage = s.DcsGramaNiladhariDivisions == 0
            ? 0
            : 100.0 * s.Matched / s.DcsGramaNiladhariDivisions;

        sb.Append("## Coverage\n\n");
        sb.Append("| Measure | Count |\n|---|---:|\n");
        sb.Append($"| DCS Grama Niladhari divisions | {s.DcsGramaNiladhariDivisions:N0} |\n");
        sb.Append($"| NSDI features | {s.GeoFeatures:N0} |\n");
        sb.Append($"| NSDI features with joinable code | {s.GeoFeaturesWithJoinableCode:N0} |\n");
        sb.Append($"| **Matched (total)** | **{s.Matched:N0}** |\n");
        sb.Append($"| — by direct code | {s.MatchedByCode:N0} |\n");
        sb.Append($"| — by confirmed DS recode | {s.MatchedByDsRecode:N0} |\n");
        sb.Append($"| — by confirmed GN mapping | {s.MatchedByGnMapping:N0} |\n");
        sb.Append($"| — realigned by name within DS block | {s.MatchedByNameRealignment:N0} |\n");
        sb.Append($"| Unmatched (no coordinates) | {s.Unmatched:N0} |\n");
        sb.Append($"| NSDI features left unused | {s.GeoUnused:N0} |\n");
        sb.Append($"| English-name disagreements | {s.EnglishNameDisagreements:N0} |\n");
        sb.Append($"| Centroid outside polygon | {s.CentroidOutsidePolygon:N0} |\n");
        sb.Append($"| **Coverage** | **{coverage.ToString("F2", CultureInfo.InvariantCulture)}%** |\n\n");

        sb.Append("`latitude`/`longitude` carry a representative point guaranteed to fall inside the\n");
        sb.Append("division. `centroid_latitude`/`centroid_longitude` carry the area-weighted centroid,\n");
        sb.Append($"which lies outside its own polygon for {s.CentroidOutsidePolygon:N0} division(s) with\n");
        sb.Append("concave or multi-part shapes.\n\n");

        var names = dcs.GramaNiladhariDivisions.ToDictionary(g => g.Code, g => g.Name.English, StringComparer.Ordinal);

        var unmatched = join.Entries
            .Where(e => e.MatchKind == GeoMatchKind.Unmatched)
            .OrderBy(e => e.DcsCode, StringComparer.Ordinal)
            .ToList();

        sb.Append($"## Divisions without coordinates ({unmatched.Count})\n\n");
        if (unmatched.Count == 0)
        {
            sb.Append("None — every DCS Grama Niladhari division has attributed geometry.\n\n");
        }
        else
        {
            sb.Append("| DCS code | English name |\n|---|---|\n");
            foreach (var entry in unmatched)
            {
                names.TryGetValue(entry.DcsCode, out var english);
                sb.Append($"| `{entry.DcsCode}` | {english ?? "(unknown)"} |\n");
            }

            sb.Append('\n');
        }

        if (join.Conflicts.Count > 0)
        {
            sb.Append($"## Conflicts ({join.Conflicts.Count})\n\n");
            sb.Append("| Code | Issue | Detail |\n|---|---|---|\n");
            foreach (var conflict in join.Conflicts.OrderBy(c => c.Code, StringComparer.Ordinal))
            {
                sb.Append($"| `{conflict.Code}` | {conflict.Issue} | {conflict.Detail} |\n");
            }

            sb.Append('\n');
        }

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        File.WriteAllText(outputPath, sb.ToString().Replace("\r\n", "\n"), new UTF8Encoding(false));
    }

    public static void WriteGapsJson(GeoJoinResult join, CanonicalDataset dcs, string outputPath)
    {
        var names = dcs.GramaNiladhariDivisions.ToDictionary(g => g.Code, g => g.Name.English, StringComparer.Ordinal);

        var payload = new
        {
            description =
                "DCS Grama Niladhari divisions with no attributed NSDI polygon, plus NSDI polygons "
                + "that no DCS code claimed. Resolve deliberately via data/mappings/geo-to-dcs.json.",
            unmatchedDcsCodes = join.Entries
                .Where(e => e.MatchKind == GeoMatchKind.Unmatched)
                .OrderBy(e => e.DcsCode, StringComparer.Ordinal)
                .Select(e => new
                {
                    code = e.DcsCode,
                    english = names.GetValueOrDefault(e.DcsCode)
                })
                .ToList(),
            unusedGeoAdminCodes = join.UnusedGeoAdminCodes,
            conflicts = join.Conflicts
        };

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        var json = JsonSerializer.Serialize(payload, GapJson).Replace("\r\n", "\n") + "\n";
        File.WriteAllText(outputPath, json, new UTF8Encoding(false));
    }
}
