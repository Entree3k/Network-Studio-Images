using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.NetworkImages;

/// <summary>
/// Available artwork types and their file extensions.
/// </summary>
public class StudioArtwork
{
    /// <summary>
    /// Gets or sets the primary image extensions.
    /// </summary>
    [JsonPropertyName("primary")]
    public IReadOnlyList<string> Primary { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the thumb image extensions.
    /// </summary>
    [JsonPropertyName("thumb")]
    public IReadOnlyList<string> Thumb { get; set; } = Array.Empty<string>();
}
