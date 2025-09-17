using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using NFluidsynth;
using Robust.Shared.ContentPack;
using Robust.Shared.Utility;

namespace Robust.Client.Audio.Midi;

internal sealed partial class MidiManager
{
    // For loading sound fonts, we have to use a callback model where we can only parse a string.
    // This API, frankly, fucking sucks.
    //
    // These prefixes are used to separate the various places a file *can* be loaded from.
    //
    // We cannot prevent Fluidsynth from trying to load prefixed paths itself if they are invalid
    // So if content specifies "/foobar.sf2" to be loaded and it doesn't exist,
    // Fluidsynth *will* try to fopen("RES:/foobar.sf2"). For this reason I'm putting in some nonsense characters
    // that will pass through Fluidsynth fine, but make sure the filename is *never* a practically valid OS path.
    //
    // NOTE: Raw disk paths *cannot* be prefixed as Fluidsynth needs to load those itself.
    // Specifically, their .dls loader doesn't respect file callbacks.
    // If you're curious why this is: it's two-fold:
    // * The Fluidsynth C code for the .dls loader just doesn't use the file callbacks, period.
    // * Even if it did, we're not specifying those file callbacks, as they're per loader,
    //   and we're only adding a *new* sound font loader with file callbacks, not modifying the existing ones.
    //   The loader for .sfX format and .dls format are different loader objects in Fluidsynth.
    internal const string PrefixCommon = "!/ -?\x0001";
    internal const string PrefixLegacy = PrefixCommon + "LEGACY";
    internal const string PrefixUser = PrefixCommon + "USER";
    internal const string PrefixResources = PrefixCommon + "RES";

    private void LoadSoundFontSetup(MidiRenderer renderer)
    {
        _midiSawmill.Debug($"Loading fallback soundfont {FallbackSoundfont}");
        // Since the last loaded soundfont takes priority, we load the fallback soundfont before the soundfont.
        renderer.LoadSoundfontResource(FallbackSoundfont);

        // Load system-specific soundfonts.
        if (OperatingSystem.IsLinux())
        {
            foreach (var filepath in LinuxSoundfonts)
            {
                if (!File.Exists(filepath) || !SoundFont.IsSoundFont(filepath))
                    continue;

                try
                {
                    _midiSawmill.Debug($"Loading OS soundfont {filepath}");
                    renderer.LoadSoundfontDisk(filepath);
                }
                catch (Exception)
                {
                    continue;
                }

                break;
            }
        }
        else if (OperatingSystem.IsMacOS())
        {
            if (File.Exists(OsxSoundfont) && SoundFont.IsSoundFont(OsxSoundfont))
            {
                _midiSawmill.Debug($"Loading OS soundfont {OsxSoundfont}");
                renderer.LoadSoundfontDisk(OsxSoundfont);
            }
        }
        else if (OperatingSystem.IsWindows())
        {
            if (File.Exists(WindowsSoundfont) && SoundFont.IsSoundFont(WindowsSoundfont))
            {
                _midiSawmill.Debug($"Loading OS soundfont {WindowsSoundfont}");
                renderer.LoadSoundfontDisk(WindowsSoundfont);
            }
        }

        // Maybe load soundfont specified in environment variable.
        // Load it here so it can override system soundfonts but not content or user data soundfonts.
        if (Environment.GetEnvironmentVariable(SoundfontEnvironmentVariable) is { } soundfontOverride)
        {
            // Just to avoid funny shit: avoid people smuggling a prefix in here.
            // I wish I could separate this properly...
            var (prefix, _) = SplitPrefix(soundfontOverride);
            if (IsValidPrefix(prefix))
            {
                _midiSawmill.Error($"Not respecting {SoundfontEnvironmentVariable} env variable: invalid file path");
            }
            else if (File.Exists(soundfontOverride) && SoundFont.IsSoundFont(soundfontOverride))
            {
                _midiSawmill.Debug($"Loading environment variable soundfont {soundfontOverride}");
                renderer.LoadSoundfontDisk(soundfontOverride);
            }
        }

        // Load content-specific custom soundfonts, which should override the system/fallback soundfont.
        _midiSawmill.Debug($"Loading soundfonts from content directory {ContentCustomSoundfontDirectory}");
        foreach (var file in _resourceManager.ContentFindFiles(ContentCustomSoundfontDirectory))
        {
            if (file.Extension != "sf2" && file.Extension != "dls" && file.Extension != "sf3") continue;
            _midiSawmill.Debug($"Loading content soundfont {file}");
            renderer.LoadSoundfontResource(file);
        }

        // Load every soundfont from the user data directory last, since those may override any other soundfont.
        _midiSawmill.Debug($"Loading soundfonts from user data directory {CustomSoundfontDirectory}");
        var enumerator = _resourceManager.UserData.Find($"{CustomSoundfontDirectory.ToRelativePath()}*").Item1;
        foreach (var file in enumerator)
        {
            if (file.Extension != "sf2" && file.Extension != "dls" && file.Extension != "sf3") continue;
            _midiSawmill.Debug($"Loading user soundfont {file}");
            renderer.LoadSoundfontUser(file);
        }
    }

    internal static string PrefixPath(string prefix, string value)
    {
        return $"{prefix}:{value}";
    }

    internal static (string prefix, string? value) SplitPrefix(string filename)
    {
        var filenameSplit = filename.Split(':', 2);
        if (filenameSplit.Length == 1)
            return (filenameSplit[0], null);

        return (filenameSplit[0], filenameSplit[1]);
    }

    internal static bool IsValidPrefix(string prefix)
    {
        return prefix is PrefixLegacy or PrefixUser or PrefixResources;
    }

    /// <summary>
    ///     This class is used to load soundfonts.
    /// </summary>
    private sealed class ResourceLoaderCallbacks : SoundFontLoaderCallbacks
    {
        private readonly MidiManager _parent;
        private readonly Dictionary<int, Stream> _openStreams = new();
        private int _nextStreamId = 1;

        public ResourceLoaderCallbacks(MidiManager parent)
        {
            _parent = parent;
        }

        public override IntPtr Open(string filename)
        {
            if (string.IsNullOrEmpty(filename))
            {
                return IntPtr.Zero;
            }

            Stream stream;
            try
            {
                stream = OpenCore(filename);
            }
            catch (Exception e)
            {
                _parent._midiSawmill.Error($"Error while opening sound font: {e}");
                return IntPtr.Zero;
            }

            var id = _nextStreamId++;

            _openStreams.Add(id, stream);

            return (IntPtr) id;
        }

        private Stream OpenCore(string filename)
        {
            var (prefix, value) = SplitPrefix(filename);

            if (!IsValidPrefix(prefix) || value == null)
                return File.OpenRead(filename);

            var resourceCache = _parent._resourceManager;
            var resourcePath = new ResPath(value);

            switch (prefix)
            {
                case PrefixUser:
                    return resourceCache.UserData.OpenRead(resourcePath);
                case PrefixResources:
                    return resourceCache.ContentFileRead(resourcePath);
                case PrefixLegacy:
                    // Try resources first, then try user data.
                    if (resourceCache.TryContentFileRead(resourcePath, out var stream))
                        return stream;

                    return resourceCache.UserData.OpenRead(resourcePath);
                default:
                    throw new UnreachableException("Invalid prefix specified!");
            }
        }

        public override unsafe int Read(IntPtr buf, long count, IntPtr sfHandle)
        {
            var length = (int) count;
            var span = new Span<byte>(buf.ToPointer(), length);
            var stream = _openStreams[(int) sfHandle];

            // Fluidsynth's docs state that this method should leave the buffer unmodified if it fails. (returns -1)
            try
            {
                // Fluidsynth does a LOT of tiny allocations (frankly, way too much).
                if (count < 1024)
                {
                    // ReSharper disable once SuggestVarOrType_Elsewhere
                    Span<byte> buffer = stackalloc byte[(int)count];

                    stream.ReadExact(buffer);

                    buffer.CopyTo(span);
                }
                else
                {
                    var buffer = stream.ReadExact(length);

                    buffer.CopyTo(span);
                }
            }
            catch (EndOfStreamException)
            {
                return -1;
            }

            return 0;
        }

        public override int Seek(IntPtr sfHandle, long offset, SeekOrigin origin)
        {
            var stream = _openStreams[(int) sfHandle];

            stream.Seek(offset, origin);

            return 0;
        }

        public override long Tell(IntPtr sfHandle)
        {
            var stream = _openStreams[(int) sfHandle];

            return (long) stream.Position;
        }

        public override int Close(IntPtr sfHandle)
        {
            if (!_openStreams.Remove((int) sfHandle, out var stream))
                return -1;

            stream.Dispose();
            return 0;

        }
    }
}
