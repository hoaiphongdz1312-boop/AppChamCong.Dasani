namespace NguyenHoaiPhong_1681.Helpers;

public static class GeoHelper
{
    private const double EarthRadiusMeters = 6_371_000;

    /// <summary>Khoảng cách giữa hai tọa độ (mét) theo công thức Haversine.</summary>
    public static double Haversine(double lat1, double lon1, double lat2, double lon2)
    {
        static double Rad(double d) => d * Math.PI / 180;
        var dLat = Rad(lat2 - lat1);
        var dLon = Rad(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(Rad(lat1)) * Math.Cos(Rad(lat2)) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return EarthRadiusMeters * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }
}
