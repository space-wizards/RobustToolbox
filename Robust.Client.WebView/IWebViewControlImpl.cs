using System;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;

namespace Robust.Client.WebView
{
    /// <summary>
    /// Internal swappable implementation of <see cref="WebViewControl"/>.
    /// </summary>
    internal interface IWebViewControlImpl : IWebViewControl
    {
        void EnteredTree();
        void ExitedTree();
        void MouseMove(GUIMouseMoveEventArgs args);
        void MouseExited();
        void MouseWheel(GUIMouseWheelEventArgs args);
        bool RawKeyEvent(in GuiRawKeyEvent guiRawEvent);
        void TextEntered(GUITextEventArgs args);
        void Resized();
        void Draw(DrawingHandleScreen handle);
        void AddBeforeBrowseHandler(Action<IBeforeBrowseContext> handler);
        void RemoveBeforeBrowseHandler(Action<IBeforeBrowseContext> handler);
    }
}
