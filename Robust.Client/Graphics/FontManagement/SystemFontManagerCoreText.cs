#if MACOS
using System;
using System.Linq;
using System.Runtime.InteropServices;
using Robust.Client.Interop.MacOS;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using CF = Robust.Client.Interop.MacOS.CoreFoundation;
using CT = Robust.Client.Interop.MacOS.CoreText;

namespace Robust.Client.Graphics.FontManagement;

/// <summary>
/// Implementation of <see cref="ISystemFontManager"/> that uses CoreText on macOS.
/// </summary>
internal sealed class SystemFontManagerCoreText : SystemFontManagerBase, ISystemFontManagerInternal
{
    private static readonly FontWidth[] FontWidths = Enum.GetValues<FontWidth>();

    public bool IsSupported => true;

    public SystemFontManagerCoreText(ILogManager logManager, IFontManagerInternal fontManager) : base(logManager,
        fontManager)
    {
    }

    public unsafe void Initialize()
    {
        Sawmill.Verbose("Getting CTFontCollection...");

        var collection = CT.CTFontCollectionCreateFromAvailableFonts(null);
        var array = CT.CTFontCollectionCreateMatchingFontDescriptors(collection);

        var count = CF.CFArrayGetCount(array);
        Sawmill.Verbose($"Have {count} descriptors...");

        for (nint i = 0; i < count.Value; i++)
        {
            var item = (__CTFontDescriptor*)CF.CFRetain(CF.CFArrayGetValueAtIndex(array, new CLong(i)));

            try
            {
                LoadFontDescriptor(item);
            }
            catch (Exception ex)
            {
                Sawmill.Error($"Failed to load font descriptor: {ex}");
            }
            finally
            {
                CF.CFRelease(item);
            }
        }

        CF.CFRelease(array);
        CF.CFRelease(collection);
    }

    private unsafe void LoadFontDescriptor(__CTFontDescriptor* descriptor)
    {
        var displayName = GetFontAttributeManaged(descriptor, CT.kCTFontDisplayNameAttribute);
        var postscriptName = GetFontAttributeManaged(descriptor, CT.kCTFontNameAttribute);
        var familyName = GetFontAttributeManaged(descriptor, CT.kCTFontFamilyNameAttribute);
        var styleName = GetFontAttributeManaged(descriptor, CT.kCTFontStyleNameAttribute);

        var url = (__CFURL*)CT.CTFontDescriptorCopyAttribute(descriptor, CT.kCTFontURLAttribute);

        const int maxPath = 1024;
        var buf = stackalloc byte[maxPath];
        var result = CF.CFURLGetFileSystemRepresentation(url, 1, buf, new CLong(maxPath));
        if (result == 0)
            throw new Exception("CFURLGetFileSystemRepresentation failed!");

        // Sawmill.Verbose(CF.CFStringToManaged(CF.CFURLGetString(url)));

        CF.CFRelease(url);

        var traits = (__CFDictionary*)CT.CTFontDescriptorCopyAttribute(descriptor, CT.kCTFontTraitsAttribute);
        var (weight, slant, width) = ParseTraits(traits);

        CF.CFRelease(traits);

        var path = Marshal.PtrToStringUTF8((nint)buf)!;

        Fonts.Add(new Handle(this)
        {
            PostscriptName = postscriptName,
            FullNames = LocalizedStringSet.FromSingle(displayName),
            FamilyNames = LocalizedStringSet.FromSingle(familyName),
            FaceNames = LocalizedStringSet.FromSingle(styleName),
            Weight = weight,
            Slant = slant,
            Width = width,
            Path = path
        });
    }

    private static unsafe (FontWeight, FontSlant, FontWidth) ParseTraits(__CFDictionary* dictionary)
    {
        var weight = FontWeight.Normal;
        var slant = FontSlant.Normal;
        var width = FontWidth.Normal;

        var weightVal = (__CFNumber*)CF.CFDictionaryGetValue(dictionary, CT.kCTFontWeightTrait);
        if (weightVal != null)
            weight = ConvertWeight(weightVal);

        var slantVal = (__CFNumber*)CF.CFDictionaryGetValue(dictionary, CT.kCTFontSlantTrait);
        if (slantVal != null)
            slant = ConvertSlant(slantVal);

        var widthVal = (__CFNumber*)CF.CFDictionaryGetValue(dictionary, CT.kCTFontWidthTrait);
        if (widthVal != null)
            width = ConvertWidth(widthVal);

        return (weight, slant, width);
    }

    private static readonly (float, FontWeight)[] FontWeightTable =
    [
        ((float) AppKit.NSFontWeightUltraLight, FontWeight.UltraLight),
        ((float) AppKit.NSFontWeightThin, FontWeight.Thin),
        ((float) AppKit.NSFontWeightLight, FontWeight.Light),
        ((float) AppKit.NSFontWeightRegular, FontWeight.Regular),
        ((float) AppKit.NSFontWeightMedium, FontWeight.Medium),
        ((float) AppKit.NSFontWeightSemiBold, FontWeight.SemiBold),
        ((float) AppKit.NSFontWeightBold, FontWeight.Bold),
        ((float) AppKit.NSFontWeightHeavy, FontWeight.Heavy),
        ((float) AppKit.NSFontWeightBlack, FontWeight.Black)
    ];

    private static unsafe FontWeight ConvertWeight(__CFNumber* number)
    {
        float val;
        CF.CFNumberGetValue(number, new CLong(CF.kCFNumberFloat32Type), &val);

        var valCopy = val;
        return FontWeightTable.MinBy(tup => Math.Abs(tup.Item1 - valCopy)).Item2;
    }

    private static unsafe FontWidth ConvertWidth(__CFNumber* number)
    {
        float val;
        CF.CFNumberGetValue(number, new CLong(CF.kCFNumberFloat32Type), &val);

        // Normalize to 0-1 range
        val = (val + 1) / 2;
        var lerped = MathHelper.Lerp((float)FontWidths[0], (float)FontWidths[^1], val);
        return FontWidths.MinBy(x => Math.Abs((float)x - lerped));
    }

    private static unsafe FontSlant ConvertSlant(__CFNumber* number)
    {
        float val;
        CF.CFNumberGetValue(number, new CLong(CF.kCFNumberFloat32Type), &val);

        // Normalize to 0-1 range
        return val == 0 ? FontSlant.Normal : FontSlant.Italic;
    }

    private static unsafe string GetFontAttributeManaged(__CTFontDescriptor* descriptor, __CFString* key)
    {
        var str = (__CFString*)CT.CTFontDescriptorCopyAttribute(descriptor, key);

        try
        {
            return CF.CFStringToManaged(str);
        }
        finally
        {
            CF.CFRelease(str);
        }
    }

    public void Shutdown()
    {
        // Nothing to do.
    }

    protected override IFontFaceHandle LoadFontFace(BaseHandle handle)
    {
        var path = ((Handle)handle).Path;
        Sawmill.Verbose(path);

        // CTFontDescriptor does not seem to have any way to identify *which* index in the font file should be accessed.
        // So we have to just load every one until the postscript name matches.
        return FontManager.LoadWithPostscriptName(new MemoryMappedFontMemoryHandle(path), handle.PostscriptName);
    }

    private sealed class Handle(SystemFontManagerCoreText parent) : BaseHandle(parent)
    {
        public required string Path;
    }
}
#endif
