using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.NetworkImages.Configuration;

/// <summary>
/// Plugin configuration.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginConfiguration"/> class.
    /// </summary>
    public PluginConfiguration()
    {
        RepositoryUrl = "https://raw.githubusercontent.com/Entree3k/Jellyfin/main/studios";
    }

    /// <summary>
    /// Gets or sets the repository URL for network artwork.
    /// </summary>
    public string RepositoryUrl { get; set; }
}
