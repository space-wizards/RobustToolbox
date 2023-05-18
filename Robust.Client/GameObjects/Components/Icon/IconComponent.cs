using Robust.Client.Graphics;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Robust.Client.GameObjects;

/// <summary>
/// This component can be used to give an entity a simple texture which can be used for displaying the entity in some
/// UI elements. The texture must be specified as an RSI state, and will correspond to the first frame of the
/// south-direction. To actually access the icon, you need to use <see cref="SpriteSystem.GetIcon"/>
/// </summary>
/// <remarks>
/// This is texture is useful displaying entities that have non-trivial sprites that require some sort of set up in
/// order to display. E.g., entities that randomize their colour by modulating a simple base sprite will look odd if
/// shown directly, and spawning a client-side entity would lead to the colour being randomized each time the UI is
/// updated.
/// </remarks>
[RegisterComponent]
public sealed class IconComponent : Component
{
    [IncludeDataField]
    public readonly SpriteSpecifier.Rsi IconSpec = default!;

    [ViewVariables]
    [Access(typeof(SpriteSystem), Other = AccessPermissions.None)] // Use Use SpriteSystem.GetIcon();
    internal Texture? Icon;
}
