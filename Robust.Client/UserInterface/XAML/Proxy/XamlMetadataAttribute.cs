namespace Robust.Client.UserInterface.XAML.Proxy
{

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
