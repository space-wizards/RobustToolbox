using System;
using System.Collections.Generic;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Robust.Server.GameStates;

internal class PVSSystem_New : EntitySystem
{
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    private PVSCollection<EntityUid> _entityPvsCollection = default!;
    private readonly List<IPVSCollection> _pvsCollections = new();

    public override void Initialize()
    {
        base.Initialize();

        _entityPvsCollection = RegisterPVSCollection<EntityUid>();
        _mapManager.MapCreated += OnMapCreated;
        _mapManager.MapDestroyed += OnMapDestroyed;
        _mapManager.OnGridCreated += OnGridCreated;
        _mapManager.OnGridRemoved += OnGridRemoved;
        _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
        SubscribeLocalEvent<MoveEvent>(OnEntityMove);
        SubscribeLocalEvent<TransformComponent, ComponentInit>(OnTransformInit);
        EntityManager.EntityDeleted += OnEntityDeleted;
    }

    private void OnTransformInit(EntityUid uid, TransformComponent component, ComponentInit args)
    {
        _entityPvsCollection.AddIndex(uid, component.Coordinates);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _mapManager.MapCreated -= OnMapCreated;
        _mapManager.MapDestroyed -= OnMapDestroyed;
        _mapManager.OnGridCreated -= OnGridCreated;
        _mapManager.OnGridRemoved -= OnGridRemoved;
        _playerManager.PlayerStatusChanged -= OnPlayerStatusChanged;
        EntityManager.EntityDeleted -= OnEntityDeleted;

    }

    private void OnEntityDeleted(object? sender, EntityUid e)
    {
        _entityPvsCollection.RemoveIndex(EntityManager.CurrentTick, e);
    }

    private void OnEntityMove(MoveEvent ev)
    {
        _entityPvsCollection.UpdateIndex(ev.Sender.Uid, ev.NewPosition);
    }

    public PVSCollection<T> RegisterPVSCollection<T>() where T : IComparable<T>, IEquatable<T>
    {
        var collection = new PVSCollection<T>();
        _pvsCollections.Add(collection);
        return collection;
    }

    public bool UnregisterPVSCollection<T>(PVSCollection<T> pvsCollection) where T : IComparable<T>, IEquatable<T> =>
        _pvsCollections.Remove(pvsCollection);

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
    {
        if (e.NewStatus == SessionStatus.InGame)
        {
            foreach (var pvsCollection in _pvsCollections)
            {
                pvsCollection.AddPlayer(e.Session);
            }
        }
        else if (e.NewStatus == SessionStatus.Disconnected)
        {
            foreach (var pvsCollection in _pvsCollections)
            {
                pvsCollection.RemovePlayer(e.Session);
            }
        }
    }

    private void OnGridRemoved(MapId mapid, GridId gridid)
    {
        foreach (var pvsCollection in _pvsCollections)
        {
            pvsCollection.RemoveGrid(gridid);
        }
    }

    private void OnGridCreated(MapId mapid, GridId gridid)
    {
        foreach (var pvsCollection in _pvsCollections)
        {
            pvsCollection.AddGrid(gridid);
        }
    }

    private void OnMapDestroyed(object? sender, MapEventArgs e)
    {
        foreach (var pvsCollection in _pvsCollections)
        {
            pvsCollection.RemoveMap(e.Map);
        }
    }

    private void OnMapCreated(object? sender, MapEventArgs e)
    {
        foreach (var pvsCollection in _pvsCollections)
        {
            pvsCollection.AddMap(e.Map);
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        _entityPvsCollection.Process();
    }
}
