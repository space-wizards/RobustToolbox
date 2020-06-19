using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Robust.Client.UserInterface
{
    /// <summary>
    ///     Helper for opening <see cref="Uri"/>s on the user's machine.
    /// </summary>
    public interface IUriOpener
    {
        /// <summary>
        ///     Open a <see cref="Uri" /> in the user's web browser.
        /// </summary>
        /// <remarks>
        ///     The URI must be an absolute <c>http://</c> or <c>https://</c> URI.
        /// </remarks>
        /// <param name="uri">The uri to open.</param>
        /// <exception cref="ArgumentException">
        ///     Thrown if the URI is not absolute or for HTTP or HTTPS.
        /// </exception>
        void OpenUri(Uri uri);

        /// <summary>
        ///     Open a URI in the user's web browser.
        /// </summary>
        /// <remarks>
        ///     The URI must be an absolute <c>http://</c> or <c>https://</c> URI.
        /// </remarks>
        /// <param name="uriString">The uri to open.</param>
        /// <exception cref="ArgumentException">
        ///     Thrown if the URI is not absolute or for HTTP or HTTPS.
        /// </exception>
        void OpenUri(string uriString);
    }

    internal abstract class UriOpenerBase : IUriOpener
    {
        public void OpenUri(Uri uri)
        {
            if (!uri.IsAbsoluteUri)
            {
                throw new ArgumentException("URI must be absolute.");
            }

            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            {
                throw new ArgumentException("URI must be for HTTP or HTTPS.");
            }

            Task.Run(() => DoOpen(uri));
        }

        public void OpenUri(string uriString)
        {
            if (!Uri.IsWellFormedUriString(uriString, UriKind.Absolute))
            {
                throw new ArgumentException("URI must be well formed & absolute.");
            }

            var uri = new Uri(uriString, UriKind.Absolute);
            OpenUri(uri);
        }

        protected abstract void DoOpen(Uri uri);
    }

    internal sealed class UriOpener : UriOpenerBase
    {
        protected override void DoOpen(Uri uri)
        {
            Process.Start(new ProcessStartInfo(uri.ToString()) {UseShellExecute = true});
        }
    }

    internal sealed class UriOpenerDummy : UriOpenerBase
    {
        protected override void DoOpen(Uri uri)
        {
            // Literally nothing.
        }
    }
}
