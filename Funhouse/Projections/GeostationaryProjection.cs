﻿using Funhouse.Models.Angles;
using Funhouse.Models.Configuration;
using MathNet.Spatial.Units;
using SixLabors.ImageSharp;
using static System.Math;
using static Funhouse.Constants.Earth;

namespace Funhouse.Projections
{
    /// <remarks>
    ///     Calculations taken from https://www.goes-r.gov/users/docs/PUG-L1b-vol3.pdf, section 5.1.2.8.1
    /// </remarks>
    public static class GeostationaryProjection
    {
        private const double RadiusPolarSquared = RadiusPolar * RadiusPolar;
        private const double RadiusEquatorSquared = RadiusEquator * RadiusEquator;

        /// <summary>
        ///     Converts a latitude and longitude to a geostationary image scanning angle.
        /// </summary>
        public static ScanningAngle? FromGeodetic(GeodeticAngle geodetic, SatelliteDefinition definition)
        {
            var satelliteLongitude = definition.Longitude;
            var satelliteHeight = definition.Height + RadiusEquator;

            var geocentricLatitude = Atan(RadiusPolarSquared / RadiusEquatorSquared * Tan(geodetic.Latitude.Radians));
            var cosLatitude = Cos(geocentricLatitude);

            var rc = RadiusPolar / Sqrt(1 - Eccentricity * Eccentricity * Pow(cosLatitude, 2));
            var sx = satelliteHeight - rc * cosLatitude * Cos(geodetic.Longitude.Radians - satelliteLongitude.Radians);
            var sy = -rc * cosLatitude * Sin(geodetic.Longitude.Radians - satelliteLongitude.Radians);
            var sz = rc * Sin(geocentricLatitude);

            // Calculate (x,y) scanning angle
            var y = Angle.FromRadians(Atan(sz / sx));
            var x = Angle.FromRadians(Asin(-sy / Sqrt(sx * sx + sy * sy + sz * sz)));

            // Check if geodetic angle is visible from satellite 
            if (satelliteHeight * (satelliteHeight - sx) < sy * sy + RadiusEquatorSquared / RadiusPolarSquared * sz * sz) return null;

            return new ScanningAngle(x, y);
        }


        public static GeodeticAngle ToGeodetic(PointF point, SatelliteDefinition definition)
        {
            var satelliteLongitude = definition.Longitude;
            var satelliteHeight = definition.Height + RadiusEquator;

            var offset = definition.ImageOffset;

            // Convert pixel coordinates into radians
            var x = Angle.FromRadians(offset.X.Radians + point.X * offset.ScaleFactor);
            var y = Angle.FromRadians(offset.Y.Radians - point.Y * offset.ScaleFactor);

            var l0 = satelliteLongitude.Radians;

            var cosX = Cos(x.Radians);
            var cosY = Cos(y.Radians);
            var sinX = Sin(x.Radians);
            var sinY = Sin(y.Radians);

            var a = sinX * sinX + cosX * cosX * (cosY * cosY + RadiusEquatorSquared / RadiusPolarSquared * sinY * sinY);
            var b = -2 * satelliteHeight * cosX * cosY;
            var c = satelliteHeight * satelliteHeight - RadiusEquatorSquared;

            var rs = (-b - Sqrt(b * b - 4 * a * c)) / 2 * a;

            var sx = rs * cosX * cosY;
            var sy = -rs * sinX;
            var sz = rs * cosX * sinY;

            var phi = Angle.FromRadians(Atan(RadiusEquatorSquared / RadiusPolarSquared * (sz / Sqrt((satelliteHeight - sx) * (satelliteHeight - sx) + sy * sy))));
            var lambda = Angle.FromRadians(l0 - Atan(sy / (satelliteHeight - sx)));

            return new GeodeticAngle(phi, lambda);
        }

        /// <summary>
        ///     Returns the pixel coordinates of a geostationary image, given a scanning angle and a spatial resolution.
        /// </summary>
        public static PointF ToImageCoordinates(ScanningAngle angle, SatelliteDefinition definition)
        {
            var offset = definition.ImageOffset;
            
            var x = (angle.X.Radians - offset.X.Radians) / offset.ScaleFactor;
            var y = (offset.Y.Radians - angle.Y.Radians) / offset.ScaleFactor;

            return new PointF((float) x, (float) y);
        }
    }
}