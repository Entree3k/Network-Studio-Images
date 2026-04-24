using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.NetworkImages.Providers;

/// <summary>
/// Provides network/studio artwork from a remote repository.
/// Matches by TMDB provider ID first, then falls back to name matching.
/// </summary>
public class NetworkImageProvider : IRemoteImageProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<NetworkImageProvider> _logger;
    private readonly TimeSpan _cacheExpire = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Initializes a new instance of the <see cref="NetworkImageProvider"/> class.
    /// </summary>
    /// <param name="httpClientFactory">Instance of the <see cref="IHttpClientFactory"/> interface.</param>
    /// <param name="memoryCache">Instance of the <see cref="IMemoryCache"/> interface.</param>
    /// <param name="logger">Instance of the <see cref="ILogger{NetworkImageProvider}"/> interface.</param>
    public NetworkImageProvider(
        IHttpClientFactory httpClientFactory,
        IMemoryCache memoryCache,
        ILogger<NetworkImageProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _memoryCache = memoryCache;
        _logger = logger;
    }

    /// <inheritdoc />
    public string Name => "Network Image Provider";

    /// <inheritdoc />
    public bool Supports(BaseItem item) => item is Studio;

    /// <inheritdoc />
    public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
        => [ImageType.Primary, ImageType.Thumb];

    /// <inheritdoc />
    public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
    {
        var repoUrl = Plugin.Instance?.Configuration.RepositoryUrl;
        if (string.IsNullOrEmpty(repoUrl))
        {
            return [];
        }

        repoUrl = repoUrl.TrimEnd('/');
        var jsonUrl = repoUrl + "/studios.json";

        var studios = await GetStudios(jsonUrl).ConfigureAwait(false);
        var match = FindMatch(item, studios);

        if (match?.Artwork == null)
        {
            return [];
        }

        var imageInfos = new List<RemoteImageInfo>();

        // URL pattern: {repoUrl}/{machine-name}/{type}.{ext}
        var urlTemplate = repoUrl + "/{0}/{1}.{2}";

        foreach (var ext in match.Artwork.Primary)
        {
            imageInfos.Add(new RemoteImageInfo
            {
                Type = ImageType.Primary,
                Url = string.Format(CultureInfo.InvariantCulture, urlTemplate, match.MachineName, "primary", ext)
            });
        }

        foreach (var ext in match.Artwork.Thumb)
        {
            imageInfos.Add(new RemoteImageInfo
            {
                Type = ImageType.Thumb,
                Url = string.Format(CultureInfo.InvariantCulture, urlTemplate, match.MachineName, "thumb", ext)
            });
        }

        return imageInfos;
    }

    /// <inheritdoc />
    public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        => _httpClientFactory
            .CreateClient(NamedClient.Default)
            .GetAsync(new Uri(url), cancellationToken);

    private static StudioDto? FindMatch(BaseItem item, IReadOnlyList<StudioDto> studios)
    {
        // Primary: match by TMDB provider ID
        if (item.TryGetProviderId(MetadataProvider.Tmdb, out var tmdbId))
        {
            foreach (var studio in studios)
            {
                if (studio.Providers?.Tmdb != null
                    && string.Equals(tmdbId, studio.Providers.Tmdb, StringComparison.OrdinalIgnoreCase))
                {
                    return studio;
                }
            }
        }

        // Secondary: match by TVDB provider ID
        if (item.TryGetProviderId(MetadataProvider.Tvdb, out var tvdbId))
        {
            foreach (var studio in studios)
            {
                if (studio.Providers?.Tvdb != null
                    && string.Equals(tvdbId, studio.Providers.Tvdb, StringComparison.OrdinalIgnoreCase))
                {
                    return studio;
                }
            }
        }

        // Fallback: match by name
        foreach (var studio in studios)
        {
            if (string.Equals(item.Name, studio.Name, StringComparison.OrdinalIgnoreCase))
            {
                return studio;
            }
        }

        return null;
    }

    private async Task<IReadOnlyList<StudioDto>> GetStudios(string jsonUrl)
    {
        if (_memoryCache.TryGetValue(jsonUrl, out IReadOnlyList<StudioDto>? cached) && cached is not null)
        {
            return cached;
        }

        try
        {
            var studios = await _httpClientFactory
                .CreateClient(NamedClient.Default)
                .GetFromJsonAsync<IReadOnlyList<StudioDto>>(jsonUrl)
                .ConfigureAwait(false);

            if (studios != null)
            {
                _memoryCache.Set(jsonUrl, studios, _cacheExpire);
                return studios;
            }
        }
        catch (HttpRequestException e)
        {
            _logger.LogWarning(e, "Error downloading studio repository");
        }
        catch (JsonException e)
        {
            _logger.LogWarning(e, "Error deserializing studio repository");
        }

        return Array.Empty<StudioDto>();
    }
}
