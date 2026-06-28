using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization;

namespace Robust.UnitTesting.Shared.Networking;

[TestFixture]
internal sealed class MsgEntityTests : RobustIntegrationTest
{
    [Test]
    public async Task UnhandledSystemMessageIsDroppedBeforeDeserializationValidation()
    {
        await using var pair = await StartConnectedPair(
            new ServerIntegrationOptions {Pool = false},
            new ClientIntegrationOptions {Pool = false});
        await RunTicksSync(pair.Server, pair.Client, 10);

        await pair.Client.WaitPost(() =>
        {
            var entMan = pair.Client.Resolve<IEntityManager>();
            entMan.EntityNetManager.SendSystemNetworkMessage(new LengthLimitedTestEvent {Value = "ABC"});
        });

        await RunTicksSync(pair.Server, pair.Client, 5);

        await pair.Server.WaitAssertion(() =>
        {
            Assert.That(pair.Server.NetMan.IsConnected, Is.True);
        });
    }

    [Test]
    public async Task HandledSystemMessageIsDeserialized()
    {
        LengthLimitedTestEventSystem.LastValue = null;

        var options = new ServerIntegrationOptions {Pool = false};
        options.BeforeStartServices += deps =>
        {
            deps.Resolve<IEntitySystemManager>().LoadExtraSystemType<LengthLimitedTestEventSystem>();
        };

        await using var pair = await StartConnectedPair(
            options,
            new ClientIntegrationOptions {Pool = false});

        await RunTicksSync(pair.Server, pair.Client, 10);

        await pair.Client.WaitPost(() =>
        {
            var entMan = pair.Client.Resolve<IEntityManager>();
            entMan.EntityNetManager.SendSystemNetworkMessage(new LengthLimitedTestEvent {Value = "OK"});
        });

        await RunTicksSync(pair.Server, pair.Client, 5);

        await pair.Server.WaitAssertion(() =>
        {
            Assert.That(LengthLimitedTestEventSystem.LastValue, Is.EqualTo("OK"));
        });
    }

    [Serializable, NetSerializable]
    private sealed class LengthLimitedTestEvent : EntityEventArgs
    {
        [NetMaxLength(2)]
        public string Value = string.Empty;
    }

    [Reflect(false)]
    private sealed class LengthLimitedTestEventSystem : EntitySystem
    {
        public static string? LastValue;

        public override void Initialize()
        {
            SubscribeNetworkEvent<LengthLimitedTestEvent>(OnLengthLimited);
        }

        private void OnLengthLimited(LengthLimitedTestEvent ev)
        {
            LastValue = ev.Value;
        }
    }
}
