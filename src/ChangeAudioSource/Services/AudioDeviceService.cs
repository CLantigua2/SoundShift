using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using NAudio.CoreAudioApi;

namespace ChangeAudioSource;

internal sealed class AudioDeviceService
{
    internal sealed record PlaybackDevice(string Id, string Name);

    internal List<PlaybackDevice> GetActivePlaybackDevices()
    {
        using MMDeviceEnumerator enumerator = new();
        return enumerator
            .EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)
            .Select(device => new PlaybackDevice(device.ID, device.FriendlyName))
            .ToList();
    }

    internal string? GetDefaultPlaybackDeviceId()
    {
        try
        {
            using MMDeviceEnumerator enumerator = new();
            using MMDevice device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            return device.ID;
        }
        catch
        {
            return null;
        }
    }

    internal void SetDefaultPlaybackDevice(string deviceId)
    {
        IPolicyConfig policyConfig = (IPolicyConfig)(object)new PolicyConfigClient();
        int consoleResult = policyConfig.SetDefaultEndpoint(deviceId, ERole.eConsole);
        int multimediaResult = policyConfig.SetDefaultEndpoint(deviceId, ERole.eMultimedia);
        int communicationsResult = policyConfig.SetDefaultEndpoint(deviceId, ERole.eCommunications);

        if (consoleResult != 0)
        {
            Marshal.ThrowExceptionForHR(consoleResult);
        }

        if (multimediaResult != 0)
        {
            Marshal.ThrowExceptionForHR(multimediaResult);
        }

        if (communicationsResult != 0)
        {
            Marshal.ThrowExceptionForHR(communicationsResult);
        }
    }

    private enum ERole
    {
        eConsole = 0,
        eMultimedia = 1,
        eCommunications = 2
    }

    [ComImport]
    [Guid("F8679F50-850A-41CF-9C72-430F290290C8")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IPolicyConfig
    {
        int GetMixFormat();
        int GetDeviceFormat();
        int ResetDeviceFormat();
        int SetDeviceFormat();
        int GetProcessingPeriod();
        int SetProcessingPeriod();
        int GetShareMode();
        int SetShareMode();
        int GetPropertyValue();
        int SetPropertyValue();
        int SetDefaultEndpoint([MarshalAs(UnmanagedType.LPWStr)] string deviceId, ERole role);
        int SetEndpointVisibility();
    }

    [ComImport]
    [Guid("870AF99C-171D-4F9E-AF0D-E63DF40C2BC9")]
    private sealed class PolicyConfigClient
    {
    }
}
