namespace Robust.Client.WebView
{
    public interface IBeforeBrowseContext
    {
        string Url { get; }
        string Method { get; }
        bool IsRedirect { get; }
        bool UserGesture { get; }
        bool IsCancelled { get; }
        void DoCancel();
    }
}
