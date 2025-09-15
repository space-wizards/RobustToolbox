using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
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
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.UnitTesting;

namespace Robust.Benchmarks.Transform;

/// <summary>
/// This benchmark tests various transform/move related functions with an entity that has many children.
/// </summary>
[Virtual, MemoryDiagnoser]
public class RecursiveMoveBenchmark : RobustIntegrationTest
{
    private IEntityManager _entMan = default!;
    private SharedTransformSystem _transform = default!;
    private ContainerSystem _container = default!;
    private PvsSystem _pvs = default!;
    private EntityCoordinates _mapCoords;
    private EntityCoordinates _gridCoords;
    private EntityCoordinates _gridCoords2;
    private EntityUid _ent;
    private EntityUid _child;
    private TransformComponent _childXform = default!;
    private EntityQuery<TransformComponent> _query;
    private ICommonSession[] _players = default!;
    private PvsSession _session = default!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        ProgramShared.PathOffset = "../../../../";
        var server = StartServer(new() {Pool = false});
        var client = StartClient(new() {Pool = false});

        Task.WhenAll(client.WaitIdleAsync(), server.WaitIdleAsync()).Wait();

        var mapMan = server.ResolveDependency<IMapManager>();
        _entMan = server.ResolveDependency<IEntityManager>();
        var confMan = server.ResolveDependency<IConfigurationManager>();
        var sPlayerMan = server.ResolveDependency<ISharedPlayerManager>();

        _transform = _entMan.System<SharedTransformSystem>();
        _container = _entMan.System<ContainerSystem>();
        _pvs = _entMan.System<PvsSystem>();
        _query = _entMan.GetEntityQuery<TransformComponent>();
        var mapSys = _entMan.System<SharedMapSystem>();

        var netMan = client.ResolveDependency<IClientNetManager>();
        client.SetConnectTarget(server);
        client.Post(() => netMan.ClientConnect(null!, 0, null!));
        server.Post(() => confMan.SetCVar(CVars.NetPVS, true));

        for (int i = 0; i < 10; i++)
        {
            server.WaitRunTicks(1).Wait();
            client.WaitRunTicks(1).Wait();
        }

        // Ensure client & server ticks are synced.
        // Client runs 1 tick ahead
        {
            var sTick = (int)server.Timing.CurTick.Value;
            var cTick = (int)client.Timing.CurTick.Value;
            var delta = cTick - sTick;

            if (delta > 1)
                server.WaitRunTicks(delta - 1).Wait();
            else if (delta < 1)
                client.WaitRunTicks(1 - delta).Wait();

            sTick = (int)server.Timing.CurTick.Value;
            cTick = (int)client.Timing.CurTick.Value;
            delta = cTick - sTick;
            if (delta != 1)
                throw new Exception("Failed setup");
        }

        // Set up map and spawn player
        server.WaitPost(() =>
        {
            var map = server.ResolveDependency<SharedMapSystem>().CreateMap(out var mapId);
            var gridComp = mapMan.CreateGridEntity(mapId);
            var grid = gridComp.Owner;
            mapSys.SetTile(grid, gridComp, Vector2i.Zero, new Tile(1));
            _gridCoords = new EntityCoordinates(grid, .5f, .5f);
            _gridCoords2 = new EntityCoordinates(grid, .5f, .6f);
            _mapCoords = new EntityCoordinates(map, 100, 100);

            var playerUid = _entMan.SpawnEntity(null, _mapCoords);

            // Attach player.
            var session = sPlayerMan.Sessions.First();
            server.PlayerMan.SetAttachedEntity(session, playerUid);
            sPlayerMan.JoinGame(session);

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

            _players = new[] {session};
            _session = _pvs.PlayerData[session];
        }).Wait();

        for (int i = 0; i < 10; i++)
        {
            server.WaitRunTicks(1).Wait();
            client.WaitRunTicks(1).Wait();
        }

        PvsTick();
        PvsTick();
    }

    private void PvsTick()
    {
        _session.ClearState();
        _pvs.CacheSessionData(_players);
        _pvs.GetVisibleChunks();
        _pvs.ProcessVisibleChunksSequential();
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

    [Benchmark]
    public void MoveEntityASmidge()
    {
        _transform.SetCoordinates(_ent, _gridCoords);
        _transform.SetCoordinates(_ent, _gridCoords2);
    }

    /// <summary>
    /// Like <see cref="MoveEntity"/>, but also processes queued PVS chunk updates.
    /// </summary>
    [Benchmark]
    public void MoveAndUpdateChunks()
    {
        _transform.SetCoordinates(_ent, _gridCoords);
        PvsTick();
        _transform.SetCoordinates(_ent, _mapCoords);
        PvsTick();
    }

    [Benchmark]
    public void MoveASmidgeAndUpdateChunk()
    {
        _transform.SetCoordinates(_ent, _gridCoords);
        PvsTick();
        _transform.SetCoordinates(_ent, _gridCoords2);
        PvsTick();
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
