namespace FlightNetwork.DataAccess.DataSeeding;

/// <summary>
/// Great-circle distance. Computed here at seed time because the database exposes no geospatial
/// functions — `point.distance` is not available on this instance.
/// </summary>
internal static class GeoDistance
{
    /// <summary>IUGG mean Earth radius, in kilometres.</summary>
    private const double EarthRadiusKm = 6371.0088;

    public static double HaversineKm(double lat1, double lon1, double lat2, double lon2)
    {
        var deltaLat = double.DegreesToRadians(lat2 - lat1);
        var deltaLon = double.DegreesToRadians(lon2 - lon1);

        var a = Math.Pow(Math.Sin(deltaLat / 2), 2)
                + Math.Cos(double.DegreesToRadians(lat1))
                * Math.Cos(double.DegreesToRadians(lat2))
                * Math.Pow(Math.Sin(deltaLon / 2), 2);

        // Clamped because floating-point drift can push `a` a hair above 1 for antipodal points,
        // which would make Asin return NaN.
        return 2 * EarthRadiusKm * Math.Asin(Math.Min(1.0, Math.Sqrt(a)));
    }
}
