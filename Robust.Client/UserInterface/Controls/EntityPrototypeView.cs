using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Robust.Client.UserInterface.Controls;

[Virtual]
public class EntityPrototypeView : SpriteView
{
    private string? _currentPrototype;
    private EntityUid? _ourEntity;
    private bool _isShowing;

    public EntityPrototypeView()
    {

    }

    public EntityPrototypeView(EntProtoId? entProto, IEntityManager entMan) : base(entMan)
    {
        SetPrototype(entProto);
    }

    public void SetPrototype(EntProtoId? entProto)
    {
        if (entProto == _currentPrototype
            && EntMan.TryGetComponent(Entity?.Owner, out MetaDataComponent? meta)
            && meta.EntityPrototype?.ID == _currentPrototype)
        {
            return;
        }

        _currentPrototype = entProto;

        if (_ourEntity != null || _isShowing)
        {
            UpdateEntity();
        }
    }

    protected override void EnteredTree()
    {
        base.EnteredTree();

        if (_currentPrototype != null)
        {
            UpdateEntity();
        }

        _isShowing = true;
    }

    protected override void ExitedTree()
    {
        base.ExitedTree();
        EntMan.TryQueueDeleteEntity(_ourEntity);
        _ourEntity = null;

        _isShowing = false;
    }

    private void UpdateEntity()
    {
        SetEntity(null);
        EntMan.DeleteEntity(_ourEntity);

        if (_currentPrototype != null)
        {
            SpriteSystem ??= EntMan.System<SpriteSystem>();

            _ourEntity = EntMan.Spawn(_currentPrototype);
            SpriteSystem.ForceUpdate(_ourEntity.Value);
            SetEntity(_ourEntity);
        }
        else
        {
            _ourEntity = null;
        }
    }
}
