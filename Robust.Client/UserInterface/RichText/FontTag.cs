using System.Collections.Generic;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Robust.Client.UserInterface.RichText;

/// <summary>
/// Applies the font provided as the tags parameter to the markup drawing context.
/// Definitely not save for user supplied markup
/// </summary>
public sealed class FontTag : IMarkupTag
{
    public const string DefaultFont = "Default";
    public const int DefaultSize = 12;

    [Dependency] private readonly IResourceCache _resourceCache = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public string Name => "font";

    /// <inheritdoc/>
    public void PushDrawContext(MarkupNode node, MarkupDrawingContext context)
    {
        string fontId = node.Value.StringValue ?? DefaultFont;

        var font = CreateFont(context.Font, node, _resourceCache, _prototypeManager, fontId);
        context.Font.Push(font);
    }

    /// <inheritdoc/>
    public void PopDrawContext(MarkupNode node, MarkupDrawingContext context)
    {
        context.Font.Pop();
    }

    /// <summary>
    /// Creates the a vector font from the supplied font id.<br/>
    /// The size of the resulting font will be either the size supplied as a parameter to the tag, the previous font size or 12
    /// </summary>
    public static Font CreateFont(
        Stack<Font> contextFontStack,
        MarkupNode node,
        IResourceCache cache,
        IPrototypeManager prototypeManager,
        string fontId)
    {
        var size = DefaultSize;

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

        if (!prototypeManager.TryIndex<FontPrototype>(fontId, out var prototype))
            prototype = prototypeManager.Index<FontPrototype>(DefaultFont);

        var fontResource = cache.GetResource<FontResource>(prototype.Path);
        return new VectorFont(fontResource, size);
    }
}
