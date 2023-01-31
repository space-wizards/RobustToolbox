using System.Collections.Generic;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.IoC;
using Robust.Shared.Utility;

namespace Robust.Client.UserInterface.RichText;

/// <summary>
/// Applies the font provided as the tags parameter to the markup drawing context.
/// Definitely not save for user supplied markup
/// </summary>
public sealed class FontTag : IMarkupTag
{
    [Dependency] private readonly IResourceCache _resourceCache = default!;

    public string Name => "font";

    public void PushDrawContext(MarkupNode node, MarkupDrawingContext context)
    {
        string fontPath = node.Value.StringValue ?? "/Fonts/NotoSans/NotoSans.ttf";
        var font = CreateFont(context.Font, node, _resourceCache, fontPath);
        context.Font.Push(font);
    }

    public void PopDrawContext(MarkupNode node, MarkupDrawingContext context)
    {
        context.Font.Pop();
    }

    /// <summary>
    /// Creates the a vector font from the supplied resource path.<br/>
    /// The size of the resulting font will be either the size supplied as a parameter to the tag, the previous font size or 12
    /// </summary>
    public static Font CreateFont(Stack<Font> contextFontStack, MarkupNode node, IResourceCache cache, string resourcePath)
    {
        var size = 12;

        if (contextFontStack.TryPeek(out var previousFont))
        {
            switch (previousFont)
            {
                case VectorFont vectorFont:
                    size = vectorFont.Size;
                    break;
                case StackedFont stackedFont:
                    if (stackedFont.Stack.Length == 0 || stackedFont.Stack[0] is not VectorFont stackVectorFont)
                        break;

                    size = stackVectorFont.Size;
                    break;
            }
        }

        if (node.Attributes.TryGetValue("size", out var sizeParameter))
            size = (int) (sizeParameter.LongValue ?? size);

        var fontResource = cache.GetResource<FontResource>(resourcePath);
        return new VectorFont(fontResource, size);
    }
}
