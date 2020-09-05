﻿using System.Threading.Tasks;
using Funhouse.Models.Projections;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Funhouse.Services
{
    public interface IImageLoader
    {
        Task<SatelliteImage> LoadAsync(string path);
    }

    public class ImageLoader : IImageLoader
    {
        private readonly ISatelliteRegistry _satelliteRegistry;

        public ImageLoader(ISatelliteRegistry satelliteRegistry) => _satelliteRegistry = satelliteRegistry;

        // TODO rename this class/method  - suggests we are just loading any old image
        public async Task<SatelliteImage> LoadAsync(string path)
        {
            var activity = new SatelliteImage(path)
            {
                Definition = _satelliteRegistry.Locate(path),
                Image = await Image.LoadAsync<Rgba32>(path)
            };

            return activity;
        }
    }
}