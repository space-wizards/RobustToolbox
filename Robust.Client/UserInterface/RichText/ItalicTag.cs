using System.Linq;
using Robust.Client.ResourceManagement;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Robust.Client.UserInterface.RichText;

#pragma warning disable CS0618 // Type or member is obsolete
public sealed class ItalicTag : IMarkupTag
#pragma warning restore CS0618 // Type or member is obsolete
{
    public const string ItalicFont = "DefaultItalic";

    [Dependency] private readonly IResourceCache _resourceCache = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public string Name => "italic";

    /// <inheritdoc/>
    public void PushDrawContext(MarkupNode node, MarkupDrawingContext context)
    {
        var font = FontTag.CreateFont(context.Font, node, _resourceCache, _prototypeManager,
            context.Tags.Any(static x => x is BoldTag)
                ? BoldItalicTag.BoldItalicFont
                : ItalicFont
        );
        context.Font.Push(font);
    }

    /// <inheritdoc/>
    public void PopDrawContext(MarkupNode node, MarkupDrawingContext context)
    {
        context.Font.Pop();
    }
}
