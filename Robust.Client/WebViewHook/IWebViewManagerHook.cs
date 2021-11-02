using System;

namespace Robust.Client.WebViewHook
{
    /// <summary>
    /// Used so that the IWebViewManager can be loaded when loading Robust.Client.WebView.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly)]
    internal sealed class WebViewManagerImplAttribute : Attribute
    {
        public readonly Type ImplementationType;

        public WebViewManagerImplAttribute(Type implementationType)
        {
            ImplementationType = implementationType;
        }
    }

    internal interface IWebViewManagerHook
    {
        void Initialize();
        void Update();
        void Shutdown();
    }
}
