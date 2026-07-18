using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Robust.Client.Animus.Actions;

[ImplicitDataDefinitionForInheritors]
[PublicAPI]
public abstract partial class AnimusActionSpriteChangeBase
{
    public abstract void Initialize(Entity<SpriteComponent> entity, EntityManager entityManager);
    public abstract void ExecuteSpriteChange(Entity<SpriteComponent> entity);
    public abstract void ResetSpriteChange(Entity<SpriteComponent> entity);
}

public sealed partial class AnimusActionSpriteChangeNull : AnimusActionSpriteChangeBase
{
    public override void Initialize(Entity<SpriteComponent> entity, EntityManager entityManager)
    {
    }

    public override void ExecuteSpriteChange(Entity<SpriteComponent> entity)
    {
    }

    public override void ResetSpriteChange(Entity<SpriteComponent> entity)
    {
    }
}
