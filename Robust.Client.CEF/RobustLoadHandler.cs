using Xilium.CefGlue;

namespace Robust.Client.CEF
{
    public sealed class RobustLoadHandler : CefLoadHandler
    {
        protected override void OnLoadStart(CefBrowser browser, CefFrame frame, CefTransitionType transitionType)
        {
            base.OnLoadStart(browser, frame, transitionType);
        }

        protected override void OnLoadEnd(CefBrowser browser, CefFrame frame, int httpStatusCode)
        {
            base.OnLoadEnd(browser, frame, httpStatusCode);
        }
    }
}
