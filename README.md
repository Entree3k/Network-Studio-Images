# Jellyfin Network Images Plugin

A Jellyfin plugin that provides network/studio artwork from a configurable GitHub repository.

## Features

- Provides Primary and Thumb images for TV network studios
- Matches studios by TMDB provider ID (primary), TVDB provider ID (secondary), or studio name (fallback)
- Configurable repository URL with a default artwork repository included
- 5-minute in-memory cache for repository data

## Installation

1. Open your Jellyfin Dashboard
2. Navigate to **Plugins** > **Repositories**
3. Add a new repository:
   - **Name:** `Network Images`
   - **URL:** `https://raw.githubusercontent.com/Entree3k/Jellyfin/main/manifest.json`
4. Go to **Catalog** and install **Network Images**
5. Restart Jellyfin

## Configuration

After installation:

1. Go to **Plugins** in the Dashboard
2. Click the three dot menu on **Network Images** and select **Settings**
3. The **Repository URL** defaults to the included artwork repository. You can change it to any compatible repository.

## Repository Format

The plugin expects a `studios.json` file at `{RepositoryUrl}/studios.json` with the following format:

```json
[
  {
    "name": "Netflix",
    "machine-name": "Netflix",
    "providers": {
      "tmdb": "213"
    },
    "artwork": {
      "primary": ["webp", "jpg"],
      "thumb": ["webp", "jpg"]
    }
  }
]
```

Images are served from: `{RepositoryUrl}/studios/{machine-name}/{type}.{extension}`

For example: `.../studios/Netflix/thumb.webp`

## Building from Source

```
dotnet build Jellyfin.Plugin.NetworkImages.sln
```

The compiled DLL will be at `Jellyfin.Plugin.NetworkImages/bin/Debug/net9.0/Jellyfin.Plugin.NetworkImages.dll`.

## License

Licensed under the GPLv3. See [LICENSE](LICENSE) for details.
