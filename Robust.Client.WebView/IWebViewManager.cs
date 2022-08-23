using System.Diagnostics.CodeAnalysis;

namespace Robust.Client.WebView
{
    public interface IWebViewManager
    {
        IWebViewWindow CreateBrowserWindow(BrowserWindowCreateParameters createParams);

        /// <summary>
        /// Overrides file extension -> mime type mappings for the <c>res://</c> protocol.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The built-in <c>res://</c> protocol needs to guess MIME types to report to CEF when resolving files.
        /// A limited set of extensions have pre-set MIME types in the engine.
        /// This method allows you to replace or add entries if need be.
        /// </para>
        /// <para>
        /// This method is thread safe.
        /// </para>
        /// </remarks>
        /// <param name="extension">
        /// The extension to specify the MIME type for.
        /// The argument must not include the starting "." of the file extension.
        /// </param>
        /// <param name="mimeType">The mime type for this file extension.</param>
        /// <seealso cref="TryGetResourceMimeType"/>
        void SetResourceMimeType(string extension, string mimeType);

        /// <summary>
        /// Tries to resolve an entry from the <see cref="SetResourceMimeType"/> list.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method is thread safe.
        /// </para>
        /// </remarks>
        bool TryGetResourceMimeType(string extension, [NotNullWhen(true)] out string? mimeType);
    }
}
