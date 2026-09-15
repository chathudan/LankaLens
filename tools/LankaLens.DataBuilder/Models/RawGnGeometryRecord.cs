using LankaLens.DataBuilder.Geometry;

namespace LankaLens.DataBuilder.Models;

/// <summary>
/// One Grama Niladhari polygon as read from the NSDI Boundaries GN layer,
/// with its derived point and extent measures.
/// </summary>
internal sealed record RawGnGeometryRecord
{
    /// <summary>
    /// NSDI <c>admin_code</c>, zero-padded to the DCS seven-digit GND_UID layout
    /// (province + district + two-digit DS + three-digit GN). Null when the source
    /// value was absent or a placeholder.
    /// </summary>
    public string? AdminCode { get; init; }

    /// <summary>NSDI <c>gnd_name_census</c> — the census-aligned English name, used as join evidence.</summary>
    public string? CensusEnglishName { get; init; }

    /// <summary>NSDI <c>gnd_name</c> — the layer's own English name.</summary>
    public string? GnName { get; init; }

    /// <summary>NSDI <c>gnd_no_census</c> — the census GN number (e.g. "498A").</summary>
    public string? CensusGnNumber { get; init; }

    /// <summary>NSDI <c>ds_division_census_code</c>.</summary>
    public string? DsCensusCode { get; init; }

    /// <summary>NSDI <c>district_census_code</c>.</summary>
    public string? DistrictCensusCode { get; init; }

    /// <summary>NSDI <c>province_census_code</c>.</summary>
    public string? ProvinceCensusCode { get; init; }

    /// <summary>Year the polygon was created, per the layer's <c>year_created</c> attribute.</summary>
    public string? YearCreated { get; init; }

    /// <summary>Derived centroid, representative point, envelope, and area. Null for empty geometry.</summary>
    public PolygonMeasures? Measures { get; init; }
}
