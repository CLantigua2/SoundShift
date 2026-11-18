using System.Net;
using System.Net.Http;
using System.Text.Json;
using ChangeAudioSource;
using Xunit;

namespace SoundShift.Tests;

public sealed class UpdateServiceTests
{
    [Theory]
    [InlineData("v1.2.3", true)]
    [InlineData("v1.2.3-beta", false)]
    [InlineData("1.2.3", false)]
    [InlineData("v1.2", false)]
    [InlineData("v1.2.3/other", false)]
    public void TryParseReleaseVersion_AcceptsOnlyStableTags(string tag, bool expected)
    {
        Assert.Equal(expected, UpdateService.TryParseReleaseVersion(tag, out _));
    }

    [Fact]
    public async Task CheckAsync_ReturnsNewerRelease()
    {
        using HttpClient client = CreateClient(ReleaseJson("v1.2.3"));
        UpdateService service = new(client);

        var update = await service.CheckAsync(new Version(1, 2, 2), CancellationToken.None);

        Assert.NotNull(update);
        Assert.Equal(new Version(1, 2, 3), update.Version);
        Assert.Equal("https://github.com/CLantigua2/SoundShift/releases/download/v1.2.3/SoundShift-Setup.exe", update.DownloadUri.ToString());
    }

    [Theory]
    [InlineData(1, 2, 3)]
    [InlineData(2, 0, 0)]
    public async Task CheckAsync_IgnoresSameOrOlderRelease(int major, int minor, int build)
    {
        using HttpClient client = CreateClient(ReleaseJson("v1.2.3"));
        UpdateService service = new(client);

        Assert.Null(await service.CheckAsync(new Version(major, minor, build), CancellationToken.None));
    }

    [Theory]
    [InlineData("v1.2.3-beta", false, false, "https://github.com/CLantigua2/SoundShift/releases/download/v1.2.3-beta/SoundShift-Setup.exe")]
    [InlineData("v1.2.3", true, false, "https://github.com/CLantigua2/SoundShift/releases/download/v1.2.3/SoundShift-Setup.exe")]
    [InlineData("v1.2.3", false, true, "https://github.com/CLantigua2/SoundShift/releases/download/v1.2.3/SoundShift-Setup.exe")]
    [InlineData("v1.2.3", false, false, "https://example.com/SoundShift-Setup.exe")]
    public async Task CheckAsync_RejectsInvalidReleases(string tag, bool draft, bool prerelease, string url)
    {
        using HttpClient client = CreateClient(ReleaseJson(tag, draft, prerelease, url));
        UpdateService service = new(client);

        await Assert.ThrowsAsync<InvalidDataException>(() => service.CheckAsync(new Version(1, 0, 0), CancellationToken.None));
    }

    [Fact]
    public async Task DownloadAsync_WritesInstallerToTemporaryFile()
    {
        using HttpClient client = CreateClient("installer content");
        UpdateService service = new(client);
        var update = new UpdateService.UpdateInfo(new Version(1, 2, 3), new Uri("https://example.com/installer"));

        string path = await service.DownloadAsync(update, CancellationToken.None);
        try
        {
            Assert.Equal("installer content", await File.ReadAllTextAsync(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task DownloadAsync_RejectsEmptyDownloads()
    {
        using HttpClient client = CreateClient("");
        UpdateService service = new(client);
        var update = new UpdateService.UpdateInfo(new Version(1, 2, 3), new Uri("https://example.com/installer"));

        await Assert.ThrowsAsync<InvalidDataException>(() => service.DownloadAsync(update, CancellationToken.None));
    }

    private static string ReleaseJson(string tag, bool draft = false, bool prerelease = false, string? url = null)
    {
        return JsonSerializer.Serialize(new
        {
            tag_name = tag,
            draft,
            prerelease,
            assets = new[]
            {
                new
                {
                    name = "SoundShift-Setup.exe",
                    browser_download_url = url ?? $"https://github.com/CLantigua2/SoundShift/releases/download/{tag}/SoundShift-Setup.exe"
                }
            }
        });
    }

    private static HttpClient CreateClient(string content) => new(new TestHandler(content));

    private sealed class TestHandler(string content) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(content) });
        }
    }
}