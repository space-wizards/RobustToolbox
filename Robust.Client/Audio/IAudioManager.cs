using Robust.Shared.Audio.Sources;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Robust.Shared;

namespace Robust.Client.Audio;

/// <summary>
/// Public audio API for stuff that can't go through <see cref="AudioSystem"/>
/// </summary>
[NotContentImplementable]
public interface IAudioManager
{
    /// <summary>
    /// Provides list of audio devices available on the system. Those device names can be used to change device used by the game.
    /// </summary>
    /// <seealso cref="ConvertAudioDeviceNameForDisplay"/>
    /// <seealso cref="CVars.AudioDevice"/>
    IReadOnlyList<string> GetAudioDevices();

    string? GetDefaultAudioDevice();

    IAudioSource? CreateAudioSource(AudioStream stream);

    AudioStream LoadAudioOggVorbis(Stream stream, string? name = null);

    AudioStream LoadAudioWav(Stream stream, string? name = null);

    AudioStream LoadAudioRaw(ReadOnlySpan<short> samples, int channels, int sampleRate, string? name = null);

    void SetMasterGain(float gain);

    /// <summary>
    /// Helper method for decoding device names into unicode, provided by OpenAL (by <see cref="GetAudioDevices"/> method) for display.
    /// <b>Make sure to use converted names only for display and not for setting device, as it will break audio.</b>
    /// </summary>
    /// <remarks>
    /// OpenAL provides device names in some system encoding, as it seems,
    /// but it does not provide info, which encoding it used to dotnet gets UTF-8 string.
    /// </remarks>
    static string ConvertAudioDeviceNameForDisplay(string deviceName)
    {
        if (CultureInfo.InstalledUICulture.TextInfo.ANSICodePage == 65001)
            return deviceName;

        var enc = Encoding.GetEncoding(CultureInfo.InstalledUICulture.TextInfo.ANSICodePage);
        var rawBytes = enc.GetBytes(deviceName);
        return Encoding.UTF8.GetString(rawBytes);
    }
}
