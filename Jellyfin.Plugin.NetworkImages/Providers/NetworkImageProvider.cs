using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
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
    private const string ProviderName = "Network Images";

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
    public string Name => ProviderName;

    /// <inheritdoc />
    public bool Supports(BaseItem item) => item is Studio;

    /// <inheritdoc />
    public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
        => [ImageType.Thumb];

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

        var studios = await GetStudios(jsonUrl, cancellationToken).ConfigureAwait(false);
        var match = FindMatch(item, studios);

        if (match?.Artwork == null)
        {
            return [];
        }

        var imageInfos = new List<RemoteImageInfo>();
        await AddExistingImages(imageInfos, repoUrl, match, ImageType.Thumb, match.Artwork.Thumb, cancellationToken)
            .ConfigureAwait(false);

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

    private async Task AddExistingImages(
        List<RemoteImageInfo> imageInfos,
        string repoUrl,
        StudioDto studio,
        ImageType imageType,
        IReadOnlyList<string> extensions,
        CancellationToken cancellationToken)
    {
        var seenExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var extension in extensions)
        {
            var normalizedExtension = NormalizeExtension(extension);
            if (normalizedExtension == null || !seenExtensions.Add(normalizedExtension))
            {
                continue;
            }

            var url = BuildImageUrl(repoUrl, studio.MachineName, imageType, normalizedExtension);
            if (!await ImageExists(url, cancellationToken).ConfigureAwait(false))
            {
                continue;
            }

            imageInfos.Add(new RemoteImageInfo
            {
                ProviderName = ProviderName,
                Type = imageType,
                Url = url,
                ThumbnailUrl = url
            });
        }
    }

    private async Task<bool> ImageExists(string url, CancellationToken cancellationToken)
    {
        var cacheKey = "NetworkImages:Exists:" + url;
        if (_memoryCache.TryGetValue(cacheKey, out bool cached))
        {
            return cached;
        }

        var exists = await ImageExistsUncached(url, cancellationToken).ConfigureAwait(false);
        _memoryCache.Set(cacheKey, exists, _cacheExpire);
        return exists;
    }

    private async Task<bool> ImageExistsUncached(string url, CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Head, new Uri(url));
            using var response = await _httpClientFactory
                .CreateClient(NamedClient.Default)
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);

            if (response.StatusCode is HttpStatusCode.MethodNotAllowed or HttpStatusCode.Forbidden)
            {
                return await ImageCanBeDownloaded(url, cancellationToken).ConfigureAwait(false);
            }

            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException e)
        {
            _logger.LogDebug(e, "Error checking image URL {Url}", url);
            return false;
        }
    }

    private async Task<bool> ImageCanBeDownloaded(string url, CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(url));
            using var response = await _httpClientFactory
                .CreateClient(NamedClient.Default)
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);

            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException e)
        {
            _logger.LogDebug(e, "Error checking image URL {Url}", url);
            return false;
        }
    }

    private static string BuildImageUrl(string repoUrl, string machineName, ImageType imageType, string extension)
    {
        var imageName = imageType == ImageType.Thumb ? "thumb" : "primary";
        return string.Format(
            CultureInfo.InvariantCulture,
            "{0}/{1}/{2}.{3}",
            repoUrl,
            Uri.EscapeDataString(machineName),
            imageName,
            Uri.EscapeDataString(extension));
    }

    private static string? NormalizeExtension(string? extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
        {
            return null;
        }

        var normalizedExtension = extension.Trim().TrimStart('.');
        return normalizedExtension.Length == 0 ? null : normalizedExtension;
    }

    private async Task<IReadOnlyList<StudioDto>> GetStudios(string jsonUrl, CancellationToken cancellationToken)
    {
        if (_memoryCache.TryGetValue(jsonUrl, out IReadOnlyList<StudioDto>? cached) && cached is not null)
        {
            return cached;
        }

        try
        {
            var studios = await _httpClientFactory
                .CreateClient(NamedClient.Default)
                .GetFromJsonAsync<IReadOnlyList<StudioDto>>(jsonUrl, cancellationToken)
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
