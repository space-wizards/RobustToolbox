using System;
using Robust.Client.ResourceManagement;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Robust.Client.UserInterface.RichText;

public sealed class HeadingTag : IMarkupTag
{
    [Dependency] private readonly IResourceCache _resourceCache = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public string Name => "head";

    /// <inheritdoc/>
    public void PushDrawContext(MarkupNode node, MarkupDrawingContext context)
    {
        if (!node.Value.TryGetLong(out var levelParam))
            return;

        var level = Math.Min(Math.Max((int)levelParam, 1), 3);
        node.Attributes["size"] = new MarkupParameter(
            (int)Math.Ceiling(FontTag.DefaultSize * 2 / Math.Sqrt(level))
        );

        var font = FontTag.CreateFont(context.Font, node, _resourceCache, _prototypeManager, BoldTag.BoldFont);
        context.Font.Push(font);
    }

    /// <inheritdoc/>
    public void PopDrawContext(MarkupNode node, MarkupDrawingContext context)
    {
        context.Font.Pop();
    }
}
