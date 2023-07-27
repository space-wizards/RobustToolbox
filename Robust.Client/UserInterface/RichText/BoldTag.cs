using Robust.Client.ResourceManagement;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Robust.Client.UserInterface.RichText;

public sealed class BoldTag : IMarkupTag
{
    public const string BoldFont = "DefaultBold";

    [Dependency] private readonly IResourceCache _resourceCache = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public string Name => "bold";

    /// <inheritdoc/>
    public void PushDrawContext(MarkupNode node, MarkupDrawingContext context)
    {
        string fontId;
        if (context.Font.TryPeek(out var previousFont)
            && previousFont is Graphics.VectorFont { Name: ItalicTag.ItalicFont })
            fontId = BoldItalicTag.BoldItalicFont;
        else
            fontId = BoldFont;
        var font = FontTag.CreateFont(context.Font, node, _resourceCache, _prototypeManager, fontId);
        context.Font.Push(font);
    }

    /// <inheritdoc/>
    public void PopDrawContext(MarkupNode node, MarkupDrawingContext context)
    {
        context.Font.Pop();
    }
}
