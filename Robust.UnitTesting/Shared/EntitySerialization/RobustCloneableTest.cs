using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Robust.UnitTesting.Shared.EntitySerialization;

[Serializable, NetSerializable]
[DataDefinition]
public sealed partial class RobustCloneableTestClass : IRobustCloneable<RobustCloneableTestClass>
{
    [DataField]
    public int IntValue;

    public RobustCloneableTestClass Clone()
    {
        return new RobustCloneableTestClass
        {
            IntValue = IntValue
        };
    }
}

[Serializable, NetSerializable]
[DataDefinition]
public partial struct RobustCloneableTestStruct : IRobustCloneable<RobustCloneableTestStruct>
{
    [DataField]
    public int IntValue;

    public RobustCloneableTestStruct Clone()
    {
        return new RobustCloneableTestStruct
        {
            IntValue = IntValue
        };
    }
}

[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RobustCloneableTestComponent : Component
{
    [DataField, AutoNetworkedField]
    public RobustCloneableTestClass TestClass = new();

    [DataField, AutoNetworkedField]
    public RobustCloneableTestStruct TestStruct = new();

    [DataField, AutoNetworkedField]
    public RobustCloneableTestStruct? NullableTestStruct;
}

public sealed class RobustCloneableTest() : RobustIntegrationTest
{
    [Test]
    public async Task TestClone()
    {
        var server = StartServer();
        var client = StartClient();

        await Task.WhenAll(server.WaitIdleAsync(), client.WaitIdleAsync());

        var sEntMan = server.EntMan;
        var sPlayerMan = server.ResolveDependency<ISharedPlayerManager>();
        var cEntMan = client.EntMan;
        var cNetMan = client.ResolveDependency<IClientNetManager>();

        MapId mapId = default;
        await server.WaitPost(() =>
        {
            server.System<SharedMapSystem>().CreateMap(out mapId);
            var coords = new MapCoordinates(0, 0, mapId);
            var uid = sEntMan.SpawnEntity(null, coords);
            var comp = sEntMan.EnsureComponent<RobustCloneableTestComponent>(uid);
            comp.TestClass.IntValue = 50;
            comp.TestStruct.IntValue = 60;
            comp.NullableTestStruct = new() { IntValue = 70 };
        });

        // Connect client.
        Assert.DoesNotThrow(() => client.SetConnectTarget(server));
        await client.WaitPost(() => cNetMan.ClientConnect(null!, 0, null!));

        async Task RunTicks()
        {
            for (int i = 0; i < 10; i++)
            {
                await server.WaitRunTicks(1);
                await client.WaitRunTicks(1);
            }
        }
        await RunTicks();

        EntityUid player = default;
        await server.WaitPost(() =>
        {
            var coords = new MapCoordinates(0, 0, mapId);
            player = sEntMan.SpawnEntity(null, coords);
            var session = sPlayerMan.Sessions.First();
            server.PlayerMan.SetAttachedEntity(session, player);
            sPlayerMan.JoinGame(session);
        });

        await RunTicks();

        await server.WaitAssertion(() =>
        {
            Assert.That(cNetMan.IsConnected, Is.True);
            var ents = cEntMan.AllEntities<RobustCloneableTestComponent>().ToList();
            Assert.That(ents, Has.Count.EqualTo(1));
            var testEnt = ents[0];

            Assert.That(testEnt.Comp.TestClass.IntValue, Is.EqualTo(50));
            Assert.That(testEnt.Comp.TestStruct.IntValue, Is.EqualTo(60));
            Assert.That(testEnt.Comp.NullableTestStruct, Is.Not.Null);
            Assert.That(testEnt.Comp.NullableTestStruct!.Value.IntValue, Is.EqualTo(70));
        });

        // Disconnect client
        await client.WaitPost(() => cNetMan.ClientDisconnect(""));
        await server.WaitRunTicks(5);
        await client.WaitRunTicks(5);
    }
}
