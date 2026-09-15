using System.Globalization;
using System.Text;
using LankaLens.DataBuilder.Joining;
using LankaLens.DataBuilder.Models;

namespace LankaLens.DataBuilder.Generation;

/// <summary>
/// Writes the DCS GNDList rows verbatim with attributed coordinates appended.
/// </summary>
/// <remarks>
/// The original workbook is hash-pinned provenance and is never modified. This writer emits a
/// derived file so downstream projects get "GNDList plus coordinates" without touching the source.
/// Unmatched rows keep every original column and leave the coordinate columns empty — never zero,
/// and never a placeholder.
/// </remarks>
internal static class GeoEnrichedCsvWriter
{
    /// <summary>Decimal places for latitude/longitude (~0.11 m at the equator).</summary>
    private const int CoordinateDecimals = 6;

    private const int AreaDecimals = 4;

    private static readonly string[] Header =
    [
        // Original GNDList columns, in workbook order.
        "Serial Number",
        "GND_UID",
        "Province_Code",
        "Province_Name",
        "District_Code",
        "District_Name",
        "DSD_Code",
        "DSD_Name",
        "GND_Code",
        "GND_NUM",
        "GND_Name",
        "LG_Code",
        "LG_Name",
        // Appended geometry columns.
        "latitude",
        "longitude",
        "centroid_latitude",
        "centroid_longitude",
        "min_latitude",
        "min_longitude",
        "max_latitude",
        "max_longitude",
        "area_sq_km",
        "geo_admin_code",
        "geo_match",
        "geo_name_agrees"
    ];

    public static void Write(
        IReadOnlyList<RawAdministrativeRecord> rawRecords,
        GeoJoinResult join,
        string outputPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

        var byCode = join.Entries.ToDictionary(e => e.DcsCode, StringComparer.Ordinal);
        var builder = new StringBuilder();

        AppendRow(builder, Header);

        foreach (var record in rawRecords)
        {
            var code = record.GnUid ?? string.Empty;
            byCode.TryGetValue(code, out var entry);

            var measures = entry?.Measures;
            var matched = measures is not null;

            AppendRow(builder,
            [
                record.SerialNumber,
                record.GnUid,
                record.ProvinceCode,
                record.ProvinceEnglish,
                record.DistrictCode,
                record.DistrictEnglish,
                record.DsCode,
                record.DsEnglish,
                record.GnCode,
                record.GnNumber,
                record.GnEnglish,
                record.LgCode,
                record.LgName,
                matched ? Coordinate(measures!.RepresentativePoint.Latitude) : null,
                matched ? Coordinate(measures!.RepresentativePoint.Longitude) : null,
                matched ? Coordinate(measures!.Centroid.Latitude) : null,
                matched ? Coordinate(measures!.Centroid.Longitude) : null,
                matched ? Coordinate(measures!.BoundingBox.MinLatitude) : null,
                matched ? Coordinate(measures!.BoundingBox.MinLongitude) : null,
                matched ? Coordinate(measures!.BoundingBox.MaxLatitude) : null,
                matched ? Coordinate(measures!.BoundingBox.MaxLongitude) : null,
                matched ? measures!.AreaSqKm.ToString("F" + AreaDecimals, CultureInfo.InvariantCulture) : null,
                entry?.GeoAdminCode,
                entry?.MatchKind ?? GeoMatchKind.Unmatched,
                matched ? (entry!.EnglishNameAgrees ? "true" : "false") : null
            ]);
        }

        // Pin LF and emit a UTF-8 BOM so Excel opens Sinhala/Tamil-adjacent text correctly.
        File.WriteAllText(outputPath, builder.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
    }

    private static string Coordinate(double value) =>
        Math.Round(value, CoordinateDecimals, MidpointRounding.AwayFromZero)
            .ToString("F" + CoordinateDecimals, CultureInfo.InvariantCulture);

    private static void AppendRow(StringBuilder builder, IReadOnlyList<string?> values)
    {
        for (var i = 0; i < values.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(',');
            }

            builder.Append(Escape(values[i]));
        }

        builder.Append('\n');
    }

    private static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var needsQuotes = value.Contains(',', StringComparison.Ordinal)
            || value.Contains('"', StringComparison.Ordinal)
            || value.Contains('\n', StringComparison.Ordinal)
            || value.Contains('\r', StringComparison.Ordinal);

        if (!needsQuotes)
        {
            return value;
        }

        return '"' + value.Replace("\"", "\"\"", StringComparison.Ordinal) + '"';
    }
}
