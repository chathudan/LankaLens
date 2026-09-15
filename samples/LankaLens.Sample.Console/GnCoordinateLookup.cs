using System.Globalization;
using System.Text;

/// <summary>
/// A Grama Niladhari division's derived location, loaded from the DataBuilder-generated
/// coordinate CSV rather than from the LankaLens.AdministrativeDivisions package itself.
/// </summary>
/// <param name="Latitude">A representative point guaranteed to fall inside the division.</param>
/// <param name="Longitude">A representative point guaranteed to fall inside the division.</param>
/// <param name="AreaSqKm">Polygon area in square kilometres.</param>
/// <param name="MatchKind">
/// How the coordinate was attributed: "code" (direct DCS/NSDI code match), "ds-recode" or
/// "gn-mapping" (confirmed recode mapping), or "name-realigned" (corrected within its DS block
/// - see data/mappings/geo-to-dcs.json).
/// </param>
internal sealed record GnLocation(double Latitude, double Longitude, double AreaSqKm, string MatchKind);

/// <summary>
/// Reads Grama Niladhari coordinates from <c>gnd-list-with-coordinates.csv</c>.
/// </summary>
/// <remarks>
/// Coordinates are intentionally <b>not</b> part of the LankaLens.AdministrativeDivisions public
/// API: they are derived from the NSDI Boundaries GN polygon layer (2017 satellite-traced,
/// redistribution terms unstated), so they are approximate and are shipped only as a DataBuilder
/// output - see docs/data-sources.md. This sample bundles that generated CSV as sample data and
/// reads it directly to show how an application can join it to a Grama Niladhari code from the
/// library. Coverage is 99.65% (13,959/14,008); a division with no attributed polygon simply has
/// no entry here.
/// </remarks>
internal static class GnCoordinateLookup
{
    private const string FileName = "gnd-list-with-coordinates.csv";

    private static readonly Lazy<IReadOnlyDictionary<string, GnLocation>> ByCode = new(Load);

    /// <summary>Looks up the derived location for a Grama Niladhari code (the DCS GND_UID).</summary>
    /// <returns>The location, or <see langword="null"/> when this division has no attributed coordinate.</returns>
    public static GnLocation? FindLocation(string gnCode) =>
        ByCode.Value.TryGetValue(gnCode, out var location) ? location : null;

    private static Dictionary<string, GnLocation> Load()
    {
        var path = Path.Combine(AppContext.BaseDirectory, FileName);
        var result = new Dictionary<string, GnLocation>(StringComparer.Ordinal);

        if (!File.Exists(path))
        {
            // Sample data ships via the csproj's linked Content item; this only fires if the
            // sample was run from a build that skipped copying it.
            return result;
        }

        using var reader = new StreamReader(path);
        reader.ReadLine(); // header

        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            var fields = SplitCsvLine(line);
            if (fields.Length < 24)
            {
                continue;
            }

            var gnUid = fields[1];
            var latitudeText = fields[13];
            var longitudeText = fields[14];

            // Unmatched divisions are written with empty coordinate columns, never zeros.
            if (gnUid.Length == 0 || latitudeText.Length == 0 || longitudeText.Length == 0)
            {
                continue;
            }

            if (!double.TryParse(latitudeText, NumberStyles.Float, CultureInfo.InvariantCulture, out var lat)
                || !double.TryParse(longitudeText, NumberStyles.Float, CultureInfo.InvariantCulture, out var lon))
            {
                continue;
            }

            double.TryParse(fields[21], NumberStyles.Float, CultureInfo.InvariantCulture, out var areaSqKm);
            var matchKind = fields[23];

            result[gnUid] = new GnLocation(lat, lon, areaSqKm, matchKind);
        }

        return result;
    }

    /// <summary>
    /// Splits one CSV line, honouring quoted fields (embedded commas and doubled quotes).
    /// Does not handle a quoted field spanning multiple physical lines - this dataset never
    /// writes embedded newlines, so a plain per-line reader is sufficient here.
    /// </summary>
    private static string[] SplitCsvLine(string line)
    {
        var fields = new List<string>();
        var field = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];

            if (inQuotes)
            {
                if (c == '"' && i + 1 < line.Length && line[i + 1] == '"')
                {
                    field.Append('"');
                    i++;
                }
                else if (c == '"')
                {
                    inQuotes = false;
                }
                else
                {
                    field.Append(c);
                }

                continue;
            }

            switch (c)
            {
                case '"':
                    inQuotes = true;
                    break;
                case ',':
                    fields.Add(field.ToString());
                    field.Clear();
                    break;
                default:
                    field.Append(c);
                    break;
            }
        }

        fields.Add(field.ToString());
        return fields.ToArray();
    }
}
