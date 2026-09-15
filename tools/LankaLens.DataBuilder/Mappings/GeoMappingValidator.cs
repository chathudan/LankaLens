using LankaLens.DataBuilder.Models;

namespace LankaLens.DataBuilder.Mappings;

internal sealed record GeoMappingValidationResult(IReadOnlyList<string> Issues)
{
    public bool Passed => Issues.Count == 0;
}

/// <summary>
/// Gate for <c>geo-to-dcs.json</c>. Every mapping must name real codes on both sides, carry
/// written evidence, and — for a DS-segment recode — show an exact GN-component bijection.
/// </summary>
internal static class GeoMappingValidator
{
    public static GeoMappingValidationResult Validate(
        IReadOnlyList<AdministrativeCodeMapping> mappings,
        CanonicalDataset dcs,
        IReadOnlyList<RawGnGeometryRecord> geoRecords)
    {
        var issues = new List<string>();

        var dcsGnCodes = dcs.GramaNiladhariDivisions
            .Select(g => g.Code)
            .ToHashSet(StringComparer.Ordinal);
        var dcsDsCodes = dcs.DivisionalSecretariats
            .Select(d => d.Code)
            .ToHashSet(StringComparer.Ordinal);

        var geoCodes = geoRecords
            .Where(r => r.AdminCode is not null)
            .Select(r => r.AdminCode!)
            .ToHashSet(StringComparer.Ordinal);
        var geoDsCodes = geoCodes
            .Select(c => c[..4])
            .ToHashSet(StringComparer.Ordinal);

        var seenTargets = new HashSet<string>(StringComparer.Ordinal);

        foreach (var mapping in mappings)
        {
            var id = $"{mapping.Type} {mapping.SourceCode}→{mapping.TargetCode}";

            if (!AdministrativeMappingTypes.Supported.Contains(mapping.Type))
            {
                issues.Add($"{id}: unsupported type '{mapping.Type}'.");
                continue;
            }

            if (mapping.Type is not (AdministrativeMappingTypes.DivisionalSecretariat
                or AdministrativeMappingTypes.GramaNiladhariDivision))
            {
                issues.Add($"{id}: only DivisionalSecretariat and GramaNiladhariDivision mappings are supported for geometry.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(mapping.Evidence))
            {
                issues.Add($"{id}: evidence is required.");
            }

            if (string.IsNullOrWhiteSpace(mapping.ReviewNote))
            {
                issues.Add($"{id}: reviewNote is required.");
            }

            if (mapping.SourceId != Joining.GeoDcsJoiner.GeoSourceId)
            {
                issues.Add($"{id}: sourceId must be '{Joining.GeoDcsJoiner.GeoSourceId}'.");
            }

            if (!seenTargets.Add(mapping.TargetCode))
            {
                issues.Add($"{id}: duplicate targetCode '{mapping.TargetCode}'.");
            }

            if (mapping.Type == AdministrativeMappingTypes.DivisionalSecretariat)
            {
                ValidateDsRecode(mapping, id, dcsDsCodes, geoDsCodes, dcsGnCodes, geoCodes, issues);
            }
            else
            {
                ValidateGnMapping(mapping, id, dcsGnCodes, geoCodes, issues);
            }
        }

        return new GeoMappingValidationResult(issues);
    }

    private static void ValidateDsRecode(
        AdministrativeCodeMapping mapping,
        string id,
        HashSet<string> dcsDsCodes,
        HashSet<string> geoDsCodes,
        HashSet<string> dcsGnCodes,
        HashSet<string> geoCodes,
        List<string> issues)
    {
        if (mapping.ChildPropagation != AdministrativeMappingTypes.ChildPropagationGnComponentUnchanged)
        {
            issues.Add(
                $"{id}: DS geometry mappings must set childPropagation="
                + $"'{AdministrativeMappingTypes.ChildPropagationGnComponentUnchanged}'.");
            return;
        }

        if (!dcsDsCodes.Contains(mapping.TargetCode))
        {
            issues.Add($"{id}: targetCode is not a DCS divisional secretariat code.");
        }

        if (!geoDsCodes.Contains(mapping.SourceCode))
        {
            issues.Add($"{id}: sourceCode is not present as a DS prefix in the NSDI snapshot.");
            return;
        }

        // Every DCS child must have a same-suffix NSDI counterpart, or the recode claim is unproven.
        var dcsChildren = dcsGnCodes.Where(c => c.StartsWith(mapping.TargetCode, StringComparison.Ordinal)).ToList();
        var missing = dcsChildren
            .Where(c => !geoCodes.Contains(mapping.SourceCode + c[4..]))
            .OrderBy(c => c, StringComparer.Ordinal)
            .ToList();

        if (missing.Count > 0)
        {
            issues.Add(
                $"{id}: GN-component bijection incomplete — {missing.Count} of {dcsChildren.Count} "
                + $"DCS child codes have no NSDI counterpart (e.g. {string.Join(", ", missing.Take(3))}).");
        }
    }

    private static void ValidateGnMapping(
        AdministrativeCodeMapping mapping,
        string id,
        HashSet<string> dcsGnCodes,
        HashSet<string> geoCodes,
        List<string> issues)
    {
        if (!dcsGnCodes.Contains(mapping.TargetCode))
        {
            issues.Add($"{id}: targetCode is not a DCS Grama Niladhari code.");
        }

        if (!geoCodes.Contains(mapping.SourceCode))
        {
            issues.Add($"{id}: sourceCode is not present in the NSDI snapshot.");
        }
    }
}
