using LankaLens.AdministrativeDivisions;

var sriLanka = AdministrativeDivisions.Default;

Console.WriteLine($"Provinces: {sriLanka.GetProvinces().Count}");
Console.WriteLine($"Districts: {sriLanka.GetDistricts().Count}");
Console.WriteLine($"Dataset: {sriLanka.DatasetMetadata.SourceName}");
Console.WriteLine();

var western = sriLanka.GetProvinceByCode("1");
if (western is not null)
{
    Console.WriteLine($"Province [1] {western.Name.English}");
    Console.WriteLine($"  Sinhala: {western.Name.Sinhala ?? "(not available)"}");
    Console.WriteLine($"  Tamil:   {western.Name.Tamil ?? "(not available)"}");

    var districts = sriLanka.GetDistrictsByProvince(western.Code);
    Console.WriteLine($"  Districts under Western: {districts.Count}");
}

Console.WriteLine();
var results = sriLanka.Search(
    "Colombo",
    new AdministrativeDivisionSearchOptions
    {
        Language = Language.English,
        MaxResults = 5
    });

Console.WriteLine("Search 'Colombo' (English):");
foreach (var hit in results)
{
    Console.WriteLine($"  [{hit.Type}] {hit.Code} — {hit.Name.English}");
}

// Grama Niladhari coordinates are a DataBuilder-generated CSV, not part of the package API
// (see GnCoordinateLookup.cs). Join each GN division's code to its derived location.
Console.WriteLine();
var colomboDs = sriLanka.GetDivisionalSecretariatsByDistrict("11").FirstOrDefault(); // Colombo DS
if (colomboDs is not null)
{
    var gramaNiladhariDivisions = sriLanka
        .GetGramaNiladhariDivisionsByDivisionalSecretariat(colomboDs.Code)
        .Take(3);

    Console.WriteLine($"Grama Niladhari divisions under {colomboDs.Name.English} DS, with derived coordinates:");
    foreach (var gn in gramaNiladhariDivisions)
    {
        var location = GnCoordinateLookup.FindLocation(gn.Code);
        Console.WriteLine(location is not null
            ? $"  {gn.Code} {gn.Name.English}: {location.Latitude:F6}, {location.Longitude:F6}"
              + $" (area {location.AreaSqKm:F2} km², match: {location.MatchKind})"
            : $"  {gn.Code} {gn.Name.English}: no coordinate attributed");
    }
}
