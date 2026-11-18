using System.Net.Http;
using System.Net.Http.Json;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace ChangeAudioSource;

internal sealed class UpdateService
{
    private const string ReleasesApi = "https://api.github.com/repos/CLantigua2/SoundShift/releases/latest";
    private const string InstallerName = "SoundShift-Setup.exe";
    private readonly HttpClient client;

    internal sealed record UpdateInfo(Version Version, Uri DownloadUri);

    internal UpdateService(HttpClient client)
    {
        this.client = client;
    }

    internal async Task<UpdateInfo?> CheckAsync(Version currentVersion, CancellationToken cancellationToken)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, ReleasesApi);
        request.Headers.UserAgent.ParseAdd("SoundShift-UpdateChecker");
        using HttpResponseMessage response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        Release? release = await response.Content.ReadFromJsonAsync<Release>(cancellationToken: cancellationToken);
        if (release is null || release.Draft || release.Prerelease || !TryParseReleaseVersion(release.TagName, out Version? version))
        {
            throw new InvalidDataException("The latest release has an invalid version.");
        }

        if (version <= currentVersion)
        {
            return null;
        }

        string expectedUrl = $"https://github.com/CLantigua2/SoundShift/releases/download/{release.TagName}/{InstallerName}";
        Asset? asset = release.Assets?.FirstOrDefault(item => item.Name == InstallerName && item.DownloadUrl == expectedUrl);
        if (asset is null)
        {
            throw new InvalidDataException("The latest release does not contain the expected installer.");
        }

        return new UpdateInfo(version, new Uri(expectedUrl));
    }

    internal static bool TryParseReleaseVersion(string? tag, [NotNullWhen(true)] out Version? version)
    {
        version = null;
        if (tag is null || !System.Text.RegularExpressions.Regex.IsMatch(tag, @"^v[0-9]+\.[0-9]+\.[0-9]+$"))
        {
            return false;
        }

        return Version.TryParse(tag[1..], out version);
    }

    internal async Task<string> DownloadAsync(UpdateInfo update, CancellationToken cancellationToken)
    {
        const long maximumSize = 200L * 1024 * 1024;
        string path = Path.Combine(Path.GetTempPath(), $"SoundShift-Setup-{Guid.NewGuid():N}.exe");
        try
        {
            using HttpResponseMessage response = await client.GetAsync(update.DownloadUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();
            if (response.Content.Headers.ContentLength > maximumSize)
            {
                throw new InvalidDataException("The installer is too large.");
            }

            await using Stream input = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using FileStream output = new(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true);
            byte[] buffer = new byte[81920];
            long total = 0;
            int count;
            while ((count = await input.ReadAsync(buffer, cancellationToken)) > 0)
            {
                total += count;
                if (total > maximumSize)
                {
                    throw new InvalidDataException("The installer is too large.");
                }

                await output.WriteAsync(buffer.AsMemory(0, count), cancellationToken);
            }

            if (total == 0)
            {
                throw new InvalidDataException("The installer is empty.");
            }

            return path;
        }
        catch
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            throw;
        }
    }

    private sealed class Release
    {
        [JsonPropertyName("tag_name")]
        public string? TagName { get; set; }
        [JsonPropertyName("draft")]
        public bool Draft { get; set; }
        [JsonPropertyName("prerelease")]
        public bool Prerelease { get; set; }
        [JsonPropertyName("assets")]
        public List<Asset>? Assets { get; set; }
    }

    private sealed class Asset
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }
        [JsonPropertyName("browser_download_url")]
        public string? DownloadUrl { get; set; }
    }
}