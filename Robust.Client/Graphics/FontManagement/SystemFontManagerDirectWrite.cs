using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.IO.MemoryMappedFiles;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Log;
using Robust.Shared.Utility;
using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;
using static TerraFX.Interop.DirectX.DWRITE_FACTORY_TYPE;
using static TerraFX.Interop.DirectX.DWRITE_FONT_PROPERTY_ID;
using static TerraFX.Interop.Windows.Windows;

namespace Robust.Client.Graphics.FontManagement;

/// <summary>
/// Implementation of <see cref="ISystemFontManager"/> that uses DirectWrite on Windows.
/// </summary>
internal sealed unsafe class SystemFontManagerDirectWrite : ISystemFontManagerInternal
{
    // For future implementors of other platforms:
    // a significant amount of code in this file will be shareable with that of other platforms,
    // so some refactoring is warranted.

    /// <summary>
    /// The "standard" locale used when looking up the PostScript name of a font face.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Font files allow the PostScript name to be localized, however in practice
    /// we would really like to have a language-unambiguous identifier to refer to a font file.
    /// We use this locale (en-US) to look up teh PostScript font name, if there are multiple provided.
    /// This matches the behavior of the Local Font Access web API:
    /// https://wicg.github.io/local-font-access/#concept-font-representation
    /// </para>
    /// </remarks>
    private static readonly CultureInfo StandardLocale = new("en-US", false);

    private readonly IConfigurationManager _cfg;
    private readonly IFontManagerInternal _fontManager;
    private readonly ISawmill _sawmill;

    private readonly object _lock = new();
    private readonly List<Handle> _fonts = [];

    private IDWriteFactory3* _dWriteFactory;
    private IDWriteFontSet* _systemFontSet;

    public bool IsSupported => true;
    public IEnumerable<ISystemFontFace> SystemFontFaces { get; }

    /// <summary>
    /// Implementation of <see cref="ISystemFontManager"/> that uses DirectWrite on Windows.
    /// </summary>
    public SystemFontManagerDirectWrite(
        ILogManager logManager,
        IConfigurationManager cfg,
        IFontManagerInternal fontManager)
    {
        _cfg = cfg;
        _fontManager = fontManager;
        _sawmill = logManager.GetSawmill("font.system");

        SystemFontFaces = _fonts.AsReadOnly();
    }

    public void Initialize()
    {
        CreateDWriteFactory();

        _systemFontSet = GetSystemFontSet(_dWriteFactory);

        lock (_lock)
        {
            var fontCount = _systemFontSet->GetFontCount();
            for (var i = 0u; i < fontCount; i++)
            {
                LoadSingleFontFromSet(_systemFontSet, i);
            }
        }

        _sawmill.Verbose($"Loaded {_fonts.Count} fonts");
    }

    public void Shutdown()
    {
        _systemFontSet->Release();
        _systemFontSet = null;

        _dWriteFactory->Release();
        _dWriteFactory = null;

        lock (_lock)
        {
            foreach (var systemFont in _fonts)
            {
                systemFont.FontFace->Release();
            }

            _fonts.Clear();
        }
    }

    private void LoadSingleFontFromSet(IDWriteFontSet* set, uint fontIndex)
    {
        // Get basic parameters that every font should probably have?
        if (!TryGetStringsSet(set, fontIndex, DWRITE_FONT_PROPERTY_ID_POSTSCRIPT_NAME, out var postscriptNames))
            return;

        if (!TryGetStringsSet(set, fontIndex, DWRITE_FONT_PROPERTY_ID_FULL_NAME, out var fullNames))
            return;

        if (!TryGetStringsSet(set, fontIndex, DWRITE_FONT_PROPERTY_ID_FAMILY_NAME, out var familyNames))
            return;

        if (!TryGetStringsSet(set, fontIndex, DWRITE_FONT_PROPERTY_ID_FACE_NAME, out var faceNames))
            return;

        // I assume these parameters can't be missing in practice, but better safe than sorry.
        TryGetStrings(set, fontIndex, DWRITE_FONT_PROPERTY_ID_WEIGHT, out var weight);
        TryGetStrings(set, fontIndex, DWRITE_FONT_PROPERTY_ID_STYLE, out var style);
        TryGetStrings(set, fontIndex, DWRITE_FONT_PROPERTY_ID_STRETCH, out var stretch);

        var parsedWeight = ParseFontWeight(weight);
        var parsedSlant = ParseFontSlant(style);
        var parsedWidth = ParseFontWidth(stretch);

        IDWriteFontFaceReference* reference = null;
        var result = set->GetFontFaceReference(fontIndex, &reference);
        ThrowIfFailed(result);

        var handle = new Handle(this, reference)
        {
            PostscriptName = GetLocalizedForLocaleOrFirst(postscriptNames, StandardLocale),
            FullNames = fullNames,
            FamilyNames = familyNames,
            FaceNames = faceNames,
            Weight = parsedWeight,
            Slant = parsedSlant,
            Width = parsedWidth
        };

        _fonts.Add(handle);
    }

    private static FontWeight ParseFontWeight(DWriteLocalizedString[]? strings)
    {
        if (strings == null)
            return FontWeight.Regular;

        return (FontWeight)Parse.Int32(strings[0].Value);
    }

    private static FontSlant ParseFontSlant(DWriteLocalizedString[]? strings)
    {
        if (strings == null)
            return FontSlant.Normal;

        return (FontSlant)Parse.Int32(strings[0].Value);
    }

    private static FontWidth ParseFontWidth(DWriteLocalizedString[]? strings)
    {
        if (strings == null)
            return FontWidth.Normal;

        return (FontWidth)Parse.Int32(strings[0].Value);
    }

    private void CreateDWriteFactory()
    {
        fixed (IDWriteFactory3** pFactory = &_dWriteFactory)
        {
            var result = DirectX.DWriteCreateFactory(
                DWRITE_FACTORY_TYPE_SHARED,
                __uuidof<IDWriteFactory3>(),
                (IUnknown**)pFactory);

            ThrowIfFailed(result);
        }
    }

    private IDWriteFontSet* GetSystemFontSet(IDWriteFactory3* factory)
    {
        IDWriteFactory6* factory6;
        IDWriteFontSet* fontSet;
        var result = factory->QueryInterface(__uuidof<IDWriteFactory6>(), (void**)&factory6);
        if (result.SUCCEEDED)
        {
            _sawmill.Verbose("IDWriteFactory6 available, using newer GetSystemFontSet");

            result = factory6->GetSystemFontSet(
                _cfg.GetCVar(CVars.FontWindowsDownloadable),
                (IDWriteFontSet1**)(&fontSet));

            factory6->Release();
        }
        else
        {
            _sawmill.Verbose("IDWriteFactory6 not available");

            result = factory->GetSystemFontSet(&fontSet);
        }

        ThrowIfFailed(result, "GetSystemFontSet");
        return fontSet;
    }

    private IFontFaceHandle LoadFontFace(IDWriteFontFaceReference* fontFace)
    {
        IDWriteFontFile* file = null;
        IDWriteFontFileLoader* loader = null;

        try
        {
            var result = fontFace->GetFontFile(&file);
            ThrowIfFailed(result, "IDWriteFontFaceReference::GetFontFile");
            result = file->GetLoader(&loader);
            ThrowIfFailed(result, "IDWriteFontFile::GetLoader");

            void* referenceKey;
            uint referenceKeyLength;
            result = file->GetReferenceKey(&referenceKey, &referenceKeyLength);
            ThrowIfFailed(result, "IDWriteFontFile::GetReferenceKey");

            IDWriteLocalFontFileLoader* localLoader;
            result = loader->QueryInterface(__uuidof<IDWriteLocalFontFileLoader>(), (void**)&localLoader);
            if (result.SUCCEEDED)
            {
                _sawmill.Verbose("Loading font face via memory mapped file...");

                // We can get the local file path on disk. This means we can directly load it via mmap.
                uint filePathLength;
                ThrowIfFailed(
                    localLoader->GetFilePathLengthFromKey(referenceKey, referenceKeyLength, &filePathLength),
                    "IDWriteLocalFontFileLoader::GetFilePathLengthFromKey");
                var filePath = new char[filePathLength + 1];
                fixed (char* pFilePath = filePath)
                {
                    ThrowIfFailed(
                        localLoader->GetFilePathFromKey(
                            referenceKey,
                            referenceKeyLength,
                            pFilePath,
                            (uint)filePath.Length),
                        "IDWriteLocalFontFileLoader::GetFilePathFromKey");
                }

                var path = new string(filePath, 0, (int)filePathLength);

                localLoader->Release();

                return _fontManager.Load(new MemoryMappedFontMemoryHandle(path));
            }
            else
            {
                _sawmill.Verbose("Loading font face via stream...");

                // DirectWrite doesn't give us anything to go with for this file, read it into regular memory.
                // If the font file has multiple faces, which is possible, then this approach will duplicate memory.
                // That sucks, but I'm really not sure whether there's any way around this short of
                // comparing the memory contents by hashing to check equality.
                // As I'm pretty sure we can't like reference equality check the font objects somehow.
                IDWriteFontFileStream* stream;
                result = loader->CreateStreamFromKey(referenceKey, referenceKeyLength, &stream);
                ThrowIfFailed(result, "IDWriteFontFileLoader::CreateStreamFromKey");

                using var streamObject = new DirectWriteStream(stream);
                return _fontManager.Load(streamObject, (int)fontFace->GetFontFaceIndex());
            }
        }
        finally
        {
            if (file != null)
                file->Release();
            if (loader != null)
                loader->Release();
        }
    }

    private static bool TryGetStrings(
        IDWriteFontSet* set,
        uint listIndex,
        DWRITE_FONT_PROPERTY_ID property,
        [NotNullWhen(true)] out DWriteLocalizedString[]? strings)
    {
        BOOL exists;
        IDWriteLocalizedStrings* dWriteStrings = null;
        var result = set->GetPropertyValues(
            listIndex,
            property,
            &exists,
            &dWriteStrings);
        ThrowIfFailed(result, "IDWriteFontSet::GetPropertyValues");

        if (!exists)
        {
            strings = null;
            return false;
        }

        try
        {
            strings = GetStrings(dWriteStrings);
            return true;
        }
        finally
        {
            dWriteStrings->Release();
        }
    }

    private static bool TryGetStringsSet(
        IDWriteFontSet* set,
        uint listIndex,
        DWRITE_FONT_PROPERTY_ID property,
        out LocalizedStringSet strings)
    {
        if (!TryGetStrings(set, listIndex, property, out var stringsArray))
        {
            strings = default;
            return false;
        }

        strings = StringsToSet(stringsArray);
        return true;
    }

    private static DWriteLocalizedString[] GetStrings(IDWriteLocalizedStrings* localizedStrings)
    {
        IDWriteStringList* list;
        ThrowIfFailed(localizedStrings->QueryInterface(__uuidof<IDWriteStringList>(), (void**)&list));

        try
        {
            return GetStrings(list);
        }
        finally
        {
            list->Release();
        }
    }

    private static DWriteLocalizedString[] GetStrings(IDWriteStringList* stringList)
    {
        var array = new DWriteLocalizedString[stringList->GetCount()];

        for (var i = 0; i < array.Length; i++)
        {
            uint length;

            ThrowIfFailed(stringList->GetStringLength((uint)i, &length), "IDWriteStringList::GetStringLength");
            var arr = new char[length + 1];
            fixed (char* pArr = arr)
            {
                ThrowIfFailed(stringList->GetString((uint)i, pArr, (uint)arr.Length), "IDWriteStringList::GetString");
            }

            var value = new string(arr, 0, (int)length);

            ThrowIfFailed(stringList->GetLocaleNameLength((uint)i, &length), "IDWriteStringList::GetLocaleNameLength");
            arr = new char[length + 1];
            fixed (char* pArr = arr)
            {
                ThrowIfFailed(
                    stringList->GetLocaleName((uint)i, pArr, (uint)arr.Length),
                    "IDWriteStringList::GetLocaleName");
            }

            var localeName = new string(arr, 0, (int)length);

            array[i] = new DWriteLocalizedString(value, localeName);
        }

        return array;
    }

    private static LocalizedStringSet StringsToSet(DWriteLocalizedString[] strings)
    {
        var dict = new Dictionary<string, string>();

        foreach (var (value, localeName) in strings)
        {
            dict[localeName] = value;
        }

        return new LocalizedStringSet { Primary = strings[0].LocaleName, Values = dict };
    }

    private static string GetLocalizedForLocaleOrFirst(LocalizedStringSet set, CultureInfo culture)
    {
        var matchCulture = culture;
        while (!Equals(matchCulture, CultureInfo.InvariantCulture))
        {
            if (set.Values.TryGetValue(culture.Name, out var value))
                return value;

            matchCulture = matchCulture.Parent;
        }

        return set.Values[set.Primary];
    }

    private sealed class Handle(SystemFontManagerDirectWrite parent, IDWriteFontFaceReference* fontFace) : ISystemFontFace
    {
        private IFontFaceHandle? _cachedFont;

        public required LocalizedStringSet FullNames;
        public required LocalizedStringSet FamilyNames;
        public required LocalizedStringSet FaceNames;
        public readonly IDWriteFontFaceReference* FontFace = fontFace;

        public required string PostscriptName { get; init; }
        public string FullName => GetLocalizedFullName(CultureInfo.CurrentCulture);
        public string FamilyName => GetLocalizedFamilyName(CultureInfo.CurrentCulture);
        public string FaceName => GetLocalizedFaceName(CultureInfo.CurrentCulture);

        public string GetLocalizedFullName(CultureInfo culture)
        {
            return GetLocalizedForLocaleOrFirst(FullNames, culture);
        }

        public string GetLocalizedFamilyName(CultureInfo culture)
        {
            return GetLocalizedForLocaleOrFirst(FamilyNames, culture);
        }

        public string GetLocalizedFaceName(CultureInfo culture)
        {
            return GetLocalizedForLocaleOrFirst(FaceNames, culture);
        }

        public required FontWeight Weight { get; init; }
        public required FontSlant Slant { get; init; }
        public required FontWidth Width { get; init; }

        public Font Load(int size)
        {
            var handle = GetFaceHandle();

            var instance = parent._fontManager.MakeInstance(handle, size);

            return new VectorFont(instance, size);
        }

        private IFontFaceHandle GetFaceHandle()
        {
            lock (parent._lock)
            {
                if (_cachedFont != null)
                    return _cachedFont;

                parent._sawmill.Verbose($"Loading system font face: {PostscriptName}");

                return _cachedFont = parent.LoadFontFace(FontFace);
            }
        }
    }

    /// <summary>
    /// A simple implementation of a .NET Stream over a IDWriteFontFileStream.
    /// </summary>
    private sealed class DirectWriteStream : Stream
    {
        private readonly IDWriteFontFileStream* _stream;
        private readonly ulong _size;

        private ulong _position;
        private bool _disposed;

        public DirectWriteStream(IDWriteFontFileStream* stream)
        {
            _stream = stream;

            fixed (ulong* pSize = &_size)
            {
                var result = _stream->GetFileSize(pSize);
                ThrowIfFailed(result, "IDWriteFontFileStream::GetFileSize");
            }
        }

        public override void Flush()
        {
            throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return Read(buffer.AsSpan(offset, count));
        }

        public override int Read(Span<byte> buffer)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(DirectWriteStream));

            var readLength = (uint)buffer.Length;
            if (readLength + _position > _size)
                readLength = (uint)(_size - _position);

            void* fragmentStart;
            void* fragmentContext;

            var result = _stream->ReadFileFragment(&fragmentStart, _position, readLength, &fragmentContext);
            ThrowIfFailed(result);

            var data = new ReadOnlySpan<byte>(fragmentStart, (int)readLength);
            data.CopyTo(buffer);

            _stream->ReleaseFileFragment(fragmentContext);

            _position += readLength;
            return (int)readLength;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    Position = offset;
                    break;
                case SeekOrigin.Current:
                    Position += offset;
                    break;
                case SeekOrigin.End:
                    Position = Length + offset;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(origin), origin, null);
            }

            return Position;
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => false;
        public override long Length => (long)_size;

        public override long Position
        {
            get => (long)_position;
            set
            {
                ArgumentOutOfRangeException.ThrowIfNegative(value);
                ArgumentOutOfRangeException.ThrowIfGreaterThan((ulong)value, _size);

                _position = (ulong)value;
            }
        }

        protected override void Dispose(bool disposing)
        {
            _stream->Release();
            _disposed = true;
        }
    }

    private record struct DWriteLocalizedString(string Value, string LocaleName);

    private struct LocalizedStringSet
    {
        /// <summary>
        /// The first locale to appear in the list of localized strings.
        /// Used as fallback if the desired locale is not provided.
        /// </summary>
        public required string Primary;
        public required Dictionary<string, string> Values;
    }

    private sealed class MemoryMappedFontMemoryHandle : IFontMemoryHandle
    {
        private readonly MemoryMappedFile _mappedFile;
        private readonly MemoryMappedViewAccessor _accessor;

        public MemoryMappedFontMemoryHandle(string filePath)
        {
            _mappedFile = MemoryMappedFile.CreateFromFile(
                filePath,
                FileMode.Open,
                null,
                0,
                MemoryMappedFileAccess.Read);

            _accessor = _mappedFile.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);
        }

        public byte* GetData()
        {
            byte* pointer = null;
            _accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref pointer);
            return pointer;
        }

        public nint GetDataSize()
        {
            return (nint)_accessor.Capacity;
        }

        public void Dispose()
        {
            _accessor.Dispose();
            _mappedFile.Dispose();
        }
    }
}
