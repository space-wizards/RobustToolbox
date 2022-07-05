using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Utility;

namespace Robust.Client.WebView.Cef;

internal sealed partial class WebViewManagerCef
{
    // Loosely based on:
    // https://developer.mozilla.org/en-US/docs/Web/HTTP/Basics_of_HTTP/MIME_types/Common_types
    private readonly Dictionary<string, string> _resourceMimeTypes = new()
    {
        { "aac", "audio/aac" },
        { "avif", "image/avif" },
        { "avi", "video/x-msvideo" },
        { "bmp", "image/bmp" },
        { "css", "text/css" },
        { "gif", "image/gif" },
        { "htm", "text/html" },
        { "html", "text/html" },
        { "ico", "image/vnd.microsoft.icon" },
        { "jpeg", "image/jpeg" },
        { "jpg", "image/jpeg" },
        { "js", "text/javascript" },
        { "json", "application/json" },
        { "jsonld", "application/ld+json" },
        { "midi", "audio/midi" },
        { "mid", "audio/midi" },
        { "mjs", "text/javascript" },
        { "mp3", "audio/mpeg" },
        { "mp4", "video/mp4" },
        { "mpeg", "video/mpeg" },
        { "oga", "audio/ogg" },
        { "ogg", "audio/ogg" },
        { "ogv", "video/ogg" },
        { "ogx", "application/ogg" },
        { "opus", "audio/opus" },
        { "otf", "font/otf" },
        { "png", "image/png" },
        { "pdf", "application/pdf" },
        { "svg", "image/svg+xml" },
        { "tiff", "image/tiff" },
        { "tif", "image/tiff" },
        { "ts", "video/mp2t" },
        { "ttf", "font/ttf" },
        { "txt", "text/plain" },
        { "wav", "audio/wav" },
        { "weba", "audio/webm" },
        { "webm", "video/webm" },
        { "webp", "image/webp" },
        { "woff", "font/woff" },
        { "woff2", "font/woff2" },
        { "xhtml", "application/xhtml+xml" },
        { "xml", "application/xml" },
        { "zip", "application/zip" },
    };

    public void SetResourceMimeType(string extension, string mimeType)
    {
        DebugTools.Assert(!extension.StartsWith("."), "SetResourceMimeType extension must not include starting dot.");

        lock (_resourceMimeTypes)
        {
            _resourceMimeTypes[extension] = mimeType;
        }
    }

    public bool TryGetResourceMimeType(string extension, [NotNullWhen(true)] out string? mimeType)
    {
        lock (_resourceMimeTypes)
        {
            return _resourceMimeTypes.TryGetValue(extension, out mimeType);
        }
    }
}
