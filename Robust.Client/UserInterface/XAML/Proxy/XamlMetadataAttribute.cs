using System;

namespace Robust.Client.UserInterface.XAML.Proxy;

/// <summary>
/// Metadata to support JIT compilation of XAML resources for a type.
/// </summary>
/// <remarks>
/// We can feed XamlX data from this type, along with new content, to get new XAML
/// resources.
///
/// This type is inert and is generated for release artifacts too, not just debug
/// artifacts. Released content should support hot reloading if loaded in a debug
/// client, but this is untested.
/// </remarks>
[AttributeUsage(validOn: AttributeTargets.Class, Inherited = false)]
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
