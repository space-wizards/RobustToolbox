using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Robust.Client.UserInterface.Controls;

[Virtual]
public class EntityPrototypeView : SpriteView
{
    private string? _currentPrototype;
    private EntityUid? _ourEntity;

    public EntityPrototypeView()
    {

    }

    public EntityPrototypeView(EntProtoId? entProto, IEntityManager entMan) : base(entMan)
    {
        SetPrototype(entProto);
    }

    public void SetPrototype(EntProtoId? entProto)
    {
        SpriteSystem ??= EntMan.System<SpriteSystem>();

        if (entProto == _currentPrototype
            && EntMan.TryGetComponent(Entity?.Owner, out MetaDataComponent? meta)
            && meta.EntityPrototype?.ID == _currentPrototype)
        {
            return;
        }

        _currentPrototype = entProto;
        SetEntity(null);
        if (_ourEntity != null)
        {
            EntMan.DeleteEntity(_ourEntity);
        }

        if (_currentPrototype != null)
        {
            _ourEntity = EntMan.Spawn(_currentPrototype);
            SpriteSystem.ForceUpdate(_ourEntity.Value);
            SetEntity(_ourEntity);
        }
    }

    protected override void EnteredTree()
    {
        base.EnteredTree();

        if (_currentPrototype != null)
            SetPrototype(_currentPrototype);
    }

    protected override void ExitedTree()
    {
        base.ExitedTree();

        if (!EntMan.Deleted(_ourEntity))
            EntMan.QueueDeleteEntity(_ourEntity);
    }
}
