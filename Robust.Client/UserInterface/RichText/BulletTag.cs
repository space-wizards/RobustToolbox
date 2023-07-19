using Robust.Shared.Utility;

namespace Robust.Client.UserInterface.RichText;

public sealed class BulletTag : IMarkupTag
{
    public string Name => "bullet";
    public bool IsUnsafe => false;

    /// <inheritdoc/>
    public string TextBefore(MarkupNode _) => " · ";
}
