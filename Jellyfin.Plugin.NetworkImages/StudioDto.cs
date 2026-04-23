using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.NetworkImages;

/// <summary>
/// A studio entry from studios.json.
/// </summary>
public class StudioDto
{
    /// <summary>
    /// Gets or sets the studio name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the machine name (used for folder/URL path).
    /// </summary>
    [JsonPropertyName("machine-name")]
    public string MachineName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the provider IDs.
    /// </summary>
    [JsonPropertyName("providers")]
    public StudioProviders? Providers { get; set; }

    /// <summary>
    /// Gets or sets the available artwork.
    /// </summary>
    [JsonPropertyName("artwork")]
    public StudioArtwork? Artwork { get; set; }
}
