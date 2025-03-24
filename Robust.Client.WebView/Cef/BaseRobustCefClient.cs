using Xilium.CefGlue;

namespace Robust.Client.WebView.Cef;

/// <summary>
/// Shared functionality for all <see cref="CefClient"/> implementations.
/// </summary>
/// <remarks>
/// Locks down a bunch of CEF functionality we absolutely do not need content to do right now.
/// </remarks>
internal abstract class BaseRobustCefClient : CefClient
{
    private readonly RobustCefPrintHandler _printHandler = new();
    private readonly RobustCefPermissionHandler _permissionHandler = new();
    private readonly RobustCefDialogHandler _dialogHandler = new();
    private readonly RobustCefDragHandler _dragHandler = new();

    protected override CefPrintHandler GetPrintHandler() => _printHandler;
    protected override CefPermissionHandler GetPermissionHandler() => _permissionHandler;
    protected override CefDialogHandler GetDialogHandler() => _dialogHandler;
    protected override CefDragHandler GetDragHandler() => _dragHandler;

    private sealed class RobustCefPrintHandler : CefPrintHandler
    {
        protected override void OnPrintSettings(CefBrowser browser, CefPrintSettings settings, bool getDefaults)
        {
        }

        protected override bool OnPrintDialog(CefBrowser browser, bool hasSelection, CefPrintDialogCallback callback)
        {
            return false;
        }

        protected override bool OnPrintJob(CefBrowser browser, string documentName, string pdfFilePath, CefPrintJobCallback callback)
        {
            return false;
        }

        protected override void OnPrintReset(CefBrowser browser)
        {
        }
    }

    private sealed class RobustCefPermissionHandler : CefPermissionHandler
    {
        protected override bool OnRequestMediaAccessPermission(
            CefBrowser browser,
            CefFrame frame,
            string requestingOrigin,
            CefMediaAccessPermissionTypes requestedPermissions,
            CefMediaAccessCallback callback)
        {
            callback.Cancel();

            return true;
        }
    }

    private sealed class RobustCefDialogHandler : CefDialogHandler
    {
        protected override bool OnFileDialog(
            CefBrowser browser,
            CefFileDialogMode mode,
            string title,
            string defaultFilePath,
            string[] acceptFilters,
            string[] acceptExtensions,
            string[] acceptDescriptions,
            CefFileDialogCallback callback)
        {
            callback.Cancel();
            return true;
        }
    }

    private sealed class RobustCefDragHandler : CefDragHandler
    {
        protected override bool OnDragEnter(CefBrowser browser, CefDragData dragData, CefDragOperationsMask mask)
        {
            return true;
        }

        protected override void OnDraggableRegionsChanged(CefBrowser browser, CefFrame frame, CefDraggableRegion[] regions)
        {
        }
    }
}
