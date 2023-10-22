using System;
using System.Linq;
using System.Numerics;
using BenchmarkDotNet.Attributes;
using Robust.Server.Containers;
using Robust.Server.GameStates;
using Robust.Shared;
using Robust.Shared.Analyzers;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.UnitTesting.Server;

namespace Robust.Benchmarks.Transform;

/// <summary>
/// This benchmark tests various transform/move related functions with an entity that has many children.
/// </summary>
[Virtual, MemoryDiagnoser]
public class RecursiveMoveBenchmark
{
    private ISimulation _simulation = default!;
    private IEntityManager _entMan = default!;
    private SharedTransformSystem _transform = default!;
    private ContainerSystem _container = default!;
    private PvsSystem _pvs = default!;
    private EntityCoordinates _mapCoords;
    private EntityCoordinates _gridCoords;
    private EntityUid _ent;
    private EntityUid _child;
    private TransformComponent _childXform = default!;
    private EntityQuery<TransformComponent> _query;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _simulation = RobustServerSimulation
            .NewSimulation()
            .InitializeInstance();

        if (!_simulation.Resolve<IConfigurationManager>().GetCVar(CVars.NetPVS))
            throw new InvalidOperationException("PVS must be enabled");

        _entMan = _simulation.Resolve<IEntityManager>();
        _transform = _entMan.System<SharedTransformSystem>();
        _container = _entMan.System<ContainerSystem>();
        _pvs = _entMan.System<PvsSystem>();
        _query = _entMan.GetEntityQuery<TransformComponent>();

        // Create map & grid
        var mapMan = _simulation.Resolve<IMapManager>();
        var mapSys = _entMan.System<SharedMapSystem>();
        var mapId = mapMan.CreateMap();
        var map = mapMan.GetMapEntityId(mapId);
        var gridComp = mapMan.CreateGridEntity(mapId);
        var grid = gridComp.Owner;
        _gridCoords = new EntityCoordinates(grid, .5f, .5f);
        _mapCoords = new EntityCoordinates(map, 100, 100);
        mapSys.SetTile(grid, gridComp, Vector2i.Zero, new Tile(1));

        // Next, we will spawn our test entity. This entity will have a  complex transform/container hierarchy.
        // This is intended to be representative of a typical SS14 player entity, with organs. clothing, and a full backpack.
        _ent = _entMan.Spawn();

        // Quick check that SetCoordinates actually changes the parent as expected
        // I.e., ensure that grid-traversal code doesn't just dump the entity on the map.
        _transform.SetCoordinates(_ent, _gridCoords);
        if (_query.GetComponent(_ent).ParentUid != _gridCoords.EntityId)
            throw new Exception("Grid traversal error.");

        _transform.SetCoordinates(_ent, _mapCoords);
        if (_query.GetComponent(_ent).ParentUid != _mapCoords.EntityId)
            throw new Exception("Grid traversal error.");

        // Add 5 direct children in slots to represent clothing.
        for (var i = 0; i < 5; i++)
        {
            var id = $"inventory{i}";
            _container.EnsureContainer<ContainerSlot>(_ent, id);
            if (!_entMan.TrySpawnInContainer(null, _ent, id, out _))
                throw new Exception($"Failed to setup entity");
        }

        // body parts
        _container.EnsureContainer<Container>(_ent, "body");
        for (var i = 0; i < 5; i++)
        {
            // Simple organ
            if (!_entMan.TrySpawnInContainer(null, _ent, "body", out _))
                throw new Exception($"Failed to setup entity");

            // body part that has another body part / limb
            if (!_entMan.TrySpawnInContainer(null, _ent, "body", out var limb))
                throw new Exception($"Failed to setup entity");

            _container.EnsureContainer<ContainerSlot>(limb.Value, "limb");
            if (!_entMan.TrySpawnInContainer(null, limb.Value, "limb", out _))
                throw new Exception($"Failed to setup entity");
        }

        // Backpack
        _container.EnsureContainer<ContainerSlot>(_ent, "inventory-backpack");
        if (!_entMan.TrySpawnInContainer(null, _ent, "inventory-backpack", out var backpack))
            throw new Exception($"Failed to setup entity");

        // Misc backpack contents.
        var backpackStorage = _container.EnsureContainer<Container>(backpack.Value, "storage");
        for (var i = 0; i < 10; i++)
        {
            if (!_entMan.TrySpawnInContainer(null, backpack.Value, "storage", out _))
                throw new Exception($"Failed to setup entity");
        }

        // Emergency box inside of the backpack
        var box = backpackStorage.ContainedEntities.First();
        var boxContainer = _container.EnsureContainer<Container>(box, "storage");
        for (var i = 0; i < 10; i++)
        {
            if (!_entMan.TrySpawnInContainer(null, box, "storage", out _))
                throw new Exception($"Failed to setup entity");
        }

        // Deepest child.
        _child = boxContainer.ContainedEntities.First();
        _childXform = _query.GetComponent(_child);

        _pvs.ProcessCollections();
    }

    /// <summary>
    /// This implicitly measures move events, including PVS and entity lookups. Though given that most of the entities
    /// are in containers, this will bias the entity lookup aspect.
    /// </summary>
    [Benchmark]
    public void MoveEntity()
    {
        _transform.SetCoordinates(_ent, _gridCoords);
        _transform.SetCoordinates(_ent, _mapCoords);
    }

    /// <summary>
    /// Like <see cref="MoveEntity"/>, but also processes queued PVS chunk updates.
    /// </summary>
    [Benchmark]
    public void MoveAndUpdateChunks()
    {
        _transform.SetCoordinates(_ent, _gridCoords);
        _pvs.ProcessCollections();
        _transform.SetCoordinates(_ent, _mapCoords);
        _pvs.ProcessCollections();
    }

    [Benchmark]
    public Vector2 GetWorldPos()
    {
        return _transform.GetWorldPosition(_childXform);
    }

    [Benchmark]
    public EntityUid GetRootUid()
    {
        var xform = _childXform;
        while (xform.ParentUid.IsValid())
        {
            xform = _query.GetComponent(xform.ParentUid);
        }
        return xform.ParentUid;
    }
}
