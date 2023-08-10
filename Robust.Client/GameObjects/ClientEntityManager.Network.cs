using Robust.Shared.GameObjects;

namespace Robust.Client.GameObjects;

public sealed partial class ClientEntityManager
{
    /// <summary>
    /// Clientside ents never get valid NetEntities.
    /// </summary>
    protected override NetEntity GenerateNetEntity() => NetEntity.Invalid;
}
