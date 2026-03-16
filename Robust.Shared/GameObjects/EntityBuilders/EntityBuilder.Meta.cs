using Robust.Shared.Localization;

namespace Robust.Shared.GameObjects.EntityBuilders;

public sealed partial class EntityBuilder
{
    /// <summary>
    ///     Sets the name of the entity.
    /// </summary>
    public EntityBuilder Named(string newName)
    {
        GetComp(out MetaDataComponent meta);
        meta._entityName = newName;
        return this;
    }

    /// <summary>
    ///     Sets the name of the entity to the given localization string.
    /// </summary>
    public EntityBuilder NamedLoc(LocId newName)
    {
        GetComp(out MetaDataComponent meta);
        meta._entityName = _locMan.GetString(newName);
        return this;
    }

    /// <summary>
    ///     Sets the description of the entity.
    /// </summary>
    public EntityBuilder Described(string newDesc)
    {
        GetComp(out MetaDataComponent meta);
        meta._entityDescription = newDesc;
        return this;
    }

    /// <summary>
    ///     Sets the description of the entity to the given localization string.
    /// </summary>
    public EntityBuilder DescribedLoc(LocId newDesc)
    {
        GetComp(out MetaDataComponent meta);
        meta._entityDescription = _locMan.GetString(newDesc);
        return this;
    }
}
