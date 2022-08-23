using System;

namespace Robust.Client.WebView
{
    public interface IWebViewWindow : IWebViewControl, IDisposable
    {
        bool Closed { get; }
    }
}
