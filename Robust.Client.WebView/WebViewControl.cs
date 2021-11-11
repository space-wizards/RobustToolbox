using System;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.WebView.Cef;
using Robust.Shared.IoC;
using Robust.Shared.ViewVariables;

namespace Robust.Client.WebView
{
    /// <summary>
    /// An UI control that presents web content.
    /// </summary>
    public sealed class WebViewControl : Control, IWebViewControl, IRawInputControl
    {
        [Dependency] private readonly IWebViewManagerInternal _webViewManager = default!;

        private readonly IWebViewControlImpl _controlImpl;

        [ViewVariables(VVAccess.ReadWrite)]
        public string Url
        {
            get => _controlImpl.Url;
            set => _controlImpl.Url = value;
        }

        [ViewVariables] public bool IsLoading => _controlImpl.IsLoading;

        public WebViewControl()
        {
            CanKeyboardFocus = true;
            KeyboardFocusOnClick = true;
            MouseFilter = MouseFilterMode.Stop;

            IoCManager.InjectDependencies(this);

            _controlImpl = _webViewManager.MakeControlImpl(this);
        }

        protected override void EnteredTree()
        {
            base.EnteredTree();

            _controlImpl.EnteredTree();
        }

        protected override void ExitedTree()
        {
            base.ExitedTree();

            _controlImpl.ExitedTree();
        }

        protected internal override void MouseMove(GUIMouseMoveEventArgs args)
        {
            base.MouseMove(args);

            _controlImpl.MouseMove(args);
        }

        protected internal override void MouseExited()
        {
            base.MouseExited();

            _controlImpl.MouseExited();
        }

        protected internal override void MouseWheel(GUIMouseWheelEventArgs args)
        {
            base.MouseWheel(args);

            _controlImpl.MouseWheel(args);
        }

        bool IRawInputControl.RawKeyEvent(in GuiRawKeyEvent guiRawEvent)
        {
            return _controlImpl.RawKeyEvent(guiRawEvent);
        }

        protected internal override void TextEntered(GUITextEventArgs args)
        {
            base.TextEntered(args);

            _controlImpl.TextEntered(args);
        }

        protected override void Resized()
        {
            base.Resized();

            _controlImpl.Resized();
        }

        protected internal override void Draw(DrawingHandleScreen handle)
        {
            base.Draw(handle);

            _controlImpl.Draw(handle);
        }

        public void StopLoad()
        {
            _controlImpl.StopLoad();
        }

        public void Reload()
        {
            _controlImpl.Reload();
        }

        public bool GoBack()
        {
            return _controlImpl.GoBack();
        }

        public bool GoForward()
        {
            return _controlImpl.GoForward();
        }

        public void ExecuteJavaScript(string code)
        {
            _controlImpl.ExecuteJavaScript(code);
        }

        public void AddResourceRequestHandler(Action<IRequestHandlerContext> handler)
        {
            _controlImpl.AddResourceRequestHandler(handler);
        }

        public void RemoveResourceRequestHandler(Action<IRequestHandlerContext> handler)
        {
            _controlImpl.RemoveResourceRequestHandler(handler);
        }

        public void AddBeforeBrowseHandler(Action<IBeforeBrowseContext> handler)
        {
            _controlImpl.AddBeforeBrowseHandler(handler);
        }

        public void RemoveBeforeBrowseHandler(Action<IBeforeBrowseContext> handler)
        {
            _controlImpl.RemoveBeforeBrowseHandler(handler);
        }
    }
}
