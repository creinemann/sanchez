﻿using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Funhouse.Extensions;
using Funhouse.ImageProcessing.ShadeEdges;
using Funhouse.ImageProcessing.Underlay;
using Funhouse.Models;
using Funhouse.Models.Angles;
using Funhouse.Models.CommandLine;
using Funhouse.Models.Configuration;
using Funhouse.Services.Equirectangular;
using Funhouse.Services.Underlay;
using Serilog;
using SixLabors.ImageSharp;
using Angle = Funhouse.Models.Angle;
using Range = Funhouse.Models.Angles.Range;

namespace Funhouse.Services
{
    public interface ICompositor
    {
        Task ComposeAsync();
    }

    public class Compositor : ICompositor
    {
        private readonly CommandLineOptions _commandLineOptions;
        private readonly RenderOptions _renderOptions;
        private readonly IImageLoader _imageLoader;
        private readonly IEquirectangularImageRenderer _equirectangularImageRenderer;
        private readonly IProjectionActivityOperations _activityOperations;

        public Compositor(
            CommandLineOptions commandLineOptions,
            RenderOptions renderOptions,
            IImageLoader imageLoader,
            IEquirectangularImageRenderer equirectangularImageRenderer,
            IProjectionActivityOperations activityOperations)
        {
            _commandLineOptions = commandLineOptions;
            _renderOptions = renderOptions;
            _imageLoader = imageLoader;
            _equirectangularImageRenderer = equirectangularImageRenderer;
            _activityOperations = activityOperations;
        }

        public async Task ComposeAsync()
        {
            // TODO TEMP
            string[] paths =
            {
                @"Resources\SampleImages\GOES16_FD_CH13_20200830T035020Z.jpg",
                @"Resources\SampleImages\Himawari8_FD_IR_20200830T035100Z.jpg",
                @"Resources\SampleImages\GOES17_FD_CH13_20200830T033031Z.jpg",
            };

            // Load source images
            Log.Information("Loading source images");

            var satelliteImageLoadTasks = paths.Select(path => _imageLoader.LoadAsync(path)).ToList();
            await Task.WhenAll(satelliteImageLoadTasks);

            Log.Information("Images loaded");

            var activities = satelliteImageLoadTasks.Select(task => task.Result).ToList();
            _activityOperations.Initialise(activities);

            // Verify that all images have an associated projection definition
            var unmappedProjections = _activityOperations.GetUnmapped();
            if (unmappedProjections.Any())
            {
                unmappedProjections.ForEach(p => Console.Error.WriteLineAsync($"Unable to determine satellite based on file prefix: {p.Path}"));
                Environment.Exit(-1);
            }

            // Calculate crop for each image based on visible range and image overlaps
            Log.Information("Processing IR image");

            _activityOperations
                .CalculateOverlap()
                .CropBorders()
                .RemoveBackground()
                .NormaliseSize()
                .NormaliseHistogram();


            // TODO this needs beefing up. Should be able to stitch and then reproject,e tc.
            switch (_renderOptions.ProjectionType)
            {
                case ProjectionType.Geostationary:
                    await _activityOperations.RenderGeostationaryUnderlayAsync();
                    break;
                case ProjectionType.Equirectangular:
                {
                    // Reproject all images to equirectangular
                    _activityOperations.ReprojectToEquirectangular();

                    // Stitch images if required
                    if (_commandLineOptions.Stitch)
                    {
                        // Combine reprojected images
                        var target = await _equirectangularImageRenderer.StitchImagesAsync(activities);

                        Log.Information("Reprojecting to geostationary");

                        // TODO we should prevent this mode if autocrop is enabled
                        // (or have autocrop ONLY for equirectangular output)

                        // Determine visible range of all satellite imagery
                        _activityOperations.GetVisibleRange(out _, out var longitudeRange);
                        var targetLongitude = Angle.FromDegrees(174.2).Radians;

                        var adjustedLongitude = -Math.PI - longitudeRange.Start + targetLongitude;

                        var definition = new SatelliteDefinition("", "", adjustedLongitude,
                            new Range(
                                Angle.FromDegrees(Constants.Satellite.VisibleRange.DefaultMinLatitude),
                                Angle.FromDegrees(Constants.Satellite.VisibleRange.DefaultMaxLatitude)),
                            new Range());


                        // TEMP
                        var arse = target.ToGeostationaryProjection(definition, _renderOptions);
                        if (_renderOptions.HazeAmount > 0) arse.ApplyHaze(_renderOptions.Tint, _renderOptions.HazeAmount);

                        await arse.SaveAsync("reprojected.jpg");

                        // Save output
                        Log.Information("Saving stitched output");
                        await target.SaveAsync(_commandLineOptions.OutputPath);

                        if (_commandLineOptions.Verbose)
                        {
                            Log.Information("Output written to {path}", Path.GetFullPath(_commandLineOptions.OutputPath));
                        }
                        else
                        {
                            Console.WriteLine($"Output written to {Path.GetFullPath(_commandLineOptions.OutputPath)}");
                        }
                    }

                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException($"Unhandled projection type: {_renderOptions.ProjectionType}");
            }
        }
    }
}