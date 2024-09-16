using Robust.Shared.Utility;

namespace Robust.Client.UserInterface.RichText;

#pragma warning disable CS0618 // Type or member is obsolete
public sealed class BulletTag : IMarkupTag
#pragma warning restore CS0618 // Type or member is obsolete
{
    public string Name => "bullet";

    /// <inheritdoc/>
    public string TextBefore(MarkupNode _) => " · ";
}
