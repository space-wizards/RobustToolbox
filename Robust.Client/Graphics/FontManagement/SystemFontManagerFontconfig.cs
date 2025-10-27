using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Robust.Shared.Log;
using SpaceWizards.Fontconfig.Interop;

namespace Robust.Client.Graphics.FontManagement;

internal sealed unsafe class SystemFontManagerFontconfig : SystemFontManagerBase, ISystemFontManagerInternal
{
    private static readonly (int Fc, FontWidth Width)[] WidthTable = [
        (Fontconfig.FC_WIDTH_ULTRACONDENSED, FontWidth.UltraCondensed),
        (Fontconfig.FC_WIDTH_EXTRACONDENSED, FontWidth.ExtraCondensed),
        (Fontconfig.FC_WIDTH_CONDENSED, FontWidth.Condensed),
        (Fontconfig.FC_WIDTH_SEMICONDENSED, FontWidth.SemiCondensed),
        (Fontconfig.FC_WIDTH_NORMAL, FontWidth.Normal),
        (Fontconfig.FC_WIDTH_SEMIEXPANDED, FontWidth.SemiExpanded),
        (Fontconfig.FC_WIDTH_EXPANDED, FontWidth.Expanded),
        (Fontconfig.FC_WIDTH_EXTRAEXPANDED, FontWidth.ExtraExpanded),
        (Fontconfig.FC_WIDTH_ULTRAEXPANDED, FontWidth.UltraExpanded),
    ];

    public bool IsSupported => true;

    public SystemFontManagerFontconfig(ILogManager logManager, IFontManagerInternal fontManager)
        : base(logManager, fontManager)
    {
    }

    public void Initialize()
    {
        Sawmill.Verbose("Initializing Fontconfig...");

        var result = Fontconfig.FcInit();
        if (result == Fontconfig.FcFalse)
            throw new InvalidOperationException("Failed to initialize fontconfig!");

        Sawmill.Verbose("Listing fonts...");

        var os = Fontconfig.FcObjectSetCreate();
        AddToObjectSet(os, Fontconfig.FC_FAMILY);
        AddToObjectSet(os, Fontconfig.FC_FAMILYLANG);
        AddToObjectSet(os, Fontconfig.FC_STYLE);
        AddToObjectSet(os, Fontconfig.FC_STYLELANG);
        AddToObjectSet(os, Fontconfig.FC_FULLNAME);
        AddToObjectSet(os, Fontconfig.FC_FULLNAMELANG);
        AddToObjectSet(os, Fontconfig.FC_POSTSCRIPT_NAME);

        AddToObjectSet(os, Fontconfig.FC_SLANT);
        AddToObjectSet(os, Fontconfig.FC_WEIGHT);
        AddToObjectSet(os, Fontconfig.FC_WIDTH);

        AddToObjectSet(os, Fontconfig.FC_FILE);
        AddToObjectSet(os, Fontconfig.FC_INDEX);

        var allPattern = Fontconfig.FcPatternCreate();
        var set = Fontconfig.FcFontList(null, allPattern, os);

        for (var i = 0; i < set->nfont; i++)
        {
            var pattern = set->fonts[i];

            try
            {
                LoadPattern(pattern);
            }
            catch (Exception e)
            {
                Sawmill.Error($"Error while loading pattern: {e}");
            }
        }

        Fontconfig.FcPatternDestroy(allPattern);
        Fontconfig.FcObjectSetDestroy(os);
        Fontconfig.FcFontSetDestroy(set);
    }

    public void Shutdown()
    {
        // Nada.
    }

    private void LoadPattern(FcPattern* pattern)
    {
        var path = PatternGetStrings(pattern, Fontconfig.FC_FILE)![0];
        var idx = PatternGetInts(pattern, Fontconfig.FC_INDEX)![0];

        var family = PatternToLocalized(pattern, Fontconfig.FC_FAMILY, Fontconfig.FC_FAMILYLANG);
        var style = PatternToLocalized(pattern, Fontconfig.FC_STYLE, Fontconfig.FC_STYLELANG);
        var fullName = PatternToLocalized(pattern, Fontconfig.FC_FULLNAME, Fontconfig.FC_FULLNAMELANG);
        var psName = PatternGetStrings(pattern, Fontconfig.FC_POSTSCRIPT_NAME);
        if (psName == null)
            return;

        var slant = PatternGetInts(pattern, Fontconfig.FC_SLANT) ?? [Fontconfig.FC_SLANT_ROMAN];
        var weight = PatternGetInts(pattern, Fontconfig.FC_WEIGHT) ?? [Fontconfig.FC_WEIGHT_REGULAR];
        var width = PatternGetInts(pattern, Fontconfig.FC_WIDTH) ?? [Fontconfig.FC_WIDTH_NORMAL];

        Fonts.Add(new Handle(this)
        {
            FilePath = path,
            FileIndex = idx,
            FaceNames = style ?? LocalizedStringSet.Empty,
            FullNames = fullName ?? LocalizedStringSet.Empty,
            FamilyNames = family ?? LocalizedStringSet.Empty,
            PostscriptName = psName[0],
            Slant = SlantFromFontconfig(slant[0]),
            Weight = WeightFromFontconfig(weight[0]),
            Width = WidthFromFontconfig(width[0])
        });
    }

    private static FontWeight WeightFromFontconfig(int value)
    {
        return (FontWeight)Fontconfig.FcWeightToOpenType(value);
    }

    private static FontSlant SlantFromFontconfig(int value)
    {
        return value switch
        {
            Fontconfig.FC_SLANT_ITALIC => FontSlant.Italic,
            Fontconfig.FC_SLANT_OBLIQUE => FontSlant.Italic,
            _ => FontSlant.Normal,
        };
    }

    private static FontWidth WidthFromFontconfig(int value)
    {
        return WidthTable.MinBy(t => Math.Abs(t.Fc - value)).Width;
    }

    private static unsafe void AddToObjectSet(FcObjectSet* os, ReadOnlySpan<byte> value)
    {
        var result = Fontconfig.FcObjectSetAdd(os, (sbyte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(value)));
        if (result == Fontconfig.FcFalse)
            throw new InvalidOperationException("Failed to add to object set!");
    }

    private static unsafe string[]? PatternGetStrings(FcPattern* pattern, ReadOnlySpan<byte> @object)
    {
        return PatternGetValues(pattern, @object, static (FcPattern* p, sbyte* o, int i, out string value) =>
        {
            byte* str = null;
            var res = Fontconfig.FcPatternGetString(p, o, i, &str);
            value = Marshal.PtrToStringUTF8((nint)str)!;
            return res;
        });
    }

    private static unsafe int[]? PatternGetInts(FcPattern* pattern, ReadOnlySpan<byte> @object)
    {
        return PatternGetValues(pattern, @object, static (FcPattern* p, sbyte* o, int i, out int value) =>
        {
            FcResult res;
            fixed (int* pValue = &value)
            {
                res = Fontconfig.FcPatternGetInteger(p, o, i, pValue);
            }
            return res;
        });
    }

    private delegate FcResult GetValue<T>(FcPattern* p, sbyte* o, int i, out T value);
    private static unsafe T[]? PatternGetValues<T>(FcPattern* pattern, ReadOnlySpan<byte> @object, GetValue<T> getValue)
    {
        var list = new List<T>();

        var i = 0;
        while (true)
        {
            var result = getValue(pattern, (sbyte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(@object)), i++, out var value);
            if (result == FcResult.FcResultMatch)
            {
                list.Add(value);
            }
            else if (result == FcResult.FcResultNoMatch)
            {
                return null;
            }
            else if (result == FcResult.FcResultNoId)
            {
                break;
            }
            else
            {
                throw new Exception($"FcPatternGetString gave error: {result}");
            }
        }

        return list.ToArray();
    }

    private static LocalizedStringSet? PatternToLocalized(FcPattern* pattern, ReadOnlySpan<byte> @object, ReadOnlySpan<byte> objectLang)
    {
        var values = PatternGetStrings(pattern, @object);
        var languages = PatternGetStrings(pattern, objectLang);

        if (values == null || languages == null || values.Length == 0 || languages.Length != values.Length)
            return null;

        var dict = new Dictionary<string, string>();

        for (var i = 0; i < values.Length; i++)
        {
            var val = values[i];
            var lang = languages[i];

            dict.TryAdd(lang, val);
        }

        return new LocalizedStringSet
        {
            Primary = languages[0],
            Values = dict
        };
    }

    protected override IFontFaceHandle LoadFontFace(BaseHandle handle)
    {
        var cast = (Handle)handle;

        return FontManager.Load(new MemoryMappedFontMemoryHandle(cast.FilePath), cast.FileIndex);
    }

    private sealed class Handle(SystemFontManagerFontconfig parent) : BaseHandle(parent)
    {
        public required string FilePath;
        public required int FileIndex;
    }
}
