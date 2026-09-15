using System.Globalization;
using System.Text.Json;
using LankaLens.DataBuilder.Geometry;
using LankaLens.DataBuilder.Models;
using LankaLens.DataBuilder.Normalization;

namespace LankaLens.DataBuilder.Parsing;

/// <summary>
/// Parses cached NSDI Boundaries GN GeoJSON pages into <see cref="RawGnGeometryRecord"/> values.
/// </summary>
internal sealed class NsdiGeoJsonParser
{
    /// <summary>
    /// NSDI uses <c>admin_code</c> 0 as a placeholder on unattributed polygons;
    /// those carry no identity and are dropped rather than joined.
    /// </summary>
    private const int PlaceholderAdminCode = 0;

    public IReadOnlyList<RawGnGeometryRecord> ParseDirectory(string directory)
    {
        if (!Directory.Exists(directory))
        {
            throw new DirectoryNotFoundException($"NSDI geometry cache not found: {directory}");
        }

        var records = new List<RawGnGeometryRecord>();
        var files = Directory.GetFiles(directory, "*.geojson")
            .OrderBy(f => f, StringComparer.Ordinal);

        foreach (var file in files)
        {
            records.AddRange(Parse(File.ReadAllText(file)));
        }

        return records;
    }

    public IReadOnlyList<RawGnGeometryRecord> Parse(string geoJson)
    {
        using var document = JsonDocument.Parse(geoJson);
        var root = document.RootElement;

        if (!root.TryGetProperty("features", out var features)
            || features.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException(
                "NSDI response is not a GeoJSON FeatureCollection (no 'features' array).");
        }

        var records = new List<RawGnGeometryRecord>(features.GetArrayLength());
        foreach (var feature in features.EnumerateArray())
        {
            var record = ParseFeature(feature);
            if (record is not null)
            {
                records.Add(record);
            }
        }

        return records;
    }

    private static RawGnGeometryRecord? ParseFeature(JsonElement feature)
    {
        if (!feature.TryGetProperty("properties", out var props))
        {
            return null;
        }

        var adminCode = ReadAdminCode(props);
        var polygons = feature.TryGetProperty("geometry", out var geometry)
            ? ReadPolygons(geometry)
            : [];

        return new RawGnGeometryRecord
        {
            AdminCode = adminCode,
            CensusEnglishName = ReadString(props, "gnd_name_census"),
            GnName = ReadString(props, "gnd_name"),
            CensusGnNumber = ReadString(props, "gnd_no_census"),
            DsCensusCode = ReadString(props, "ds_division_census_code"),
            DistrictCensusCode = ReadString(props, "district_census_code"),
            ProvinceCensusCode = ReadString(props, "province_census_code"),
            YearCreated = ReadString(props, "year_created"),
            Measures = polygons.Count > 0 ? PolygonGeometry.Measure(polygons) : null
        };
    }

    private static string? ReadAdminCode(JsonElement props)
    {
        if (!props.TryGetProperty("admin_code", out var element))
        {
            return null;
        }

        var raw = element.ValueKind switch
        {
            JsonValueKind.Number when element.TryGetInt64(out var number) => number,
            JsonValueKind.String when long.TryParse(
                element.GetString(),
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var parsed) => parsed,
            _ => (long?)null
        };

        if (raw is null or PlaceholderAdminCode || raw < 0)
        {
            return null;
        }

        var text = raw.Value.ToString(CultureInfo.InvariantCulture);

        // Only the full seven-digit GND_UID layout is joinable; shorter values are
        // truncated district/DS stubs and carry no GN identity.
        return text.Length == 7 ? text : null;
    }

    private static string? ReadString(JsonElement props, string name) =>
        props.TryGetProperty(name, out var element) && element.ValueKind == JsonValueKind.String
            ? TextNormalizer.NormalizeOptionalText(element.GetString())
            : null;

    private static IReadOnlyList<GeoPolygon> ReadPolygons(JsonElement geometry)
    {
        if (geometry.ValueKind != JsonValueKind.Object
            || !geometry.TryGetProperty("type", out var typeElement)
            || !geometry.TryGetProperty("coordinates", out var coordinates))
        {
            return [];
        }

        return typeElement.GetString() switch
        {
            "Polygon" => ReadPolygon(coordinates) is { } single ? [single] : [],
            "MultiPolygon" => coordinates.EnumerateArray()
                .Select(ReadPolygon)
                .Where(p => p is not null)
                .Select(p => p!)
                .ToList(),
            _ => []
        };
    }

    private static GeoPolygon? ReadPolygon(JsonElement ringArray)
    {
        if (ringArray.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var rings = ringArray.EnumerateArray().Select(ReadRing).Where(r => r is not null).ToList();
        if (rings.Count == 0)
        {
            return null;
        }

        return new GeoPolygon(rings[0]!, rings.Skip(1).Select(r => r!).ToList());
    }

    private static GeoRing? ReadRing(JsonElement pointArray)
    {
        if (pointArray.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var vertices = new List<GeoVertex>(pointArray.GetArrayLength());
        foreach (var point in pointArray.EnumerateArray())
        {
            if (point.ValueKind != JsonValueKind.Array || point.GetArrayLength() < 2)
            {
                continue;
            }

            // GeoJSON coordinate order is [longitude, latitude].
            var lon = point[0].GetDouble();
            var lat = point[1].GetDouble();
            vertices.Add(new GeoVertex(lon, lat));
        }

        return vertices.Count >= 3 ? new GeoRing(vertices) : null;
    }
}
