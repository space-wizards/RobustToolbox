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
    private static readonly Vector2 TargetPosition = new(3, 4);

    [Test]
    public async Task PredictedQueueDeletionRollsBack()
    {
        await using var pair = await StartConnectedPair();

        var (server, client, targetNet, clientTarget, clientParent) = await SetupTarget(pair.Server, pair.Client);

        await client.WaitPost(() =>
        {
            Assert.That(client.EntMan.EntityExists(clientTarget), Is.True);
            Assert.That(client.EntMan.IsQueuedForDeletion(clientTarget), Is.False);

            client.EntMan.RaisePredictiveEvent(new PredictDeleteMessage(targetNet, PredictedDeleteMode.Queue, 2));

            Assert.That(client.EntMan.EntityExists(clientTarget), Is.True);
            Assert.That(client.EntMan.IsQueuedForDeletion(clientTarget), Is.True);
        });

        await client.WaitRunTicks(2);

        await client.WaitPost(() =>
        {
            Assert.That(((ClientEntityManager) client.EntMan).IsPredictedDetached(clientTarget), Is.True);
            Assert.That(client.EntMan.IsQueuedForDeletion(clientTarget), Is.True);
        });

        await RunTicksSync(server, client, 10);

        await AssertRolledBack(client, clientTarget, clientParent);
    }

    [Test]
    public async Task DirectPredictedDeletionRollsBack()
    {
        await using var pair = await StartConnectedPair();

        var (server, client, targetNet, clientTarget, clientParent) = await SetupTarget(pair.Server, pair.Client);

        await client.WaitPost(() =>
        {
            client.EntMan.RaisePredictiveEvent(new PredictDeleteMessage(targetNet, PredictedDeleteMode.Direct, 0));
        });

        await client.WaitRunTicks(2);

        await client.WaitPost(() =>
        {
            Assert.That(client.EntMan.EntityExists(clientTarget), Is.True);
            Assert.That(((ClientEntityManager) client.EntMan).IsPredictedDetached(clientTarget), Is.True);
            Assert.That(client.EntMan.IsQueuedForDeletion(clientTarget), Is.True);
        });

        await RunTicksSync(server, client, 10);

        await AssertRolledBack(client, clientTarget, clientParent);
    }

    [Test]
    public async Task RepeatedPredictedDeletionDoesNotQueueOrDetachRepeatedly()
    {
        await using var pair = await StartConnectedPair();

        var (_, client, _, clientTarget, _) = await SetupTarget(pair.Server, pair.Client);

        await client.WaitPost(() =>
        {
            var ent = new Entity<MetaDataComponent?, TransformComponent?>(clientTarget, null, null);

            client.EntMan.PredictedDeleteEntity(ent);
            var xform = client.EntMan.GetComponent<TransformComponent>(clientTarget);
            var firstDetachTick = xform.LastModifiedTick;

            Assert.That(((ClientEntityManager) client.EntMan).IsPredictedDetached(clientTarget), Is.True);
            Assert.That(client.EntMan.IsQueuedForDeletion(clientTarget), Is.True);

            client.EntMan.PredictedDeleteEntity(ent);

            Assert.That(((ClientEntityManager) client.EntMan).IsPredictedDetached(clientTarget), Is.True);
            Assert.That(client.EntMan.IsQueuedForDeletion(clientTarget), Is.True);
            Assert.That(xform.LastModifiedTick, Is.EqualTo(firstDetachTick));
        });
    }

    [Test]
    public async Task PredictedDeletionFollowedByAuthoritativeDeletionDeletesEntity()
    {
        await using var pair = await StartConnectedPair();

        var (server, client, targetNet, clientTarget, _) = await SetupTarget(pair.Server, pair.Client);
        var serverTarget = server.EntMan.GetEntity(targetNet);

        await client.WaitPost(() =>
        {
            client.EntMan.RaisePredictiveEvent(new PredictDeleteMessage(targetNet, PredictedDeleteMode.Direct, 0));
        });

        await client.WaitRunTicks(2);

        await client.WaitPost(() =>
        {
            Assert.That(((ClientEntityManager) client.EntMan).IsPredictedDetached(clientTarget), Is.True);
            Assert.That(client.EntMan.IsQueuedForDeletion(clientTarget), Is.True);
        });

        await server.WaitPost(() => server.EntMan.DeleteEntity(serverTarget));
        await RunTicksSync(server, client, 10);

        await client.WaitPost(() =>
        {
            Assert.That(client.EntMan.EntityExists(clientTarget), Is.False);
            Assert.That(((ClientEntityManager) client.EntMan).IsPredictedDetached(clientTarget), Is.False);
            Assert.That(client.EntMan.IsQueuedForDeletion(clientTarget), Is.False);
        });
    }

    [Test]
    public async Task PredictedDeletionRollbackRestoresTransformState()
    {
        await using var pair = await StartConnectedPair();

        var (server, client, _, clientTarget, clientParent) = await SetupTarget(pair.Server, pair.Client);

        await client.WaitPost(() =>
        {
            var ent = new Entity<MetaDataComponent?, TransformComponent?>(clientTarget, null, null);
            client.EntMan.PredictedDeleteEntity(ent);

            var meta = client.EntMan.GetComponent<MetaDataComponent>(clientTarget);
            var xform = client.EntMan.GetComponent<TransformComponent>(clientTarget);

            Assert.That(meta.Flags & MetaDataFlags.Detached, Is.EqualTo(MetaDataFlags.Detached));
            Assert.That(xform.ParentUid, Is.EqualTo(EntityUid.Invalid));
        });

        await RunTicksSync(server, client, 10);

        await AssertRolledBack(client, clientTarget, clientParent);
    }

    private async Task<(
        ServerIntegrationInstance Server,
        ClientIntegrationInstance Client,
        NetEntity TargetNet,
        EntityUid ClientTarget,
        EntityUid ClientParent)> SetupTarget(
        ServerIntegrationInstance server,
        ClientIntegrationInstance client)
    {
        await server.WaitPost(() => server.CfgMan.SetCVar(CVars.NetPVS, true));

        EntityUid map = default;
        EntityUid player = default;
        EntityUid serverParent = default;
        NetEntity targetNet = default;
        NetEntity parentNet = default;

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

            serverParent = server.EntMan.SpawnAttachedTo(null, coords);
            var target = server.EntMan.SpawnAttachedTo(null, new EntityCoordinates(serverParent, TargetPosition));
            parentNet = server.EntMan.GetNetEntity(serverParent);
            targetNet = server.EntMan.GetNetEntity(target);
        });

        await RunTicksSync(server, client, 10);

        var clientTarget = client.EntMan.GetEntity(targetNet);
        var clientParent = client.EntMan.GetEntity(parentNet);

        await client.WaitPost(() =>
        {
            Assert.That(client.EntMan.EntityExists(clientTarget), Is.True);
            var xform = client.EntMan.GetComponent<TransformComponent>(clientTarget);

            Assert.That(xform.ParentUid, Is.EqualTo(clientParent));
            Assert.That(xform.LocalPosition, Is.EqualTo(TargetPosition));
        });

        return (server, client, targetNet, clientTarget, clientParent);
    }

    private static async Task AssertRolledBack(
        ClientIntegrationInstance client,
        EntityUid clientTarget,
        EntityUid clientParent)
    {
        await client.WaitPost(() =>
        {
            Assert.That(client.EntMan.EntityExists(clientTarget), Is.True);
            Assert.That(client.EntMan.Deleted(clientTarget), Is.False);
            Assert.That(client.EntMan.IsQueuedForDeletion(clientTarget), Is.False);

            var meta = client.EntMan.GetComponent<MetaDataComponent>(clientTarget);
            var xform = client.EntMan.GetComponent<TransformComponent>(clientTarget);

            Assert.That(((ClientEntityManager) client.EntMan).IsPredictedDetached(clientTarget), Is.False);
            Assert.That(meta.Flags & MetaDataFlags.Detached, Is.EqualTo(MetaDataFlags.None));
            Assert.That(xform.ParentUid, Is.EqualTo(clientParent));
            Assert.That(xform.LocalPosition, Is.EqualTo(TargetPosition));
        });
    }

    public sealed partial class PredictedDeletionTestSystem : EntitySystem
    {
        [Dependency] private INetManager _net = default!;
        [Dependency] private IGameTiming _timing = default!;

        private NetEntity _entity;
        private PredictedDeleteMode _mode;
        private int _remainingPredictionTicks;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeAllEvent<PredictDeleteMessage>(OnPredictDelete);
        }

        private void OnPredictDelete(PredictDeleteMessage ev, EntitySessionEventArgs args)
        {
            if (!_net.IsClient || !_timing.InPrediction)
                return;

            _entity = ev.Entity;
            _mode = ev.Mode;
            _remainingPredictionTicks = ev.PredictionTicks;
            PredictDeletion();
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            if (!_net.IsClient || !_timing.InPrediction || _remainingPredictionTicks <= 0)
                return;

            PredictDeletion();
        }

        private void PredictDeletion()
        {
            _remainingPredictionTicks--;

            if (!TryGetEntity(_entity, out var uid))
                return;

            if (_mode == PredictedDeleteMode.Direct)
                PredictedDel(uid.Value);
            else
                PredictedQueueDel(uid.Value);
        }
    }

    [Serializable, NetSerializable]
    public sealed class PredictDeleteMessage(NetEntity entity, PredictedDeleteMode mode, int predictionTicks) : EntityEventArgs
    {
        public NetEntity Entity { get; } = entity;
        public PredictedDeleteMode Mode { get; } = mode;
        public int PredictionTicks { get; } = predictionTicks;
    }

    [Serializable, NetSerializable]
    public enum PredictedDeleteMode : byte
    {
        Queue,
        Direct
    }
}
