using JetBrains.Annotations;
using Robust.Client.Animus.Actions;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Robust.Client.Animus.States;

[PublicAPI]
internal sealed partial class AnimusStateSpriteChange : AnimusStateBase
{
    [DataField]
    internal AnimusActionSpriteChangeBase Action = NullAction;

    private static readonly AnimusActionSpriteChangeBase NullAction = new AnimusActionSpriteChangeNull();
    private EntityManager _entityManager;

    internal override void Initialize(EntityUid ent, EntityManager entityManager, AnimusInstance animusInstance)
    {
        base.Initialize(ent, entityManager, animusInstance);
        _entityManager = entityManager;
        if (!entityManager.TryGetComponent<SpriteComponent>(ent, out var spriteComponent))
            return;
        Action.Initialize((ent, spriteComponent), entityManager);
    }

    internal override void Enter(EntityUid ent, bool enteredByTrigger)
    {
        if (!_entityManager.TryGetComponent<SpriteComponent>(ent, out var spriteComponent))
            return;
        Action.ExecuteSpriteChange((ent, spriteComponent));
    }

    internal override void Exit(EntityUid ent)
    {
        if (!_entityManager.TryGetComponent<SpriteComponent>(ent, out var spriteComponent))
            return;
        Action.ResetSpriteChange((ent, spriteComponent));
    }
}
