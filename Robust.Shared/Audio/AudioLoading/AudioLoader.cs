using System;
using System.IO;

namespace Robust.Shared.Audio.AudioLoading;

/// <summary>
/// Implements functionality for loading audio files.
/// </summary>
/// <seealso cref="AudioLoaderOgg"/>
/// <seealso cref="AudioLoaderWav"/>
internal static class AudioLoader
{
    /// <summary>
    /// Test if the given file name is something that we can load.
    /// </summary>
    /// <remarks>
    /// This is detected based on file extension.
    /// </remarks>
    public static bool IsLoadableAudioFile(ReadOnlySpan<char> filename)
    {
        var extension = Path.GetExtension(filename);
        return extension is ".wav" or ".ogg";
    }

    /// <summary>
    /// Load metadata about an audio file. Can handle all supported audio file types.
    /// </summary>
    /// <param name="stream">Stream containing audio file data to load.</param>
    /// <param name="filename">File name of the audio file. Used to detect which file type it is.</param>
    public static AudioMetadata LoadAudioMetadata(Stream stream, ReadOnlySpan<char> filename)
    {
        var extension = Path.GetExtension(filename);
        if (extension is ".ogg")
        {
            return AudioLoaderOgg.LoadAudioMetadata(stream);
        }
        else if (extension is ".wav")
        {
            return AudioLoaderWav.LoadAudioMetadata(stream);
        }
        else
        {
            throw new ArgumentException($"Unknown file type: {extension}");
        }
    }
}

/// <summary>
/// Contains basic metadata of an audio file.
/// </summary>
/// <seealso cref="AudioLoader"/>
internal record AudioMetadata(TimeSpan Length, int ChannelCount, string? Title = null, string? Artist = null);
