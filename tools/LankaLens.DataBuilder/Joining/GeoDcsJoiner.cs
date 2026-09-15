using LankaLens.DataBuilder.Mappings;
using LankaLens.DataBuilder.Models;

namespace LankaLens.DataBuilder.Joining;

/// <summary>
/// Attributes NSDI Grama Niladhari polygons to DCS Grama Niladhari codes.
/// </summary>
/// <remarks>
/// The NSDI <c>admin_code</c> attribute carries the same seven-digit layout as the DCS
/// <c>GND_UID</c> (province + district + two-digit DS + three-digit GN), so the primary join
/// is an exact code match. Residual DCS codes are resolved only through confirmed mappings in
/// <c>geo-to-dcs.json</c>. Name-only matching is never performed.
/// </remarks>
internal sealed class GeoDcsJoiner
{
    public const string GeoSourceId = "nsdi-gnd-boundaries";
    public const string MappingFileName = "geo-to-dcs.json";

    public static GeoJoinResult Join(
        CanonicalDataset dcs,
        IReadOnlyList<RawGnGeometryRecord> geoRecords,
        IReadOnlyList<AdministrativeCodeMapping> mappings)
    {
        var conflicts = new List<GeoJoinConflict>();
        var byAdminCode = IndexByAdminCode(geoRecords, conflicts);

        // DCS DS code -> NSDI DS code, from confirmed DS-segment recodes.
        var dsRecodes = mappings
            .Where(m => m.Type == AdministrativeMappingTypes.DivisionalSecretariat
                && m.ChildPropagation == AdministrativeMappingTypes.ChildPropagationGnComponentUnchanged)
            .GroupBy(m => m.TargetCode, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First().SourceCode, StringComparer.Ordinal);

        // DCS GN code -> NSDI GN code, from confirmed one-off GN mappings.
        var gnMappings = mappings
            .Where(m => m.Type == AdministrativeMappingTypes.GramaNiladhariDivision)
            .GroupBy(m => m.TargetCode, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First().SourceCode, StringComparer.Ordinal);

        var entries = new List<GeoJoinEntry>(dcs.GramaNiladhariDivisions.Count);
        var claimedBy = new Dictionary<string, string>(StringComparer.Ordinal);

        var candidates = dcs.GramaNiladhariDivisions
            .ToDictionary(
                gn => gn.Code,
                gn => ResolveGeoCode(gn, byAdminCode, dsRecodes, gnMappings),
                StringComparer.Ordinal);

        var realigned = RealignWithinBlocks(dcs, byAdminCode, candidates, conflicts);

        foreach (var gn in dcs.GramaNiladhariDivisions)
        {
            var (geoCode, kind) = candidates[gn.Code];

            if (geoCode is null || !byAdminCode.TryGetValue(geoCode, out var geo))
            {
                entries.Add(new GeoJoinEntry(
                    gn.Code,
                    null,
                    GeoMatchKind.Unmatched,
                    null,
                    null,
                    EnglishNameAgrees: false));
                continue;
            }

            // A polygon must never be attributed to two DCS codes.
            if (claimedBy.TryGetValue(geoCode, out var owner))
            {
                conflicts.Add(new GeoJoinConflict(
                    gn.Code,
                    "DUPLICATE_GEO_CLAIM",
                    $"NSDI admin_code {geoCode} is already attributed to DCS {owner}; leaving {gn.Code} unmatched."));
                entries.Add(new GeoJoinEntry(
                    gn.Code,
                    null,
                    GeoMatchKind.Unmatched,
                    null,
                    null,
                    EnglishNameAgrees: false));
                continue;
            }

            claimedBy[geoCode] = gn.Code;

            var geoName = geo.CensusEnglishName ?? geo.GnName;
            var agrees = NamesAgree(gn.Name.English, geoName);

            if (!agrees && kind != GeoMatchKind.Code)
            {
                conflicts.Add(new GeoJoinConflict(
                    gn.Code,
                    "MAPPED_NAME_DISAGREEMENT",
                    $"Mapped NSDI {geoCode} English name '{geoName}' differs from DCS '{gn.Name.English}'."));
            }

            entries.Add(new GeoJoinEntry(gn.Code, geoCode, kind, geo.Measures, geoName, agrees));
        }

        var unused = byAdminCode.Keys
            .Where(code => !claimedBy.ContainsKey(code))
            .OrderBy(code => code, StringComparer.Ordinal)
            .ToList();

        var summary = new GeoJoinSummary(
            DcsGramaNiladhariDivisions: dcs.GramaNiladhariDivisions.Count,
            GeoFeatures: geoRecords.Count,
            GeoFeaturesWithJoinableCode: byAdminCode.Count,
            MatchedByCode: entries.Count(e => e.MatchKind == GeoMatchKind.Code),
            MatchedByDsRecode: entries.Count(e => e.MatchKind == GeoMatchKind.DsRecode),
            MatchedByGnMapping: entries.Count(e => e.MatchKind == GeoMatchKind.GnMapping),
            Unmatched: entries.Count(e => e.MatchKind == GeoMatchKind.Unmatched),
            GeoUnused: unused.Count,
            EnglishNameDisagreements: entries.Count(e => e.GeoAdminCode is not null && !e.EnglishNameAgrees),
            CentroidOutsidePolygon: entries.Count(e => e.Measures is { CentroidInside: false }),
            MatchedByNameRealignment: realigned);

        return new GeoJoinResult(entries, summary, conflicts, unused);
    }

    /// <summary>
    /// Corrects code matches that landed on the wrong division.
    /// </summary>
    /// <remarks>
    /// In a handful of DS blocks the two authorities assign different codes to the same division:
    /// the English name and the NSDI <c>gnd_no_census</c> travel together onto a code that DCS uses
    /// for a different division. Matching on code alone therefore attaches the wrong polygon. Where a
    /// division's own name identifies exactly one feature inside the same DS block — unique on both
    /// sides, so no guesswork is involved — that feature wins over the code. The DS block itself is
    /// still established by code; this only decides which division within it.
    /// </remarks>
    private static int RealignWithinBlocks(
        CanonicalDataset dcs,
        IReadOnlyDictionary<string, RawGnGeometryRecord> byAdminCode,
        Dictionary<string, (string? GeoCode, string Kind)> candidates,
        List<GeoJoinConflict> conflicts)
    {
        // Group by the DS block of the feature each division currently points at.
        var blocks = new Dictionary<string, List<CanonicalGramaNiladhariDivision>>(StringComparer.Ordinal);
        foreach (var gn in dcs.GramaNiladhariDivisions)
        {
            var candidate = candidates[gn.Code].GeoCode;
            if (candidate is null)
            {
                continue;
            }

            var block = candidate[..4];
            if (!blocks.TryGetValue(block, out var list))
            {
                blocks[block] = list = [];
            }

            list.Add(gn);
        }

        var featuresByBlock = byAdminCode.Keys
            .GroupBy(code => code[..4], StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.Ordinal);

        var realigned = 0;

        foreach (var (block, divisions) in blocks.OrderBy(b => b.Key, StringComparer.Ordinal))
        {
            if (!featuresByBlock.TryGetValue(block, out var features))
            {
                continue;
            }

            // Only names that are unambiguous on BOTH sides of the block can decide anything.
            var uniqueDcsNames = divisions
                .GroupBy(d => Simplify(d.Name.English ?? string.Empty), StringComparer.Ordinal)
                .Where(g => g.Count() == 1 && g.Key.Length > 0)
                .ToDictionary(g => g.Key, g => g.Single(), StringComparer.Ordinal);

            var uniqueGeoNames = features
                .GroupBy(c => Simplify(
                    byAdminCode[c].CensusEnglishName ?? byAdminCode[c].GnName ?? string.Empty),
                    StringComparer.Ordinal)
                .Where(g => g.Count() == 1 && g.Key.Length > 0)
                .ToDictionary(g => g.Key, g => g.Single(), StringComparer.Ordinal);

            foreach (var division in divisions.OrderBy(d => d.Code, StringComparer.Ordinal))
            {
                var (currentCode, kind) = candidates[division.Code];
                if (currentCode is null || !byAdminCode.TryGetValue(currentCode, out var current))
                {
                    continue;
                }

                var ownName = Simplify(division.Name.English ?? string.Empty);
                var attachedName = Simplify(current.CensusEnglishName ?? current.GnName ?? string.Empty);

                // The code match already agrees with the name: nothing to correct.
                if (ownName.Length == 0 || ownName == attachedName)
                {
                    continue;
                }

                if (!uniqueDcsNames.ContainsKey(ownName)
                    || !uniqueGeoNames.TryGetValue(ownName, out var correctCode)
                    || correctCode == currentCode)
                {
                    continue;
                }

                candidates[division.Code] = (correctCode, GeoMatchKind.NameRealigned);
                realigned++;

                conflicts.Add(new GeoJoinConflict(
                    division.Code,
                    "CODE_NAME_REALIGNED",
                    $"Code match pointed at NSDI {currentCode} '{current.CensusEnglishName ?? current.GnName}', "
                    + $"but '{division.Name.English}' is uniquely NSDI {correctCode} within DS block {block}; "
                    + "attached the name-matched polygon instead."));
            }
        }

        return realigned;
    }

    private static (string? GeoCode, string Kind) ResolveGeoCode(
        CanonicalGramaNiladhariDivision gn,
        IReadOnlyDictionary<string, RawGnGeometryRecord> byAdminCode,
        IReadOnlyDictionary<string, string> dsRecodes,
        IReadOnlyDictionary<string, string> gnMappings)
    {
        if (byAdminCode.ContainsKey(gn.Code))
        {
            return (gn.Code, GeoMatchKind.Code);
        }

        if (gnMappings.TryGetValue(gn.Code, out var mappedGn))
        {
            return (mappedGn, GeoMatchKind.GnMapping);
        }

        // DS-segment recode: keep the three-digit GN component, swap the four-digit DS prefix.
        if (gn.Code.Length == 7
            && dsRecodes.TryGetValue(gn.Code[..4], out var geoDs)
            && geoDs.Length == 4)
        {
            return (geoDs + gn.Code[4..], GeoMatchKind.DsRecode);
        }

        return (null, GeoMatchKind.Unmatched);
    }

    private static Dictionary<string, RawGnGeometryRecord> IndexByAdminCode(
        IReadOnlyList<RawGnGeometryRecord> geoRecords,
        List<GeoJoinConflict> conflicts)
    {
        var index = new Dictionary<string, RawGnGeometryRecord>(StringComparer.Ordinal);

        foreach (var record in geoRecords)
        {
            if (record.AdminCode is null)
            {
                continue;
            }

            if (index.TryGetValue(record.AdminCode, out var existing))
            {
                conflicts.Add(new GeoJoinConflict(
                    record.AdminCode,
                    "DUPLICATE_ADMIN_CODE",
                    $"NSDI returned multiple features for admin_code {record.AdminCode} "
                    + $"('{existing.CensusEnglishName ?? existing.GnName}' and '{record.CensusEnglishName ?? record.GnName}'); keeping the first."));
                continue;
            }

            // A feature with no usable geometry carries no coordinates worth joining.
            if (record.Measures is null)
            {
                conflicts.Add(new GeoJoinConflict(
                    record.AdminCode,
                    "EMPTY_GEOMETRY",
                    $"NSDI feature {record.AdminCode} has no usable polygon geometry."));
                continue;
            }

            index[record.AdminCode] = record;
        }

        return index;
    }

    /// <summary>
    /// Compares English names for corroboration only. Never used to create a match —
    /// it reports whether an already-code-matched pair agrees.
    /// </summary>
    internal static bool NamesAgree(string? dcsName, string? geoName)
    {
        if (string.IsNullOrWhiteSpace(dcsName) || string.IsNullOrWhiteSpace(geoName))
        {
            return false;
        }

        return string.Equals(Simplify(dcsName), Simplify(geoName), StringComparison.Ordinal);
    }

    private static string Simplify(string value)
    {
        Span<char> buffer = stackalloc char[value.Length];
        var length = 0;
        foreach (var c in value)
        {
            if (char.IsLetterOrDigit(c))
            {
                buffer[length++] = char.ToLowerInvariant(c);
            }
        }

        return new string(buffer[..length]);
    }
}
