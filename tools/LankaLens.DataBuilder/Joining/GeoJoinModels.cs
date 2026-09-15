using LankaLens.DataBuilder.Geometry;

namespace LankaLens.DataBuilder.Joining;

/// <summary>How a DCS Grama Niladhari code was matched to an NSDI polygon.</summary>
internal static class GeoMatchKind
{
    /// <summary>NSDI <c>admin_code</c> equalled the DCS GND_UID directly.</summary>
    public const string Code = "code";

    /// <summary>Matched through a confirmed DS-segment recode with unchanged GN component.</summary>
    public const string DsRecode = "ds-recode";

    /// <summary>Matched through a confirmed one-off GN-level mapping.</summary>
    public const string GnMapping = "gn-mapping";

    /// <summary>
    /// The code match landed on a feature carrying a different name, and the division's own name
    /// identified exactly one other feature inside the same DS block. DCS and NSDI disagree about
    /// which code belongs to which division here, so the name decides.
    /// </summary>
    public const string NameRealigned = "name-realigned";

    /// <summary>No polygon could be attributed to this DCS code.</summary>
    public const string Unmatched = "unmatched";
}

/// <summary>One DCS Grama Niladhari division with whatever geometry was attributed to it.</summary>
internal sealed record GeoJoinEntry(
    string DcsCode,
    string? GeoAdminCode,
    string MatchKind,
    PolygonMeasures? Measures,
    string? GeoEnglishName,
    bool EnglishNameAgrees);

internal sealed record GeoJoinSummary(
    int DcsGramaNiladhariDivisions,
    int GeoFeatures,
    int GeoFeaturesWithJoinableCode,
    int MatchedByCode,
    int MatchedByDsRecode,
    int MatchedByGnMapping,
    int Unmatched,
    int GeoUnused,
    int EnglishNameDisagreements,
    int CentroidOutsidePolygon,
    int MatchedByNameRealignment = 0)
{
    public int Matched =>
        MatchedByCode + MatchedByDsRecode + MatchedByGnMapping + MatchedByNameRealignment;
}

internal sealed record GeoJoinConflict(
    string Code,
    string Issue,
    string Detail);

internal sealed record GeoJoinResult(
    IReadOnlyList<GeoJoinEntry> Entries,
    GeoJoinSummary Summary,
    IReadOnlyList<GeoJoinConflict> Conflicts,
    IReadOnlyList<string> UnusedGeoAdminCodes);
