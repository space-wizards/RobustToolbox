#if MACOS
using System.Runtime.InteropServices;
using CFIndex = System.Runtime.InteropServices.CLong;
using Boolean = byte;

namespace Robust.Client.Interop.MacOS;

// ReSharper disable InconsistentNaming

/// <summary>
/// Binding to macOS Core Foundation.
/// </summary>
internal static unsafe class CoreFoundation
{
    private const string CoreFoundationLibrary = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";

    public const int kCFNumberFloat32Type = 5;

    public static string CFStringToManaged(__CFString* str)
    {
        var length = CFStringGetLength(str);

        return string.Create(
            checked((int)length.Value),
            (nint)str,
            static (span, arg) =>
            {
                fixed (char* pBuffer = span)
                {
                    CFStringGetCharacters((__CFString*)arg,
                        new CFRange
                        {
                            location = new CFIndex(0),
                            length = new CFIndex(span.Length),
                        },
                        pBuffer);
                }
            });
    }

    [DllImport(CoreFoundationLibrary)]
    internal static extern void* CFRetain(void* cf);

    [DllImport(CoreFoundationLibrary)]
    internal static extern void CFRelease(void* cf);

    [DllImport(CoreFoundationLibrary)]
    internal static extern CFIndex CFArrayGetCount(__CFArray* array);

    [DllImport(CoreFoundationLibrary)]
    internal static extern void* CFArrayGetValueAtIndex(__CFArray* array, CFIndex index);

    [DllImport(CoreFoundationLibrary)]
    internal static extern CFIndex CFStringGetLength(__CFString* str);

    [DllImport(CoreFoundationLibrary)]
    internal static extern void CFStringGetCharacters(__CFString* str, CFRange range, char* buffer);

    [DllImport(CoreFoundationLibrary)]
    internal static extern Boolean CFURLGetFileSystemRepresentation(
        __CFURL* url,
        Boolean resolveAgainstBase,
        byte* buffer,
        CFIndex maxBufLen);

    [DllImport(CoreFoundationLibrary)]
    internal static extern __CFString* CFURLGetString(__CFURL* url);

    [DllImport(CoreFoundationLibrary)]
    internal static extern CFIndex CFDictionaryGetCount(__CFDictionary* theDict);

    [DllImport(CoreFoundationLibrary)]
    internal static extern void* CFDictionaryGetValue(__CFDictionary* theDict, void* key);

    [DllImport(CoreFoundationLibrary)]
    internal static extern void CFDictionaryGetKeysAndValues(__CFDictionary* theDict, void** keys, void** values);

    [DllImport(CoreFoundationLibrary)]
    internal static extern void CFNumberGetValue(__CFNumber* number, CLong theType, void* valuePtr);
}

internal struct __CFNumber;

internal struct __CFString;

internal struct __CFURL;

internal struct __CFArray;

internal struct __CFDictionary;

internal struct CFRange
{
    public CFIndex location;
    public CFIndex length;
}
#endif
