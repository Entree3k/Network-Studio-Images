# Network Studio Images Plugin

A Jellyfin plugin that provides Network artwork from a configurable GitHub repository.

## Features

- Provides Primary and Thumb images for TV network studios
- Matches studios by TMDB provider ID (primary), TVDB provider ID (secondary), or studio name (fallback)
- Configurable repository URL with a default artwork repository included

## Installation

1. Open your Jellyfin Dashboard
2. Navigate to **Plugins** > **Repositories**
3. Add a new repository:
   - **Name:** `Network Images`
   - **URL:** `https://raw.githubusercontent.com/Entree3k/Network-Studio-Images/refs/heads/main/manifest.json`
4. Go to **Catalog** and install **Network Images**
5. Restart Jellyfin

## Configuration

After installation:

1. Go to **Plugins** in the Dashboard
2. Click the three dot menu on **Network Images** and select **Settings**
3. The **Repository URL** is my own image repo at https://github.com/Entree3k/Jellyfin

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
