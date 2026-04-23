using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.NetworkImages;

/// <summary>
/// Provider IDs for a studio.
/// </summary>
public class StudioProviders
{
    /// <summary>
    /// Gets or sets the TMDB ID.
    /// </summary>
    [JsonPropertyName("tmdb")]
    public string? Tmdb { get; set; }

    /// <summary>
    /// Gets or sets the TVDB ID.
    /// </summary>
    [JsonPropertyName("tvdb")]
    public string? Tvdb { get; set; }

    /// <summary>
    /// Gets or sets the IMDB ID.
    /// </summary>
    [JsonPropertyName("imdb")]
    public string? Imdb { get; set; }
}
