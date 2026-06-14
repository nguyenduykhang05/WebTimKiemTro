using System;

namespace SmartRoomFinder.Helpers
{
    public static class HaversineHelper
    {
        private const double EarthRadiusKm = 6371.0;

        public static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            var dLat = DegreesToRadians(lat2 - lat1);
            var dLon = DegreesToRadians(lon2 - lon1);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return EarthRadiusKm * c;
        }

        private static double DegreesToRadians(double degrees)
        {
            return degrees * Math.PI / 180.0;
        }

        public static (double MinLat, double MaxLat, double MinLng, double MaxLng) GetBoundingBox(double lat, double lng, double radiusKm)
        {
            var latDelta = (radiusKm / EarthRadiusKm) * (180.0 / Math.PI);
            var lngDelta = (radiusKm / (EarthRadiusKm * Math.Cos(lat * Math.PI / 180.0))) * (180.0 / Math.PI);

            return (
                lat - latDelta,
                lat + latDelta,
                lng - lngDelta,
                lng + lngDelta
            );
        }
    }
}
