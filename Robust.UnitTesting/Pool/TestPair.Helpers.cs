using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Robust.UnitTesting.Pool;

// This partial file contains misc helper functions to make writing tests easier.
public partial class TestPair<TServer, TClient>
{
    /// <summary>
    /// Convert a client-side uid into a server-side uid
    /// </summary>
    public EntityUid ToServerUid(EntityUid uid) => ConvertUid(uid, Client, Server);

    /// <summary>
    /// Convert a server-side uid into a client-side uid
    /// </summary>
    public EntityUid ToClientUid(EntityUid uid) => ConvertUid(uid, Server, Client);

    private static EntityUid ConvertUid(EntityUid uid, IIntegrationInstance source, IIntegrationInstance destination)
    {
        if (!uid.IsValid())
            return EntityUid.Invalid;

        if (!source.EntMan.TryGetComponent<MetaDataComponent>(uid, out var meta))
        {
            Assert.Fail($"Failed to resolve MetaData while converting the EntityUid for entity {uid}");
            return EntityUid.Invalid;
        }

        if (!destination.EntMan.TryGetEntity(meta.NetEntity, out var otherUid))
        {
            Assert.Fail($"Failed to resolve net ID while converting the EntityUid entity {source.EntMan.ToPrettyString(uid)}");
            return EntityUid.Invalid;
        }

        return otherUid.Value;
    }

    /// <summary>
    /// Execute a command on the server and wait some number of ticks.
    /// </summary>
    public async Task WaitCommand(string cmd, int numTicks = 10)
    {
        await Server.ExecuteCommand(cmd);
        await RunTicksSync(numTicks);
    }

    /// <summary>
    /// Execute a command on the client and wait some number of ticks.
    /// </summary>
    public async Task WaitClientCommand(string cmd, int numTicks = 10)
    {
        await Client.ExecuteCommand(cmd);
        await RunTicksSync(numTicks);
    }

    /// <summary>
    /// Retrieve all entity prototypes that have some component.
    /// </summary>
    public List<(EntityPrototype, T)> GetPrototypesWithComponent<T>(
        HashSet<string>? ignored = null,
        bool ignoreAbstract = true,
        bool ignoreTestPrototypes = true)
        where T : IComponent, new()
    {
        if (!Server.Resolve<IComponentFactory>().TryGetRegistration<T>(out var reg)
            && !Client.Resolve<IComponentFactory>().TryGetRegistration<T>(out reg))
        {
            Assert.Fail($"Unknown component: {typeof(T).Name}");
            return new();
        }

        var id = reg.Name;
        var list = new List<(EntityPrototype, T)>();
        foreach (var proto in Server.ProtoMan.EnumeratePrototypes<EntityPrototype>())
        {
            if (ignored != null && ignored.Contains(proto.ID))
                continue;

            if (ignoreAbstract && proto.Abstract)
                continue;

            if (ignoreTestPrototypes && IsTestPrototype(proto))
                continue;

            if (proto.Components.TryGetComponent(id, out var cmp))
                list.Add((proto, (T)cmp));
        }

        return list;
    }

    /// <summary>
    /// Retrieve all entity prototypes that have some component.
    /// </summary>
    public List<EntityPrototype> GetPrototypesWithComponent(
        Type type,
        HashSet<string>? ignored = null,
        bool ignoreAbstract = true,
        bool ignoreTestPrototypes = true)
    {
        if (!Server.Resolve<IComponentFactory>().TryGetRegistration(type, out var reg)
            && !Client.Resolve<IComponentFactory>().TryGetRegistration(type, out reg))
        {
            Assert.Fail($"Unknown component: {type.Name}");
            return new();
        }

        var id = reg.Name;
        var list = new List<EntityPrototype>();
        foreach (var proto in Server.ProtoMan.EnumeratePrototypes<EntityPrototype>())
        {
            if (ignored != null && ignored.Contains(proto.ID))
                continue;

            if (ignoreAbstract && proto.Abstract)
                continue;

            if (ignoreTestPrototypes && IsTestPrototype(proto))
                continue;

            if (proto.Components.ContainsKey(id))
                list.Add((proto));
        }

        return list;
    }

    public async Task Connect()
    {
        Assert.That(Client.NetMan.IsConnected, Is.False);
        await Client.Connect(Server);
        await ReallyBeIdle(10);
        await Client.WaitRunTicks(1);
    }

    public async Task Disconnect(string reason = "")
    {
        await Client.WaitPost(() => Client.CNetMan.ClientDisconnect(reason));
        await ReallyBeIdle(10);
    }

    public bool IsTestPrototype(EntityPrototype proto)
    {
        return _loadedEntityPrototypes.Contains(proto.ID);
    }

    public bool IsTestEntityPrototype(string id)
    {
        return _loadedEntityPrototypes.Contains(id);
    }

    public bool IsTestPrototype<TPrototype>(string id) where TPrototype : IPrototype
    {
        return IsTestPrototype(typeof(TPrototype), id);
    }

    public bool IsTestPrototype<TPrototype>(TPrototype proto) where TPrototype : IPrototype
    {
        return IsTestPrototype(typeof(TPrototype), proto.ID);
    }

    public bool IsTestPrototype(Type kind, string id)
    {
        return _loadedPrototypes.TryGetValue(kind, out var ids) && ids.Contains(id);
    }

    /// <summary>
    /// Runs the server-client pair in sync
    /// </summary>
    /// <param name="ticks">How many ticks to run them for</param>
    public async Task RunTicksSync(int ticks)
    {
        for (var i = 0; i < ticks; i++)
        {
            await Server.WaitRunTicks(1);
            await Client.WaitRunTicks(1);
        }
    }

    /// <summary>
    /// Convert a time interval to some number of ticks.
    /// </summary>
    public int SecondsToTicks(float seconds)
    {
        return (int) Math.Ceiling(seconds / Server.Timing.TickPeriod.TotalSeconds);
    }

    /// <summary>
    /// Run the server & client in sync for some amount of time
    /// </summary>
    public async Task RunSeconds(float seconds)
    {
        await RunTicksSync(SecondsToTicks(seconds));
    }

    /// <summary>
    /// Runs the server-client pair in sync, but also ensures they are both idle each tick.
    /// </summary>
    /// <param name="runTicks">How many ticks to run</param>
    public async Task ReallyBeIdle(int runTicks = 25)
    {
        for (var i = 0; i < runTicks; i++)
        {
            await Client.WaitRunTicks(1);
            await Server.WaitRunTicks(1);
            for (var idleCycles = 0; idleCycles < 4; idleCycles++)
            {
                await Client.WaitIdleAsync();
                await Server.WaitIdleAsync();
            }
        }
    }

    /// <summary>
    /// Run the server/clients until the ticks are synchronized.
    /// By default the client will be one tick ahead of the server.
    /// </summary>
    public async Task SyncTicks(int targetDelta = 1)
    {
        var sTick = (int)Server.Timing.CurTick.Value;
        var cTick = (int)Client.Timing.CurTick.Value;
        var delta = cTick - sTick;

        if (delta == targetDelta)
            return;
        if (delta > targetDelta)
            await Server.WaitRunTicks(delta - targetDelta);
        else
            await Client.WaitRunTicks(targetDelta - delta);

        sTick = (int)Server.Timing.CurTick.Value;
        cTick = (int)Client.Timing.CurTick.Value;
        delta = cTick - sTick;
        Assert.That(delta, Is.EqualTo(targetDelta));
    }

    /// <summary>
    /// Creates a map with a single grid consisting of one tile.
    /// </summary>
    [MemberNotNull(nameof(TestMap))]
    public async Task<TestMapData> CreateTestMap(bool initialized, ushort tileTypeId)
    {
        TestMap = new TestMapData();
        await Server.WaitIdleAsync();
        var sys = Server.System<SharedMapSystem>();

        await Server.WaitPost(() =>
        {
            TestMap.MapUid = sys.CreateMap(out TestMap.MapId, runMapInit: initialized);
            TestMap.Grid = Server.MapMan.CreateGridEntity(TestMap.MapId);
            TestMap.GridCoords = new EntityCoordinates(TestMap.Grid, 0, 0);
            TestMap.MapCoords = new MapCoordinates(0, 0, TestMap.MapId);
            sys.SetTile(TestMap.Grid.Owner, TestMap.Grid.Comp, TestMap.GridCoords, new Tile(tileTypeId));
            TestMap.Tile = sys.GetAllTiles(TestMap.Grid.Owner, TestMap.Grid.Comp).First();
        });

        if (!Settings.Connected)
            return TestMap;

        await RunTicksSync(10);
        TestMap.CMapUid = ToClientUid(TestMap.MapUid);
        TestMap.CGridUid = ToClientUid(TestMap.Grid);
        TestMap.CGridCoords = new EntityCoordinates(TestMap.CGridUid, 0, 0);

        return TestMap;
    }

    /// <inheritdoc cref="CreateTestMap(bool, ushort)"/>
    [MemberNotNull(nameof(TestMap))]
    public async Task<TestMapData> CreateTestMap(bool initialized, string tileName)
    {
        var defMan = Server.Resolve<ITileDefinitionManager>();
        if (!defMan.TryGetDefinition(tileName, out var def))
            Assert.Fail($"Unknown tile: {tileName}");
        return await CreateTestMap(initialized, def?.TileId ?? 1);
    }
}
