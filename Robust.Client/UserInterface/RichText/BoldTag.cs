using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.IoC;
using Robust.Shared.Utility;

namespace Robust.Client.UserInterface.RichText;

public sealed class BoldTag : IMarkupTag
{
    [Dependency] private readonly IResourceCache _resourceCache = default!;
    public string Name => "bold";

    public void PushDrawContext(MarkupNode node, MarkupDrawingContext context)
    {
        var size = 12;

        if (context.Font.TryPeek(out var previousFont))
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

        var fontResource = _resourceCache.GetResource<FontResource>("/Fonts/NotoSans/NotoSans-Bold.ttf");
        var font = new VectorFont(fontResource, size);
        context.Font.Push(font);
    }

    public void PopDrawContext(MarkupNode node, MarkupDrawingContext context)
    {
        context.Font.Pop();
    }
}
