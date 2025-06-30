using Robust.Shared.Utility;

namespace Robust.Client.UserInterface.RichText;

public sealed class BulletTag : IMarkupTagHandler
{
    public string Name => "bullet";

    /// <inheritdoc/>
    public string TextBefore(MarkupNode _) => " · ";
}
