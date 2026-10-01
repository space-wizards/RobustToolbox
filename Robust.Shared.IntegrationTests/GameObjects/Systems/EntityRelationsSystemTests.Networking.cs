using System.Numerics;
using NUnit.Framework;
using Robust.Client.Timing;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Robust.Shared.IntegrationTests.GameObjects.Systems;

// This partial class tests specifically for proper networking of the EntityRelations.
internal sealed partial class EntityRelationsSystemTests
{
    [Test]
    public async Task Relations_BasicNetworking()
    {
        var sOptions = new ServerIntegrationOptions();
        sOptions.Pool = false;
        sOptions.BeforeRegisterComponents += () =>
        {
            var fact = IoCManager.Resolve<IComponentFactory>();
            fact.RegisterClass<EntityRelationsTestComponent>();
        };
        sOptions.BeforeStart += () =>
        {
            var sysMan = IoCManager.Resolve<IEntitySystemManager>();
            sysMan.LoadExtraSystemType<EntityRelationsTestSystem>();
        };

        var cOptions = new ClientIntegrationOptions();
        cOptions.Pool = false;
        cOptions.BeforeRegisterComponents += () =>
        {
            var fact = IoCManager.Resolve<IComponentFactory>();
            fact.RegisterClass<EntityRelationsTestComponent>();
        };
        cOptions.BeforeStart += () =>
        {
            var sysMan = IoCManager.Resolve<IEntitySystemManager>();
            sysMan.LoadExtraSystemType<EntityRelationsTestSystem>();
        };

        var server = StartServer(sOptions);
        var client = StartClient(cOptions);

        await Task.WhenAll(client.WaitIdleAsync(), server.WaitIdleAsync());

        var clientNetManager = client.ResolveDependency<IClientNetManager>();

        Assert.DoesNotThrow(() => client.SetConnectTarget(server));
        client.Post(() =>
        {
            clientNetManager.ClientConnect(null!, 0, null!);
        });

        for (int i = 0; i < 10; i++)
        {
            await server.WaitRunTicks(1);
            await client.WaitRunTicks(1);
        }

        var sEntMan = server.Resolve<IEntityManager>();
        var cEntMan = client.Resolve<IEntityManager>();
        var sPlayerManager = server.ResolveDependency<IPlayerManager>();

        var clientTime = client.ResolveDependency<IClientGameTiming>();
        var serverTime = server.ResolveDependency<IGameTiming>();

        MapId mapId;
        var mapPos = MapCoordinates.Nullspace;

        EntityUid sEntityUid = default!;
        NetEntity netEntTarget = default;
        NetEntity netEntOwner = default;

        await server.WaitAssertion(() =>
        {
            sEntMan.System<SharedMapSystem>().CreateMap(out mapId);
            mapPos = new MapCoordinates(new Vector2(0, 0), mapId);

            sEntityUid = sEntMan.Spawn(null, mapPos);

            // Setup PVS
            sEntMan.AddComponent<EyeComponent>(sEntityUid);
            var player = sPlayerManager.Sessions.First();
            server.PlayerMan.SetAttachedEntity(player, sEntityUid);
            sPlayerManager.JoinGame(player);
        });

        await server.WaitAssertion(() =>
        {
            var ownerEnt = sEntMan.Spawn(null, mapPos);
            var targetEnt = sEntMan.Spawn(null, mapPos);

            var testCompServer = sEntMan.EnsureComponent<EntityRelationsTestComponent>(ownerEnt);

            sEntMan.DirtyEntity(ownerEnt);
            sEntMan.DirtyEntity(targetEnt);

            SetRelations(ownerEnt, testCompServer, targetEnt, sEntMan);

            var relationsCompServer = sEntMan.GetComponent<EntityRelationsComponent>(ownerEnt);
            var targetRelationsCompServer = sEntMan.GetComponent<EntityRelationsComponent>(targetEnt);

            sEntMan.Dirty(ownerEnt, relationsCompServer);
            sEntMan.Dirty(ownerEnt, testCompServer);
            sEntMan.Dirty(targetEnt, targetRelationsCompServer);

            netEntOwner = sEntMan.GetNetEntity(ownerEnt);
            netEntTarget = sEntMan.GetNetEntity(targetEnt);
        });

        for (int i = 0; i < 10; i++)
        {
            await server.WaitRunTicks(1);
            await client.WaitRunTicks(1);
        }

        await server.WaitRunTicks(1);

        while (clientTime.LastRealTick < serverTime.CurTick - 1)
        {
            await client.WaitRunTicks(1);
        }

        var ownerEntC = cEntMan.GetEntity(netEntOwner);
        var targetEntC = cEntMan.GetEntity(netEntTarget);

        await client.WaitAssertion(() =>
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(cEntMan.HasComponent<EntityRelationsComponent>(ownerEntC));
                Assert.That(cEntMan.HasComponent<EntityRelationsComponent>(targetEntC));
            }

            var testComp = cEntMan.GetComponent<EntityRelationsTestComponent>(ownerEntC);
            var relationsComp = cEntMan.GetComponent<EntityRelationsComponent>(ownerEntC);
            var targetRelationsComp = cEntMan.GetComponent<EntityRelationsComponent>(targetEntC);

            using (Assert.EnterMultipleScope())
            {
                AssertTestCompTarget(testComp, targetEntC);

                Assert.That(relationsComp.Relations, Has.Count.EqualTo(EntityRelationsTestComponent.FieldCount));
                Assert.That(targetRelationsComp.Relations, Has.Count.EqualTo(EntityRelationsTestComponent.FieldCount));
            }
        });

        await client.WaitPost(() => clientNetManager.ClientDisconnect(""));
        await server.WaitRunTicks(5);
        await client.WaitRunTicks(5);
    }
}
