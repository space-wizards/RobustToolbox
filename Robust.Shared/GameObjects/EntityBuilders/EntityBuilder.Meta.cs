using Robust.Shared.Localization;

namespace Robust.Shared.GameObjects.EntityBuilders;

public sealed partial class EntityBuilder
{
    /// <summary>
    ///     Sets the name of the entity.
    /// </summary>
    public EntityBuilder Named(string newName)
    {
        MetaData._entityName = newName;
        return this;
    }

    /// <summary>
    ///     Sets the name of the entity to the given localization string.
    /// </summary>
    public EntityBuilder NamedLoc(LocId newName)
    {
        MetaData._entityName = _locMan.GetString(newName);
        return this;
    }

    /// <summary>
    ///     Sets the description of the entity.
    /// </summary>
    public EntityBuilder Described(string newDesc)
    {
        MetaData._entityDescription = newDesc;
        return this;
    }

    /// <summary>
    ///     Sets the description of the entity to the given localization string.
    /// </summary>
    public EntityBuilder DescribedLoc(LocId newDesc)
    {
        MetaData._entityDescription = _locMan.GetString(newDesc);
        return this;
    }
}
