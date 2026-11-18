using ChangeAudioSource;
using Xunit;

namespace SoundShift.Tests;

public sealed class AudioDeviceServiceTests
{
    private static readonly AudioDeviceService.PlaybackDevice[] Devices =
    {
        new("first", "Speakers"),
        new("second", "Headphones"),
        new("third", "Speakers")
    };

    [Theory]
    [InlineData("first", "third")]
    [InlineData("third", "first")]
    [InlineData("second", "first")]
    [InlineData("unknown", "first")]
    [InlineData(null, "first")]
    public void GetNextPlaybackDevice_SkipsExcludedDevicesAndWraps(string? currentId, string expectedId)
    {
        var result = AudioDeviceService.GetNextPlaybackDevice(Devices, new[] { "SECOND" }, currentId);

        Assert.Equal(expectedId, result?.Id);
    }

    [Fact]
    public void GetNextPlaybackDevice_UsesAllDevicesByDefault()
    {
        var result = AudioDeviceService.GetNextPlaybackDevice(Devices, Array.Empty<string>(), "first");

        Assert.Equal("second", result?.Id);
    }

    [Fact]
    public void GetNextPlaybackDevice_ReturnsOnlyIncludedDevice()
    {
        var result = AudioDeviceService.GetNextPlaybackDevice(Devices, new[] { "first", "third" }, "second");

        Assert.Equal("second", result?.Id);
    }

    [Fact]
    public void GetNextPlaybackDevice_ReturnsNullWhenAllDevicesAreExcluded()
    {
        var result = AudioDeviceService.GetNextPlaybackDevice(Devices, new[] { "first", "second", "third" }, "first");

        Assert.Null(result);
    }

    [Fact]
    public void GetNextPlaybackDevice_ReturnsNullWhenNoDevicesAreAvailable()
    {
        var result = AudioDeviceService.GetNextPlaybackDevice(Array.Empty<AudioDeviceService.PlaybackDevice>(), Array.Empty<string>(), null);

        Assert.Null(result);
    }
}