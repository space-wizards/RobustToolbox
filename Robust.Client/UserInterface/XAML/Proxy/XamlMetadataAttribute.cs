namespace Robust.Client.UserInterface.XAML.Proxy
{
    // PYREX NOTE: This part of the hot reloading code sticks around regardless of whether we're doing a tools build
    // or a regular build.
    //
    // Of course, we don't use it on a regular build, but including this and other hot reloading-specific structures
    // means our content artifacts will support hot reloading even if compiled for release.
    //
    // (That is, only the client has to change.)
    public sealed class XamlMetadataAttribute: System.Attribute
    {
        public readonly string Uri;
        public readonly string FileName;
        public readonly string Content;

        public XamlMetadataAttribute(string uri, string fileName, string content)
        {
            Uri = uri;
            FileName = fileName;
            Content = content;
        }
    }

}
