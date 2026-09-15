namespace LankaLens.DataBuilder.Geometry;

/// <summary>One WGS84 vertex as stored in GeoJSON coordinate order (longitude first).</summary>
internal readonly record struct GeoVertex(double Longitude, double Latitude);

/// <summary>A WGS84 point in latitude/longitude order.</summary>
internal readonly record struct GeoPoint(double Latitude, double Longitude);

/// <summary>Axis-aligned WGS84 envelope.</summary>
internal readonly record struct GeoBoundingBox(
    double MinLatitude,
    double MinLongitude,
    double MaxLatitude,
    double MaxLongitude);

/// <summary>One polygon ring. The first and last vertex may or may not be repeated.</summary>
internal sealed record GeoRing(IReadOnlyList<GeoVertex> Vertices);

/// <summary>A polygon with one exterior ring and zero or more interior rings (holes).</summary>
internal sealed record GeoPolygon(GeoRing Exterior, IReadOnlyList<GeoRing> Holes);

/// <summary>
/// Derived point and extent measures for one feature's geometry.
/// </summary>
/// <param name="Centroid">Area-weighted centroid. May fall outside concave or multi-part shapes.</param>
/// <param name="RepresentativePoint">A point guaranteed to lie inside the shape.</param>
/// <param name="BoundingBox">Axis-aligned envelope over every ring.</param>
/// <param name="AreaSqKm">Exterior area minus hole area.</param>
/// <param name="CentroidInside">Whether <paramref name="Centroid"/> lies inside the shape.</param>
internal sealed record PolygonMeasures(
    GeoPoint Centroid,
    GeoPoint RepresentativePoint,
    GeoBoundingBox BoundingBox,
    double AreaSqKm,
    bool CentroidInside);

/// <summary>
/// Planar geometry for small administrative polygons.
/// </summary>
/// <remarks>
/// Rings are projected to a local equirectangular frame in metres about the shape's own
/// bounding-box centre. Over a Grama Niladhari division (a few kilometres across at most)
/// the resulting centroid and area error is far below the precision of the underlying
/// 2017 satellite-traced boundaries, and the projection keeps the maths exactly
/// reproducible across platforms.
/// </remarks>
internal static class PolygonGeometry
{
    /// <summary>Mean Earth radius (IUGG) in metres.</summary>
    private const double EarthRadiusMetres = 6371008.8;

    public static PolygonMeasures? Measure(IReadOnlyList<GeoPolygon> polygons)
    {
        if (polygons.Count == 0)
        {
            return null;
        }

        var box = ComputeBoundingBox(polygons);
        if (box is null)
        {
            return null;
        }

        var envelope = box.Value;
        var latOrigin = (envelope.MinLatitude + envelope.MaxLatitude) / 2.0;
        var lonOrigin = (envelope.MinLongitude + envelope.MaxLongitude) / 2.0;
        var cosLat = Math.Cos(latOrigin * Math.PI / 180.0);

        // Project once; every later step works in metres.
        var projected = polygons
            .Select(p => new ProjectedPolygon(
                Project(p.Exterior, latOrigin, lonOrigin, cosLat),
                p.Holes.Select(h => Project(h, latOrigin, lonOrigin, cosLat)).ToList()))
            .ToList();

        double totalArea = 0;
        double weightedX = 0;
        double weightedY = 0;

        foreach (var polygon in projected)
        {
            var (exteriorArea, exteriorCx, exteriorCy) = RingCentroid(polygon.Exterior);
            var netArea = Math.Abs(exteriorArea);
            var cx = exteriorCx * Math.Abs(exteriorArea);
            var cy = exteriorCy * Math.Abs(exteriorArea);

            foreach (var hole in polygon.Holes)
            {
                var (holeArea, holeCx, holeCy) = RingCentroid(hole);
                netArea -= Math.Abs(holeArea);
                cx -= holeCx * Math.Abs(holeArea);
                cy -= holeCy * Math.Abs(holeArea);
            }

            if (netArea <= 0)
            {
                continue;
            }

            totalArea += netArea;
            weightedX += cx;
            weightedY += cy;
        }

        if (totalArea <= 0)
        {
            // Degenerate geometry: fall back to the envelope centre.
            var fallback = new GeoPoint(latOrigin, lonOrigin);
            return new PolygonMeasures(fallback, fallback, envelope, 0, CentroidInside: false);
        }

        var centroidX = weightedX / totalArea;
        var centroidY = weightedY / totalArea;
        var centroid = Unproject(centroidX, centroidY, latOrigin, lonOrigin, cosLat);

        var inside = IsInside(projected, centroidX, centroidY);
        var representative = inside
            ? centroid
            : FindRepresentativePoint(projected, centroidY, latOrigin, lonOrigin, cosLat) ?? centroid;

        return new PolygonMeasures(
            centroid,
            representative,
            envelope,
            totalArea / 1_000_000.0,
            inside);
    }

    private static GeoBoundingBox? ComputeBoundingBox(IReadOnlyList<GeoPolygon> polygons)
    {
        double minLat = double.MaxValue, minLon = double.MaxValue;
        double maxLat = double.MinValue, maxLon = double.MinValue;
        var any = false;

        foreach (var polygon in polygons)
        {
            foreach (var ring in polygon.Holes.Prepend(polygon.Exterior))
            {
                foreach (var v in ring.Vertices)
                {
                    any = true;
                    minLat = Math.Min(minLat, v.Latitude);
                    maxLat = Math.Max(maxLat, v.Latitude);
                    minLon = Math.Min(minLon, v.Longitude);
                    maxLon = Math.Max(maxLon, v.Longitude);
                }
            }
        }

        return any ? new GeoBoundingBox(minLat, minLon, maxLat, maxLon) : null;
    }

    private static double[] Project(GeoRing ring, double latOrigin, double lonOrigin, double cosLat)
    {
        // Flat [x0, y0, x1, y1, ...] keeps the inner loops allocation-free.
        var coords = new double[ring.Vertices.Count * 2];
        for (var i = 0; i < ring.Vertices.Count; i++)
        {
            var v = ring.Vertices[i];
            coords[i * 2] = EarthRadiusMetres * (v.Longitude - lonOrigin) * Math.PI / 180.0 * cosLat;
            coords[(i * 2) + 1] = EarthRadiusMetres * (v.Latitude - latOrigin) * Math.PI / 180.0;
        }

        return coords;
    }

    private static GeoPoint Unproject(double x, double y, double latOrigin, double lonOrigin, double cosLat)
    {
        var lat = latOrigin + (y / EarthRadiusMetres * 180.0 / Math.PI);
        var lon = lonOrigin + (x / EarthRadiusMetres * 180.0 / Math.PI / cosLat);
        return new GeoPoint(lat, lon);
    }

    private static (double Area, double CentroidX, double CentroidY) RingCentroid(double[] coords)
    {
        var count = coords.Length / 2;
        if (count < 3)
        {
            return (0, 0, 0);
        }

        double area = 0, cx = 0, cy = 0;
        for (var i = 0; i < count; i++)
        {
            var j = (i + 1) % count;
            var xi = coords[i * 2];
            var yi = coords[(i * 2) + 1];
            var xj = coords[j * 2];
            var yj = coords[(j * 2) + 1];

            var cross = (xi * yj) - (xj * yi);
            area += cross;
            cx += (xi + xj) * cross;
            cy += (yi + yj) * cross;
        }

        area /= 2.0;
        if (Math.Abs(area) < double.Epsilon)
        {
            return (0, 0, 0);
        }

        return (area, cx / (6.0 * area), cy / (6.0 * area));
    }

    /// <summary>
    /// Even-odd ray casting across every ring. Holes need no special handling:
    /// a point inside a hole crosses one extra boundary and so reads as outside.
    /// </summary>
    private static bool IsInside(IReadOnlyList<ProjectedPolygon> polygons, double x, double y)
    {
        var inside = false;
        foreach (var polygon in polygons)
        {
            foreach (var ring in polygon.Holes.Prepend(polygon.Exterior))
            {
                var count = ring.Length / 2;
                for (int i = 0, j = count - 1; i < count; j = i++)
                {
                    var yi = ring[(i * 2) + 1];
                    var yj = ring[(j * 2) + 1];
                    if (yi > y == yj > y)
                    {
                        continue;
                    }

                    var xi = ring[i * 2];
                    var xj = ring[j * 2];
                    var crossX = xi + ((y - yi) / (yj - yi) * (xj - xi));
                    if (x < crossX)
                    {
                        inside = !inside;
                    }
                }
            }
        }

        return inside;
    }

    /// <summary>
    /// Sweeps a horizontal line at the centroid's latitude and returns the midpoint of the
    /// widest interior span, which is guaranteed to lie inside the shape.
    /// </summary>
    private static GeoPoint? FindRepresentativePoint(
        IReadOnlyList<ProjectedPolygon> polygons,
        double y,
        double latOrigin,
        double lonOrigin,
        double cosLat)
    {
        var crossings = new List<double>();
        foreach (var polygon in polygons)
        {
            foreach (var ring in polygon.Holes.Prepend(polygon.Exterior))
            {
                var count = ring.Length / 2;
                for (int i = 0, j = count - 1; i < count; j = i++)
                {
                    var yi = ring[(i * 2) + 1];
                    var yj = ring[(j * 2) + 1];
                    if (yi > y == yj > y)
                    {
                        continue;
                    }

                    var xi = ring[i * 2];
                    var xj = ring[j * 2];
                    crossings.Add(xi + ((y - yi) / (yj - yi) * (xj - xi)));
                }
            }
        }

        if (crossings.Count < 2)
        {
            return null;
        }

        crossings.Sort();

        double bestWidth = -1;
        double bestX = 0;
        for (var i = 0; i + 1 < crossings.Count; i += 2)
        {
            var width = crossings[i + 1] - crossings[i];
            if (width > bestWidth)
            {
                bestWidth = width;
                bestX = (crossings[i] + crossings[i + 1]) / 2.0;
            }
        }

        return bestWidth < 0 ? null : Unproject(bestX, y, latOrigin, lonOrigin, cosLat);
    }

    private sealed record ProjectedPolygon(double[] Exterior, IReadOnlyList<double[]> Holes);
}
