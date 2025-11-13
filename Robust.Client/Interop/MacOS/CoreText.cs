#if MACOS
using System.Runtime.InteropServices;

namespace Robust.Client.Interop.MacOS;

// ReSharper disable InconsistentNaming

/// <summary>
/// Binding to macOS Core Text.
/// </summary>
internal static unsafe class CoreText
{
    private const string CoreTextLibrary = "/System/Library/Frameworks/CoreText.framework/CoreText";

    public static readonly __CFString* kCTFontURLAttribute;
    public static readonly __CFString* kCTFontNameAttribute;
    public static readonly __CFString* kCTFontDisplayNameAttribute;
    public static readonly __CFString* kCTFontFamilyNameAttribute;
    public static readonly __CFString* kCTFontStyleNameAttribute;
    public static readonly __CFString* kCTFontTraitsAttribute;
    public static readonly __CFString* kCTFontWeightTrait;
    public static readonly __CFString* kCTFontWidthTrait;
    public static readonly __CFString* kCTFontSlantTrait;

    static CoreText()
    {
        var lib = NativeLibrary.Load(CoreTextLibrary);
        kCTFontURLAttribute = *(__CFString**)NativeLibrary.GetExport(lib, nameof(kCTFontURLAttribute));
        kCTFontNameAttribute = *(__CFString**)NativeLibrary.GetExport(lib, nameof(kCTFontNameAttribute));
        kCTFontDisplayNameAttribute = *(__CFString**)NativeLibrary.GetExport(lib, nameof(kCTFontDisplayNameAttribute));
        kCTFontFamilyNameAttribute = *(__CFString**)NativeLibrary.GetExport(lib, nameof(kCTFontFamilyNameAttribute));
        kCTFontStyleNameAttribute = *(__CFString**)NativeLibrary.GetExport(lib, nameof(kCTFontStyleNameAttribute));
        kCTFontTraitsAttribute = *(__CFString**)NativeLibrary.GetExport(lib, nameof(kCTFontTraitsAttribute));
        kCTFontWeightTrait = *(__CFString**)NativeLibrary.GetExport(lib, nameof(kCTFontWeightTrait));
        kCTFontWidthTrait = *(__CFString**)NativeLibrary.GetExport(lib, nameof(kCTFontWidthTrait));
        kCTFontSlantTrait = *(__CFString**)NativeLibrary.GetExport(lib, nameof(kCTFontSlantTrait));
    }

    [DllImport(CoreTextLibrary)]
    public static extern __CTFontCollection* CTFontCollectionCreateFromAvailableFonts(__CFDictionary* options);

    [DllImport(CoreTextLibrary)]
    public static extern __CFArray* CTFontCollectionCreateMatchingFontDescriptors(__CTFontCollection* collection);

    [DllImport(CoreTextLibrary)]
    public static extern void* CTFontDescriptorCopyAttribute(__CTFontDescriptor* descriptor, __CFString* attribute);

    [DllImport(CoreTextLibrary)]
    public static extern __CFDictionary* CTFontDescriptorCopyAttributes(__CTFontDescriptor* descriptor);
}

internal struct __CTFontCollection;
internal struct __CTFontDescriptor;
#endif
