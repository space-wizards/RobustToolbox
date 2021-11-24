using System;
using Robust.Client.WebView.Cef;

namespace Robust.Client.WebView
{
    public interface IWebViewControl
    {
        /// <summary>
        ///     Current URL of the browser. Set to load a new page.
        /// </summary>
        string Url { get; set; }

        /// <summary>
        ///     Whether the browser is currently loading a page.
        /// </summary>
        bool IsLoading { get; }

        /// <summary>
        ///     Stops loading the current page.
        /// </summary>
        void StopLoad();

        /// <summary>
        ///     Reload the current page.
        /// </summary>
        void Reload();

        /// <summary>
        ///     Navigate back.
        /// </summary>
        /// <returns>Whether the browser could navigate back.</returns>
        bool GoBack();

        /// <summary>
        ///     Navigate forward.
        /// </summary>
        /// <returns>Whether the browser could navigate forward.</returns>
        bool GoForward();

        /// <summary>
        ///     Execute arbitrary JavaScript on the current page.
        /// </summary>
        /// <param name="code">JavaScript code.</param>
        void ExecuteJavaScript(string code);

        void AddResourceRequestHandler(Action<IRequestHandlerContext> handler);
        void RemoveResourceRequestHandler(Action<IRequestHandlerContext> handler);
    }
}
