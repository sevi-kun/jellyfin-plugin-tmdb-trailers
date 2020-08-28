﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Channels;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Tmdb.Trailers.Channels
{
    /// <summary>
    /// Trailers Channel.
    /// </summary>
    public class TmdbTrailerChannel : IChannel, IDisableMediaSourceDisplay, IDisposable, IScheduledTask, ISupportsLatestMedia
    {
        private readonly ILogger<TmdbTrailerChannel> _logger;
        private readonly TmdbManager _tmdbManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="TmdbTrailerChannel"/> class.
        /// </summary>
        /// <param name="loggerFactory">Instance of the <see cref="ILoggerFactory"/> interface.</param>
        /// <param name="memoryCache">Instance of the <see cref="IMemoryCache"/> interface.</param>
        public TmdbTrailerChannel(ILoggerFactory loggerFactory, IMemoryCache memoryCache)
        {
            _logger = loggerFactory.CreateLogger<TmdbTrailerChannel>();
            _logger.LogDebug(nameof(TmdbTrailerChannel));
            _tmdbManager = new TmdbManager(loggerFactory, memoryCache);
        }

        /// <summary>
        /// Gets the channel name.
        /// </summary>
        public string Name => "TMDb Trailers";

        /// <inheritdoc />
        public string Key => "TMDb Trailer Refresh";

        /// <summary>
        /// Gets the channel description.
        /// </summary>
        public string Description => TmdbTrailerPlugin.Instance.Description;

        /// <inheritdoc />
        public string Category => "Trailers";

        /// <inheritdoc />
        public string DataVersion => Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);

        /// <inheritdoc />
        public string HomePageUrl => "https://jellyfin.org";

        /// <inheritdoc />
        public ChannelParentalRating ParentalRating => ChannelParentalRating.GeneralAudience;

        /// <inheritdoc />
        public InternalChannelFeatures GetChannelFeatures()
        {
            _logger.LogDebug(nameof(GetChannelFeatures));
            return new InternalChannelFeatures
            {
                ContentTypes = new List<ChannelMediaContentType>
                {
                    ChannelMediaContentType.MovieExtra
                },
                MediaTypes = new List<ChannelMediaType>
                {
                    ChannelMediaType.Video
                },
                AutoRefreshLevels = 4
            };
        }

        /// <inheritdoc />
        public bool IsEnabledFor(string userId)
        {
            return TmdbTrailerPlugin.Instance.Configuration.EnableTrailersChannel;
        }

        /// <inheritdoc />
        public Task<ChannelItemResult> GetChannelItems(InternalChannelItemQuery query, CancellationToken cancellationToken)
        {
            return _tmdbManager.GetAllChannelItems(false, cancellationToken);
        }

        /// <inheritdoc />
        public Task<DynamicImageResponse> GetChannelImage(ImageType type, CancellationToken cancellationToken)
        {
            return _tmdbManager.GetChannelImage(type);
        }

        /// <inheritdoc />
        public IEnumerable<ImageType> GetSupportedChannelImages()
        {
            return _tmdbManager.GetSupportedChannelImages();
        }

        /// <inheritdoc />
        public async Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            if (TmdbTrailerPlugin.Instance.Configuration.EnableTrailersChannel)
            {
                await _tmdbManager.GetAllChannelItems(true, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            yield return new TaskTriggerInfo
            {
                Type = TaskTriggerInfo.TriggerStartup
            };

            yield return new TaskTriggerInfo
            {
                Type = TaskTriggerInfo.TriggerDaily,
                TimeOfDayTicks = TimeSpan.FromHours(4).Ticks
            };
        }

        /// <inheritdoc />
        public Task<IEnumerable<ChannelItemInfo>> GetLatestMedia(ChannelLatestMediaSearch request, CancellationToken cancellationToken)
        {
            // Implementing ISupportsLatestMedia is currently the only way to "automatically" refresh the library.
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose.
        /// </summary>
        /// <param name="disposing">Dispose everything.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _tmdbManager?.Dispose();
            }
        }
    }
}