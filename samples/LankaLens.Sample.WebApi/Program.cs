using LankaLens.AdministrativeDivisions;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var sriLanka = AdministrativeDivisions.Default;

app.MapGet("/", () => Results.Ok(new
{
    message = "LankaLens.Sample.WebApi",
    provinces = sriLanka.GetProvinces().Count,
    districts = sriLanka.GetDistricts().Count,
    divisionalSecretariats = sriLanka.GetDivisionalSecretariats().Count,
    gramaNiladhariDivisions = sriLanka.GetGramaNiladhariDivisions().Count
}));

app.MapGet("/provinces", () =>
    sriLanka.GetProvinces().Select(p => new
    {
        p.Code,
        english = p.Name.English,
        sinhala = p.Name.Sinhala,
        tamil = p.Name.Tamil
    }));

app.MapGet("/districts/{code}", (string code) =>
{
    var district = sriLanka.GetDistrictByCode(code);
    return district is null
        ? Results.NotFound()
        : Results.Ok(new
        {
            district.Code,
            district.ProvinceCode,
            english = district.Name.English,
            sinhala = district.Name.Sinhala,
            tamil = district.Name.Tamil
        });
});

app.MapGet("/gramaniladhari/{code}", (string code) =>
{
    var gn = sriLanka.GetGramaNiladhariDivisionByCode(code);
    if (gn is null)
    {
        return Results.NotFound();
    }

    // Coordinates are not part of the LankaLens.AdministrativeDivisions package API - they're
    // read from a DataBuilder-generated CSV bundled as sample data (see GnCoordinateLookup.cs).
    // Derived from 2017 NSDI satellite-traced polygons: approximate, not survey-grade, and null
    // for the ~0.35% of divisions with no attributed polygon.
    var location = GnCoordinateLookup.FindLocation(gn.Code);

    return Results.Ok(new
    {
        gn.Code,
        gn.DivisionalSecretariatCode,
        english = gn.Name.English,
        sinhala = gn.Name.Sinhala,
        tamil = gn.Name.Tamil,
        location = location is null
            ? null
            : new
            {
                location.Latitude,
                location.Longitude,
                areaSqKm = location.AreaSqKm,
                match = location.MatchKind
            }
    });
});

app.MapGet("/districts/{districtCode}/gramaniladhari", (string districtCode) =>
{
    var divisionalSecretariats = sriLanka.GetDivisionalSecretariatsByDistrict(districtCode);
    if (divisionalSecretariats.Count == 0)
    {
        return Results.NotFound();
    }

    var withLocations = divisionalSecretariats
        .SelectMany(ds => sriLanka.GetGramaNiladhariDivisionsByDivisionalSecretariat(ds.Code))
        .Select(gn =>
        {
            var location = GnCoordinateLookup.FindLocation(gn.Code);
            return new
            {
                gn.Code,
                gn.DivisionalSecretariatCode,
                english = gn.Name.English,
                latitude = location?.Latitude,
                longitude = location?.Longitude
            };
        });

    return Results.Ok(withLocations);
});

app.MapGet("/search", (string q, Language? language, int? maxResults) =>
{
    if (string.IsNullOrWhiteSpace(q))
    {
        return Results.BadRequest(new { error = "Query parameter 'q' is required." });
    }

    var results = sriLanka.Search(
        q,
        new AdministrativeDivisionSearchOptions
        {
            Language = language,
            MaxResults = maxResults
        });

    return Results.Ok(results.Select(r => new
    {
        r.Code,
        type = r.Type.ToString(),
        english = r.Name.English,
        sinhala = r.Name.Sinhala,
        tamil = r.Name.Tamil,
        r.ProvinceCode,
        r.DistrictCode,
        r.DivisionalSecretariatCode
    }));
});

app.Run();
