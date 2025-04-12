using System;
using System.Collections.Generic;
using System.Numerics;
using Robust.Client.UserInterface;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Robust.Client.GameObjects;

public sealed class UserInterfaceSystem : SharedUserInterfaceSystem
{
    private Dictionary<EntityUid, Dictionary<Enum, Vector2>> _savedPositions = new();
    private Dictionary<BoundUserInterface, Control> _registeredControls = new();

    public override void Initialize()
    {
        base.Initialize();
        ProtoManager.PrototypesReloaded += OnProtoReload;
    }

    public override void Shutdown()
    {
        base.Shutdown();
        ProtoManager.PrototypesReloaded -= OnProtoReload;
    }

    /// <inheritdoc />
    public override void OpenUi(Entity<UserInterfaceComponent?> entity, Enum key, bool predicted = false)
    {
        var player = Player.LocalEntity;

        if (player == null)
            return;

        OpenUi(entity, key, player.Value, predicted);
    }

    protected override void SavePosition(BoundUserInterface bui)
    {
        if (!_registeredControls.Remove(bui, out var control))
            return;

        var keyed = _savedPositions[bui.Owner];
        keyed[bui.UiKey] = control.Position;
    }

    /// <summary>
    /// Registers a control so it will later have its position stored by <see cref="SavePosition"/> when the BUI is closed.
    /// </summary>
    public void RegisterControl(BoundUserInterface bui, Control control)
    {
        DebugTools.Assert(!_registeredControls.ContainsKey(bui));
        _registeredControls[bui] = control;
        _savedPositions.GetOrNew(bui.Owner);
    }

    public override bool TryGetPosition(Entity<UserInterfaceComponent?> entity, Enum key, out Vector2 position)
    {
        position = default;

        if (!_savedPositions.TryGetValue(entity.Owner, out var keyed))
        {
            return false;
        }

        if (!keyed.TryGetValue(key, out position))
        {
            return false;
        }

        return true;
    }

    private void OnProtoReload(PrototypesReloadedEventArgs obj)
    {
        var player = Player.LocalEntity;

        if (!UserQuery.TryComp(player, out var userComp))
            return;

        foreach (var uid in userComp.OpenInterfaces.Keys)
        {
            if (!UIQuery.TryComp(uid, out var uiComp))
                continue;

            foreach (var bui in uiComp.ClientOpenInterfaces.Values)
            {
                bui.OnProtoReload(obj);
            }
        }
    }
}
