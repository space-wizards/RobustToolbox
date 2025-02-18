using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.EntitySerialization.Components;

/// <summary>
/// This component is optionally added to entities that get loaded from yaml files. It stores the UID that the entity
/// had within the yaml file. This is used when saving the entity back to a yaml file so that it re-uses the same UID.
/// </summary>
/// <remarks>
/// This is primarily intended to reduce the diff sizes when modifying yaml maps. Note that there is no guarantee that
/// the given uid will be used when writing the entity. E.g., if more than one entity have this component with the
/// same uid, only one of those entities will be saved with the requested id.
/// </remarks>
[RegisterComponent, UnsavedComponent]
public sealed partial class YamlUidComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public int Uid;
}
