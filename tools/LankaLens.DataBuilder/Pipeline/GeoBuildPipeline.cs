using LankaLens.DataBuilder.Acquisition;
using LankaLens.DataBuilder.Generation;
using LankaLens.DataBuilder.Joining;
using LankaLens.DataBuilder.Mappings;
using LankaLens.DataBuilder.Models;
using LankaLens.DataBuilder.Normalization;
using LankaLens.DataBuilder.Parsing;
using LankaLens.DataBuilder.Reporting;
using LankaLens.DataBuilder.Sources;

namespace LankaLens.DataBuilder.Pipeline;

internal sealed record GeoPipelineResult(
    GeoJoinResult Join,
    NsdiSnapshotManifest Manifest,
    int MappingsApplied,
    string CsvPath);

/// <summary>
/// Builds the coordinate-enriched GNDList from the hash-pinned DCS workbook and the
/// cached NSDI boundary snapshot. Never fetches live data and never writes into
/// <c>data/source/</c> or the embedded runtime dataset.
/// </summary>
internal static class GeoBuildPipeline
{
    public static GeoPipelineResult Run(PipelinePaths paths)
    {
        var catalog = SourceCatalogLoader.Load(paths.SourceDirectory);

        var primary = catalog.Sources.FirstOrDefault(s => s.Id == DataBuildPipeline.PrimarySourceId)
            ?? throw new InvalidOperationException(
                $"Source catalog is missing required entry '{DataBuildPipeline.PrimarySourceId}'.");

        // Verifies the SHA-256 pin before parsing.
        var primaryPath = SourceCatalogLoader.ResolveSourcePath(paths.SourceDirectory, primary);
        var rawRecords = new GndListWorkbookParser().Parse(primaryPath);

        if (!DateOnly.TryParse(primary.RetrievedDate, out var retrievedDate))
        {
            throw new InvalidOperationException(
                $"Source catalog entry '{DataBuildPipeline.PrimarySourceId}' is missing a valid retrievedDate.");
        }

        DateOnly? effectiveDate = null;
        if (DateOnly.TryParse(primary.PublishedOrUpdatedDate, out var parsedEffective))
        {
            effectiveDate = parsedEffective;
        }

        var dcsDataset = CanonicalNormalizer.Normalize(
            rawRecords,
            new CanonicalDatasetMetadata(
                primary.Organization,
                primary.Title,
                primary.PublishedOrUpdatedDate,
                effectiveDate,
                retrievedDate));

        var geoEntry = catalog.Sources.FirstOrDefault(s => s.Id == GeoDcsJoiner.GeoSourceId);
        if (!NsdiBoundaryClient.TryLoadVerifiedSnapshot(
                paths.SourceDirectory,
                geoEntry,
                out var manifest,
                out var error)
            || manifest is null)
        {
            throw new InvalidOperationException(
                error ?? "NSDI boundary snapshot could not be loaded. Run acquire-geo.");
        }

        var geoRecords = new NsdiGeoJsonParser()
            .ParseDirectory(NsdiBoundaryClient.CacheDirectory(paths.SourceDirectory));

        var mappingPath = Path.Combine(paths.ResolvedMappingsDirectory, GeoDcsJoiner.MappingFileName);
        var mappings = MappingFileLoader.LoadFrom(mappingPath);

        var validation = GeoMappingValidator.Validate(mappings, dcsDataset, geoRecords);
        if (!validation.Passed)
        {
            var detail = string.Join("; ", validation.Issues);
            throw new InvalidOperationException(
                $"Geo mapping file validation failed ({mappingPath}): {detail}");
        }

        var join = GeoDcsJoiner.Join(dcsDataset, geoRecords, mappings);

        GeoEnrichedCsvWriter.Write(rawRecords, join, paths.GeoEnrichedCsvPath);
        GeoCoverageReportWriter.WriteMarkdown(join, manifest, dcsDataset, paths.GeoCoverageMarkdownPath);
        GeoCoverageReportWriter.WriteGapsJson(join, dcsDataset, paths.GeoGapsJsonPath);

        return new GeoPipelineResult(join, manifest, mappings.Count, paths.GeoEnrichedCsvPath);
    }
}
