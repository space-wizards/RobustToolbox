using Robust.Client.ResourceManagement;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Robust.Client.UserInterface.RichText;

public sealed class ItalicTag : IMarkupTag
{
    public const string ItalicFont = "DefaultItalic";

    [Dependency] private readonly IResourceCache _resourceCache = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public string Name => "italic";

    /// <inheritdoc/>
    public void PushDrawContext(MarkupNode node, MarkupDrawingContext context)
    {
        string fontId;
        if (context.Font.TryPeek(out var previousFont)
            && previousFont is Graphics.VectorFont { Name: BoldTag.BoldFont })
            fontId = BoldItalicTag.BoldItalicFont;
        else
            fontId = ItalicFont;
        var font = FontTag.CreateFont(context.Font, node, _resourceCache, _prototypeManager, fontId);
        context.Font.Push(font);
    }

    /// <inheritdoc/>
    public void PopDrawContext(MarkupNode node, MarkupDrawingContext context)
    {
        context.Font.Pop();
    }
}
