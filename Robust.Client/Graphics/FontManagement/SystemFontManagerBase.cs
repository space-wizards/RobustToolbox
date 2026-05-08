using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Threading;
using Robust.Shared.Log;

namespace Robust.Client.Graphics.FontManagement;

internal abstract class SystemFontManagerBase
{
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
    protected static readonly CultureInfo StandardLocale = new("en-US", false);

    protected readonly IFontManagerInternal FontManager;
    protected readonly ISawmill Sawmill;

    protected readonly Lock Lock = new();
    protected readonly List<BaseHandle> Fonts = [];

    public IEnumerable<ISystemFontFace> SystemFontFaces { get; }

    public SystemFontManagerBase(ILogManager logManager, IFontManagerInternal fontManager)
    {
        FontManager = fontManager;
        Sawmill = logManager.GetSawmill("font.system");

        SystemFontFaces = Fonts.AsReadOnly();
    }

    protected abstract IFontFaceHandle LoadFontFace(BaseHandle handle);

    protected static string GetLocalizedForLocaleOrFirst(LocalizedStringSet set, CultureInfo culture)
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

    protected abstract class BaseHandle(SystemFontManagerBase parent) : ISystemFontFace
    {
        private IFontFaceHandle? _cachedFont;

        public required string PostscriptName { get; init; }

        public required LocalizedStringSet FullNames;
        public required LocalizedStringSet FamilyNames;
        public required LocalizedStringSet FaceNames;

        public required FontWeight Weight { get; init; }
        public required FontSlant Slant { get; init; }
        public required FontWidth Width { get; init; }

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

        public Font Load(int size)
        {
            var handle = GetFaceHandle();

            var instance = parent.FontManager.MakeInstance(handle, size);

            return new VectorFont(instance, size);
        }

        private IFontFaceHandle GetFaceHandle()
        {
            lock (parent.Lock)
            {
                if (_cachedFont != null)
                    return _cachedFont;

                parent.Sawmill.Verbose($"Loading system font face: {PostscriptName}");

                return _cachedFont = parent.LoadFontFace(this);
            }
        }
    }

    protected struct LocalizedStringSet
    {
        public static readonly LocalizedStringSet Empty = FromSingle("");

        /// <summary>
        /// The first locale to appear in the list of localized strings.
        /// Used as fallback if the desired locale is not provided.
        /// </summary>
        public required string Primary;
        public required Dictionary<string, string> Values;

        public static LocalizedStringSet FromSingle(string value, string language = "en")
        {
            return new LocalizedStringSet
            {
                Primary = language,
                Values = new Dictionary<string, string> { { language, value } }
            };
        }
    }

    protected sealed class MemoryMappedFontMemoryHandle : IFontMemoryHandle
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

        public unsafe byte* GetData()
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
