using Robust.Client.ResourceManagement;
using Robust.Shared.IoC;
using Robust.Shared.Utility;

namespace Robust.Client.UserInterface.RichText;

public sealed class ItalicTag : IMarkupTag
{
    [Dependency] private readonly IResourceCache _resourceCache = default!;
    public string Name => "italic";

    public void PushDrawContext(MarkupNode node, MarkupDrawingContext context)
    {
        var font = FontTag.CreateFont(context.Font, node, _resourceCache, "/Fonts/NotoSans/NotoSans-Italic.ttf");
        context.Font.Push(font);
    }

    public void PopDrawContext(MarkupNode node, MarkupDrawingContext context)
    {
        context.Font.Pop();
    }
}
