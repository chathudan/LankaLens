using LankaLens.DataBuilder.Generation;
using LankaLens.DataBuilder.Geometry;
using LankaLens.DataBuilder.Joining;
using LankaLens.DataBuilder.Mappings;
using LankaLens.DataBuilder.Models;
using LankaLens.DataBuilder.Parsing;

namespace LankaLens.DataBuilder.Tests;

public sealed class PolygonGeometryTests
{
    private static GeoRing Ring(params (double Lon, double Lat)[] points) =>
        new(points.Select(p => new GeoVertex(p.Lon, p.Lat)).ToList());

    /// <summary>A degree of latitude is ~111.2 km, so a 0.1 deg square near the equator is ~123 km².</summary>
    [Fact]
    public void Square_Has_Expected_Area_And_Centre_Centroid()
    {
        var square = new GeoPolygon(
            Ring((80.0, 7.0), (80.1, 7.0), (80.1, 7.1), (80.0, 7.1)),
            []);

        var measures = PolygonGeometry.Measure([square]);

        Assert.NotNull(measures);
        Assert.Equal(7.05, measures!.Centroid.Latitude, 6);
        Assert.Equal(80.05, measures.Centroid.Longitude, 6);
        Assert.True(measures.CentroidInside);
        Assert.InRange(measures.AreaSqKm, 120, 126);
    }

    [Fact]
    public void Bounding_Box_Spans_Every_Vertex()
    {
        var square = new GeoPolygon(
            Ring((80.0, 7.0), (80.1, 7.0), (80.1, 7.1), (80.0, 7.1)),
            []);

        var measures = PolygonGeometry.Measure([square])!;

        Assert.Equal(7.0, measures.BoundingBox.MinLatitude, 6);
        Assert.Equal(80.0, measures.BoundingBox.MinLongitude, 6);
        Assert.Equal(7.1, measures.BoundingBox.MaxLatitude, 6);
        Assert.Equal(80.1, measures.BoundingBox.MaxLongitude, 6);
    }

    [Fact]
    public void Hole_Is_Subtracted_From_Area()
    {
        var outer = Ring((80.0, 7.0), (80.1, 7.0), (80.1, 7.1), (80.0, 7.1));
        var hole = Ring((80.04, 7.04), (80.06, 7.04), (80.06, 7.06), (80.04, 7.06));

        var solid = PolygonGeometry.Measure([new GeoPolygon(outer, [])])!;
        var withHole = PolygonGeometry.Measure([new GeoPolygon(outer, [hole])])!;

        Assert.True(withHole.AreaSqKm < solid.AreaSqKm);
        // The hole is a fifth of the square's width and height, so a twenty-fifth of its area.
        Assert.InRange(solid.AreaSqKm - withHole.AreaSqKm, solid.AreaSqKm * 0.03, solid.AreaSqKm * 0.05);
    }

    [Fact]
    public void Centroid_Outside_Concave_Shape_Yields_Representative_Point_Inside()
    {
        // A "C" opening east: its area-weighted centroid falls in the notch, outside the shape.
        var c = new GeoPolygon(
            Ring(
                (80.00, 7.00), (80.10, 7.00), (80.10, 7.02), (80.02, 7.02),
                (80.02, 7.08), (80.10, 7.08), (80.10, 7.10), (80.00, 7.10)),
            []);

        var measures = PolygonGeometry.Measure([c]);

        Assert.NotNull(measures);
        Assert.False(measures!.CentroidInside);
        // The representative point keeps the centroid's latitude but moves to a covered longitude.
        Assert.Equal(measures.Centroid.Latitude, measures.RepresentativePoint.Latitude, 9);
        Assert.NotEqual(measures.Centroid.Longitude, measures.RepresentativePoint.Longitude, 9);
        Assert.InRange(measures.RepresentativePoint.Longitude, 80.00, 80.02);
    }

    [Fact]
    public void MultiPolygon_Area_Is_The_Sum_Of_Its_Parts()
    {
        var west = new GeoPolygon(Ring((80.0, 7.0), (80.1, 7.0), (80.1, 7.1), (80.0, 7.1)), []);
        var east = new GeoPolygon(Ring((80.5, 7.0), (80.6, 7.0), (80.6, 7.1), (80.5, 7.1)), []);

        var single = PolygonGeometry.Measure([west])!;
        var both = PolygonGeometry.Measure([west, east])!;

        Assert.InRange(both.AreaSqKm, single.AreaSqKm * 1.95, single.AreaSqKm * 2.05);
        Assert.Equal(80.0, both.BoundingBox.MinLongitude, 6);
        Assert.Equal(80.6, both.BoundingBox.MaxLongitude, 6);
    }

    [Fact]
    public void Empty_Input_Yields_No_Measures()
    {
        Assert.Null(PolygonGeometry.Measure([]));
    }
}

public sealed class NsdiGeoJsonParserTests
{
    private const string Square = "[[[80.0,7.0],[80.1,7.0],[80.1,7.1],[80.0,7.1],[80.0,7.0]]]";

    private static string Feature(string properties, string geometryType = "Polygon", string? coords = null) =>
        $$"""
        {"type":"FeatureCollection","features":[
          {"type":"Feature",
           "geometry":{"type":"{{geometryType}}","coordinates":{{coords ?? Square}}},
           "properties":{{properties}}}]}
        """;

    [Fact]
    public void Parses_Admin_Code_And_Names()
    {
        var json = Feature("""{"admin_code":1103005,"gnd_name_census":"Sammanthranapura","gnd_name":"Sammanthranapura"}""");

        var records = new NsdiGeoJsonParser().Parse(json);

        var record = Assert.Single(records);
        Assert.Equal("1103005", record.AdminCode);
        Assert.Equal("Sammanthranapura", record.CensusEnglishName);
        Assert.NotNull(record.Measures);
    }

    [Fact]
    public void Placeholder_Admin_Code_Zero_Is_Not_Joinable()
    {
        var records = new NsdiGeoJsonParser().Parse(Feature("""{"admin_code":0}"""));

        Assert.Null(Assert.Single(records).AdminCode);
    }

    [Fact]
    public void Truncated_Admin_Code_Is_Not_Joinable()
    {
        // District/DS stubs lack a GN component and must never be matched to a GN code.
        var records = new NsdiGeoJsonParser().Parse(Feature("""{"admin_code":1103}"""));

        Assert.Null(Assert.Single(records).AdminCode);
    }

    [Fact]
    public void MultiPolygon_Is_Parsed()
    {
        var multi = $"[{Square},[[[80.5,7.0],[80.6,7.0],[80.6,7.1],[80.5,7.1],[80.5,7.0]]]]";
        var json = Feature("""{"admin_code":1103005}""", "MultiPolygon", multi);

        var record = Assert.Single(new NsdiGeoJsonParser().Parse(json));

        Assert.NotNull(record.Measures);
        Assert.Equal(80.6, record.Measures!.BoundingBox.MaxLongitude, 6);
    }

    [Fact]
    public void Non_FeatureCollection_Throws()
    {
        Assert.Throws<InvalidOperationException>(
            () => new NsdiGeoJsonParser().Parse("""{"error":{"code":400}}"""));
    }
}

public sealed class GeoDcsJoinerTests
{
    private static CanonicalDataset Dataset(params string[] gnCodes) =>
        new(
            new CanonicalDatasetMetadata("org", "name", null, null, new DateOnly(2026, 1, 1)),
            [new CanonicalProvince("1", new CanonicalLocalizedName("Western", null, null))],
            [new CanonicalDistrict("11", "1", new CanonicalLocalizedName("Colombo", null, null))],
            gnCodes.Select(c => c[..4]).Distinct(StringComparer.Ordinal)
                .Select(ds => new CanonicalDivisionalSecretariat(ds, "11", new CanonicalLocalizedName("DS " + ds, null, null)))
                .ToList(),
            gnCodes.Select(c => new CanonicalGramaNiladhariDivision(
                c, c[..4], new CanonicalLocalizedName("GN " + c, null, null))).ToList());

    private static RawGnGeometryRecord Geo(string adminCode, string? name = null) =>
        new()
        {
            AdminCode = adminCode,
            CensusEnglishName = name ?? "GN " + adminCode,
            Measures = PolygonGeometry.Measure(
            [
                new GeoPolygon(
                    new GeoRing([
                        new GeoVertex(80.0, 7.0), new GeoVertex(80.1, 7.0),
                        new GeoVertex(80.1, 7.1), new GeoVertex(80.0, 7.1)
                    ]),
                    [])
            ])
        };

    private static AdministrativeCodeMapping Mapping(
        string type,
        string source,
        string target,
        string? childPropagation) =>
        new(type, source, target, "recode", GeoDcsJoiner.GeoSourceId, "evidence", "url", null, "note", childPropagation, false);

    [Fact]
    public void Exact_Code_Match_Attaches_Geometry()
    {
        var result = GeoDcsJoiner.Join(Dataset("1103005"), [Geo("1103005")], []);

        var entry = Assert.Single(result.Entries);
        Assert.Equal(GeoMatchKind.Code, entry.MatchKind);
        Assert.NotNull(entry.Measures);
        Assert.True(entry.EnglishNameAgrees);
        Assert.Equal(1, result.Summary.MatchedByCode);
    }

    [Fact]
    public void Unmatched_Code_Yields_No_Geometry()
    {
        var result = GeoDcsJoiner.Join(Dataset("1103005"), [Geo("9999999")], []);

        var entry = Assert.Single(result.Entries);
        Assert.Equal(GeoMatchKind.Unmatched, entry.MatchKind);
        Assert.Null(entry.Measures);
        Assert.Equal(1, result.Summary.Unmatched);
        Assert.Single(result.UnusedGeoAdminCodes);
    }

    [Fact]
    public void Ds_Recode_Preserves_The_Gn_Component()
    {
        var mappings = new[]
        {
            Mapping(
                AdministrativeMappingTypes.DivisionalSecretariat,
                source: "2303",
                target: "2302",
                AdministrativeMappingTypes.ChildPropagationGnComponentUnchanged)
        };

        var result = GeoDcsJoiner.Join(Dataset("2302005"), [Geo("2303005")], mappings);

        var entry = Assert.Single(result.Entries);
        Assert.Equal(GeoMatchKind.DsRecode, entry.MatchKind);
        Assert.Equal("2303005", entry.GeoAdminCode);
        Assert.Equal(1, result.Summary.MatchedByDsRecode);
    }

    [Fact]
    public void Ds_Mapping_Without_GnComponentUnchanged_Is_Ignored()
    {
        var mappings = new[]
        {
            Mapping(AdministrativeMappingTypes.DivisionalSecretariat, "2303", "2302", childPropagation: null)
        };

        var result = GeoDcsJoiner.Join(Dataset("2302005"), [Geo("2303005")], mappings);

        Assert.Equal(GeoMatchKind.Unmatched, Assert.Single(result.Entries).MatchKind);
    }

    [Fact]
    public void Gn_Level_Mapping_Attaches_A_Renumbered_Polygon()
    {
        var mappings = new[]
        {
            Mapping(AdministrativeMappingTypes.GramaNiladhariDivision, "9103052", "9103210", null)
        };

        var result = GeoDcsJoiner.Join(Dataset("9103210"), [Geo("9103052")], mappings);

        var entry = Assert.Single(result.Entries);
        Assert.Equal(GeoMatchKind.GnMapping, entry.MatchKind);
        Assert.Equal("9103052", entry.GeoAdminCode);
    }

    [Fact]
    public void A_Polygon_Is_Never_Attributed_To_Two_Dcs_Codes()
    {
        // Both DCS codes resolve to the same NSDI feature; the second must be refused, not duplicated.
        var mappings = new[]
        {
            Mapping(AdministrativeMappingTypes.GramaNiladhariDivision, "1103005", "1103010", null)
        };

        var result = GeoDcsJoiner.Join(Dataset("1103005", "1103010"), [Geo("1103005")], mappings);

        Assert.Equal(1, result.Summary.Matched);
        Assert.Equal(1, result.Summary.Unmatched);
        Assert.Contains(result.Conflicts, c => c.Issue == "DUPLICATE_GEO_CLAIM");
    }

    [Fact]
    public void Swapped_Codes_Within_A_Ds_Block_Are_Realigned_By_Name()
    {
        // DCS and NSDI disagree about which code belongs to which division: the names are swapped.
        var dataset = new CanonicalDataset(
            new CanonicalDatasetMetadata("org", "name", null, null, new DateOnly(2026, 1, 1)),
            [new CanonicalProvince("2", new CanonicalLocalizedName("Central", null, null))],
            [new CanonicalDistrict("22", "2", new CanonicalLocalizedName("Matale", null, null))],
            [new CanonicalDivisionalSecretariat("2203", "22", new CanonicalLocalizedName("DS", null, null))],
            [
                new CanonicalGramaNiladhariDivision("2203110", "2203", new CanonicalLocalizedName("Silwathgama", null, null)),
                new CanonicalGramaNiladhariDivision("2203145", "2203", new CanonicalLocalizedName("Puwakpitiya", null, null))
            ]);

        var result = GeoDcsJoiner.Join(
            dataset,
            [Geo("2203110", "Puwakpitiya"), Geo("2203145", "Silwathgama")],
            []);

        var silwathgama = result.Entries.Single(e => e.DcsCode == "2203110");
        var puwakpitiya = result.Entries.Single(e => e.DcsCode == "2203145");

        Assert.Equal(GeoMatchKind.NameRealigned, silwathgama.MatchKind);
        Assert.Equal("2203145", silwathgama.GeoAdminCode);
        Assert.Equal("2203110", puwakpitiya.GeoAdminCode);
        Assert.True(silwathgama.EnglishNameAgrees);
        Assert.True(puwakpitiya.EnglishNameAgrees);
        Assert.Equal(2, result.Summary.MatchedByNameRealignment);
        Assert.Contains(result.Conflicts, c => c.Issue == "CODE_NAME_REALIGNED");
    }

    [Fact]
    public void Ambiguous_Names_Within_A_Block_Are_Not_Realigned()
    {
        // Two divisions share a name, so nothing in the block can be decided by name.
        var dataset = new CanonicalDataset(
            new CanonicalDatasetMetadata("org", "name", null, null, new DateOnly(2026, 1, 1)),
            [new CanonicalProvince("2", new CanonicalLocalizedName("Central", null, null))],
            [new CanonicalDistrict("22", "2", new CanonicalLocalizedName("Matale", null, null))],
            [new CanonicalDivisionalSecretariat("2203", "22", new CanonicalLocalizedName("DS", null, null))],
            [
                new CanonicalGramaNiladhariDivision("2203110", "2203", new CanonicalLocalizedName("Repeated", null, null)),
                new CanonicalGramaNiladhariDivision("2203145", "2203", new CanonicalLocalizedName("Repeated", null, null))
            ]);

        var result = GeoDcsJoiner.Join(
            dataset,
            [Geo("2203110", "Other"), Geo("2203145", "Repeated")],
            []);

        Assert.Equal(0, result.Summary.MatchedByNameRealignment);
        Assert.All(result.Entries, e => Assert.Equal(GeoMatchKind.Code, e.MatchKind));
    }

    [Fact]
    public void Name_Disagreement_Is_Reported_But_Does_Not_Block_A_Code_Match()
    {
        var result = GeoDcsJoiner.Join(Dataset("1103005"), [Geo("1103005", "Something Else")], []);

        var entry = Assert.Single(result.Entries);
        Assert.Equal(GeoMatchKind.Code, entry.MatchKind);
        Assert.False(entry.EnglishNameAgrees);
        Assert.Equal(1, result.Summary.EnglishNameDisagreements);
    }

    [Theory]
    [InlineData("Dambagolla", "Dambagolla", true)]
    [InlineData("New Weligama", "new  weligama", true)]
    [InlineData("Kotagepitiya North", "Kotagepitiya-North", true)]
    [InlineData("Dambagolla", "Dambagalla", false)]
    [InlineData("Colombo", null, false)]
    public void Name_Agreement_Ignores_Case_Spacing_And_Punctuation_Only(
        string dcsName,
        string? geoName,
        bool expected)
    {
        Assert.Equal(expected, GeoDcsJoiner.NamesAgree(dcsName, geoName));
    }
}

public sealed class GeoEnrichedCsvWriterTests
{
    private static RawAdministrativeRecord Row(string uid, string name) => new()
    {
        SerialNumber = "1",
        GnUid = uid,
        ProvinceCode = "1",
        ProvinceEnglish = "Western",
        DistrictCode = "11",
        DistrictEnglish = "Colombo",
        DsCode = "1103",
        DsEnglish = "Colombo",
        GnCode = "5",
        GnEnglish = name,
        LgCode = "11",
        LgName = "Colombo MC"
    };

    private static GeoJoinResult Join(params GeoJoinEntry[] entries) =>
        new(entries, new GeoJoinSummary(entries.Length, 0, 0, 0, 0, 0, 0, 0, 0, 0), [], []);

    [Fact]
    public void Writes_Original_Columns_Plus_Coordinates()
    {
        var measures = PolygonGeometry.Measure(
        [
            new GeoPolygon(
                new GeoRing([
                    new GeoVertex(80.0, 7.0), new GeoVertex(80.1, 7.0),
                    new GeoVertex(80.1, 7.1), new GeoVertex(80.0, 7.1)
                ]),
                [])
        ]);

        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".csv");
        try
        {
            GeoEnrichedCsvWriter.Write(
                [Row("1103005", "Sammanthranapura")],
                Join(new GeoJoinEntry("1103005", "1103005", GeoMatchKind.Code, measures, "Sammanthranapura", true)),
                path);

            var lines = File.ReadAllLines(path);
            Assert.Equal(2, lines.Length);
            Assert.StartsWith("Serial Number,GND_UID,", lines[0], StringComparison.Ordinal);
            Assert.Contains("latitude,longitude,centroid_latitude", lines[0], StringComparison.Ordinal);

            var fields = lines[1].Split(',');
            Assert.Equal("1103005", fields[1]);
            Assert.Equal("Sammanthranapura", fields[10]);
            Assert.Equal("7.050000", fields[13]);
            Assert.Equal("80.050000", fields[14]);
            Assert.Equal("code", fields[23]);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Unmatched_Row_Keeps_Its_Columns_And_Leaves_Coordinates_Empty()
    {
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".csv");
        try
        {
            GeoEnrichedCsvWriter.Write(
                [Row("4415095", "Kokuthoduvai South")],
                Join(new GeoJoinEntry("4415095", null, GeoMatchKind.Unmatched, null, null, false)),
                path);

            var fields = File.ReadAllLines(path)[1].Split(',');
            Assert.Equal("4415095", fields[1]);
            Assert.Equal("Kokuthoduvai South", fields[10]);
            // Never zero, never a placeholder.
            Assert.Equal(string.Empty, fields[13]);
            Assert.Equal(string.Empty, fields[14]);
            Assert.Equal("unmatched", fields[23]);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Names_Containing_A_Comma_Are_Quoted()
    {
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".csv");
        try
        {
            GeoEnrichedCsvWriter.Write(
                [Row("1103005", "Colombo, North")],
                Join(new GeoJoinEntry("1103005", null, GeoMatchKind.Unmatched, null, null, false)),
                path);

            Assert.Contains("\"Colombo, North\"", File.ReadAllText(path), StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
