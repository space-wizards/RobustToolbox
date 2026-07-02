using System.Numerics;
using NUnit.Framework;
using Robust.Client.GameObjects;
using Robust.Shared;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Robust.UnitTesting.Shared.GameState;

internal sealed partial class PredictedDeletionTests : RobustIntegrationTest
{
    [Test]
    public async Task PredictedQueueDeletionRollsBack()
    {
        var serverOpts = new ServerIntegrationOptions { Pool = false };
        var clientOpts = new ClientIntegrationOptions { Pool = false };
        await using var pair = await StartConnectedPair(serverOpts, clientOpts);

        var server = pair.Server;
        var client = pair.Client;

        await server.WaitPost(() => server.CfgMan.SetCVar(CVars.NetPVS, true));

        EntityUid map = default;
        EntityUid player = default;
        EntityUid serverTarget = default;
        NetEntity targetNet = default;

        await server.WaitPost(() =>
        {
            map = server.System<SharedMapSystem>().CreateMap();
        });

        await RunTicksSync(server, client, 10);

        await server.WaitPost(() =>
        {
            var coords = new EntityCoordinates(map, Vector2.Zero);

            player = server.EntMan.SpawnAttachedTo(null, coords);
            var session = server.PlayerMan.Sessions.First();
            server.PlayerMan.SetAttachedEntity(session, player);
            server.PlayerMan.JoinGame(session);

            serverTarget = server.EntMan.SpawnAttachedTo(null, coords);
            targetNet = server.EntMan.GetNetEntity(serverTarget);
        });

        await RunTicksSync(server, client, 10);

        var clientTarget = client.EntMan.GetEntity(targetNet);
        var clientMap = client.EntMan.GetEntity(server.EntMan.GetNetEntity(map));

        await client.WaitPost(() =>
        {
            Assert.That(client.EntMan.EntityExists(clientTarget), Is.True);
            Assert.That(client.EntMan.IsQueuedForDeletion(clientTarget), Is.False);

            client.EntMan.RaisePredictiveEvent(new PredictQueueDeleteMessage(targetNet, 2));

            Assert.That(client.EntMan.EntityExists(clientTarget), Is.True);
            Assert.That(client.EntMan.IsQueuedForDeletion(clientTarget), Is.True);
        });

        await client.WaitRunTicks(2);

        await client.WaitPost(() =>
        {
            Assert.That(((ClientEntityManager) client.EntMan).IsPredictedDetached(clientTarget), Is.True);
        });

        await RunTicksSync(server, client, 10);

        await client.WaitPost(() =>
        {
            Assert.That(client.EntMan.EntityExists(clientTarget), Is.True);
            Assert.That(client.EntMan.Deleted(clientTarget), Is.False);
            Assert.That(client.EntMan.IsQueuedForDeletion(clientTarget), Is.False);

            var meta = client.EntMan.GetComponent<MetaDataComponent>(clientTarget);
            var xform = client.EntMan.GetComponent<TransformComponent>(clientTarget);

            Assert.That(meta.Flags & MetaDataFlags.Detached, Is.EqualTo(MetaDataFlags.None));
            Assert.That(xform.ParentUid, Is.EqualTo(clientMap));
        });
    }

    public sealed partial class PredictedDeletionTestSystem : EntitySystem
    {
        [Dependency] private INetManager _net = default!;
        [Dependency] private IGameTiming _timing = default!;

        private NetEntity _entity;
        private int _remainingPredictionTicks;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeAllEvent<PredictQueueDeleteMessage>(OnPredictQueueDelete);
        }

        private void OnPredictQueueDelete(PredictQueueDeleteMessage ev, EntitySessionEventArgs args)
        {
            if (!_net.IsClient || !_timing.InPrediction)
                return;

            _entity = ev.Entity;
            _remainingPredictionTicks = ev.PredictionTicks;
            QueuePredictedDeletion();
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            if (!_net.IsClient || !_timing.InPrediction || _remainingPredictionTicks <= 0)
                return;

            QueuePredictedDeletion();
        }

        private void QueuePredictedDeletion()
        {
            _remainingPredictionTicks--;
            PredictedQueueDel(GetEntity(_entity));
        }
    }

    [Serializable, NetSerializable]
    public sealed class PredictQueueDeleteMessage(NetEntity entity, int predictionTicks) : EntityEventArgs
    {
        public NetEntity Entity { get; } = entity;
        public int PredictionTicks { get; } = predictionTicks;
    }
}
