﻿using SixLabors.ImageSharp.PixelFormats;

namespace Funhouse
{
    public static class Constants
    {
        public const string DefinitionsPath = @"Resources\Satellites.json";

        
        public static class Earth
        {
            /// <summary>
            ///     GRS80 semi-major axis of earth (metres)
            /// </summary>
            public const double RadiusEquator = 6378137;

            /// <summary>
            ///     GRS80 semi-minor axis of earth (metres)
            /// </summary>
            public const double RadiusPolar = 6356752.31414;

            public const double Eccentricity = 0.0818191910435;
        }

        public static class DebugColours
        {
            public static readonly Rgba32 OutsideDisc = Rgba32.ParseHex("556270");
            public static readonly Rgba32 OutsideSatellite = Rgba32.ParseHex("4ECDC4");
            public static readonly Rgba32 Spare1 = Rgba32.ParseHex("C7F464");
            public static readonly Rgba32 Spare2 = Rgba32.ParseHex("FF6B6B");
            public static readonly Rgba32 Spare3 = Rgba32.ParseHex("C44D58");
        }
    }
}