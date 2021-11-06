using System;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;

namespace Robust.Client.WebView.Headless
{
    internal sealed class WebViewManagerHeadless : IWebViewManagerImpl
    {
        public IWebViewWindow CreateBrowserWindow(BrowserWindowCreateParameters createParams)
        {
            return new WebViewWindowDummy();
        }

        public IWebViewControlImpl MakeControlImpl(WebViewControl owner)
        {
            return new WebViewControlImplDummy();
        }

        public void Initialize()
        {
            // Nop
        }

        public void Update()
        {
            // Nop
        }

        public void Shutdown()
        {
            // Nop
        }

        private abstract class DummyBase : IWebViewControl
        {
            public string Url { get; set; } = "about:blank";
            public bool IsLoading => true;

            public void StopLoad()
            {
            }

            public void Reload()
            {
            }

            public bool GoBack()
            {
                return false;
            }

            public bool GoForward()
            {
                return false;
            }

            public void ExecuteJavaScript(string code)
            {
            }

            public void AddResourceRequestHandler(Action<IRequestHandlerContext> handler)
            {
            }

            public void RemoveResourceRequestHandler(Action<IRequestHandlerContext> handler)
            {
            }
        }

        private sealed class WebViewControlImplDummy : DummyBase, IWebViewControlImpl
        {
            public void EnteredTree()
            {
            }

            public void ExitedTree()
            {
            }

            public void MouseMove(GUIMouseMoveEventArgs args)
            {
            }

            public void MouseExited()
            {
            }

            public void MouseWheel(GUIMouseWheelEventArgs args)
            {
            }

            public bool RawKeyEvent(in GuiRawKeyEvent guiRawEvent)
            {
                return false;
            }

            public void TextEntered(GUITextEventArgs args)
            {
            }

            public void Resized()
            {
            }

            public void Draw(DrawingHandleScreen handle)
            {
            }

            public void AddBeforeBrowseHandler(Action<IBeforeBrowseContext> handler)
            {
            }

            public void RemoveBeforeBrowseHandler(Action<IBeforeBrowseContext> handler)
            {
            }
        }

        private sealed class WebViewWindowDummy : DummyBase, IWebViewWindow
        {
            public void Dispose()
            {
            }

            public bool Closed => false;
        }
    }
}
