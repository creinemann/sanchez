﻿using System.Collections.Generic;
using System.Linq;
using Funhouse.Models.Projections;
using Serilog;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Funhouse.Services.Equirectangular
{
    public interface IImageStitcher
    {
        /// <summary>
        ///     Stitches equirectangular satellite IR images into a single image
        /// </summary>
        /// <param name="images"></param>
        /// <returns></returns>
        Image<Rgba32> Stitch(List<SatelliteImage> images);
    }

    public class ImageStitcher : IImageStitcher
    {
        public Image<Rgba32> Stitch(List<SatelliteImage> images)
        {
            // Identify minimum horizontal offset im source images
            var minOffset = images.Select(p => p.OffsetX).Min();
            var target = NewTargetImage(images, minOffset);

            Log.Information("Output image size: {width} x {height} px", target.Width, target.Height);

            // Composite all images. Images will have their horizontal offsets pre-calculated and overlaps
            // blended, so compositing just involves combining them in the correct stacking order.
            target.Mutate(context =>
            {
                // Render all images in correct stacking order
                foreach (var projection in images.OrderByDescending(p => p.OffsetX))
                {
                    // Identify horizontal offset of each image
                    var location = new Point(projection.OffsetX - minOffset, 0);
                    context.DrawImage(projection.Image, location, PixelColorBlendingMode.Normal, 1.0f);
                }
            });
            
            return target;
        }

        /// <summary>
        ///     Initialises the target image, calculating image size based on size of source images and
        ///     adjusting for image offsets.
        /// </summary>
        private static Image<Rgba32> NewTargetImage(IEnumerable<SatelliteImage> projections, int minOffset)
        {
            // As we know the horizontal offsets of all images being composed, the output width is the 
            // maximum offset plus the width of the final image, minus the minimum offset.
            var finalProjection = projections.OrderBy(p => p.OffsetX).Last();
            
            var outputWidth = finalProjection.OffsetX + finalProjection.Image!.Width - minOffset;
            return new Image<Rgba32>(outputWidth, finalProjection.Image!.Height);
        }
    }
}