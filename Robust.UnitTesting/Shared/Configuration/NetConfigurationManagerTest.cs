using NUnit.Framework;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Player;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Robust.UnitTesting.Shared.Configuration;

[TestFixture]
[Parallelizable(ParallelScope.All)]
[TestOf(typeof(NetConfigurationManager))]
internal sealed class NetConfigurationManagerTest : RobustIntegrationTest
{
    [Test]
    public async Task TestSubscribeUnsubscribe()
    {
        using var server = StartServer();
        using var client = StartClient();

        await Task.WhenAll(client.WaitIdleAsync(), server.WaitIdleAsync());

        var serverNetConfiguration = server.ResolveDependency<INetConfigurationManager>();
        var clientNetConfiguration = client.ResolveDependency<INetConfigurationManager>();

        // CVar def consts
        const string CVarName = "net.foo_bar";
        const CVar CVarFlags = CVar.CLIENT | CVar.REPLICATED;
        const int DefaultValue = 1;

        // setup debug CVar
        server.Post(() =>
        {
            serverNetConfiguration.RegisterCVar(CVarName, DefaultValue, CVarFlags);
        });

        client.Post(() =>
        {
            clientNetConfiguration.RegisterCVar(CVarName, DefaultValue, CVarFlags);
        });

        await Task.WhenAll(client.WaitIdleAsync(), server.WaitIdleAsync());
        // connect client
        Assert.DoesNotThrow(() => client.SetConnectTarget(server));
        await client.WaitPost(() =>
        {
            client.Resolve<IClientNetManager>().ClientConnect(null!, 0, null!);
        });

        await RunTicks(server, client);

        var session = server.PlayerMan.Sessions.First();

        Assert.Multiple(() =>
        {
            Assert.That(serverNetConfiguration.GetClientCVar<int>(session.Channel, CVarName), Is.EqualTo(DefaultValue));
            Assert.That(clientNetConfiguration.GetClientCVar<int>(session.Channel, CVarName), Is.EqualTo(DefaultValue));
        });


        ICommonSession? subscribeSession = default!;
        var SubscribeValue = 0;
        var timesRan = 0;
        void ClientValueChanged(int value, ICommonSession session)
        {
            timesRan++;
            SubscribeValue = value;
            subscribeSession = session;
        }

        // actually subscribe
        server.Post(() =>
        {
            serverNetConfiguration.OnClientCVarChanges<int>(CVarName, ClientValueChanged, null);
        });

        // set new value in client
        const int NewValue = 8;
        Assert.That(NewValue, Is.Not.EqualTo(DefaultValue));
        client.Post(() =>
        {
            clientNetConfiguration.SetCVar(CVarName, NewValue);
        });

        await RunTicks(server, client);

        // assert handling cvar change and receiving event
        Assert.Multiple(() =>
        {
            Assert.That(clientNetConfiguration.GetClientCVar<int>(session.Channel, CVarName), Is.EqualTo(NewValue));
            Assert.That(serverNetConfiguration.GetClientCVar<int>(session.Channel, CVarName), Is.EqualTo(NewValue));

            Assert.That(timesRan, Is.EqualTo(1));
            Assert.That(SubscribeValue, Is.EqualTo(NewValue));
            Assert.That(subscribeSession, Is.EqualTo(session));
        });

        // unsubscribe
        server.Post(() =>
        {
            serverNetConfiguration.UnsubClientCVarChanges<int>(CVarName, ClientValueChanged, null);
        });

        // set new value in client
        const int UnsubValue = 16;
        Assert.That(UnsubValue, Is.Not.EqualTo(NewValue));
        client.Post(() =>
        {
            clientNetConfiguration.SetCVar(CVarName, UnsubValue);
        });

        await RunTicks(server, client);

        // assert handling cvar change and unsubscribing
        Assert.Multiple(() =>
        {
            Assert.That(clientNetConfiguration.GetClientCVar<int>(session.Channel, CVarName), Is.EqualTo(UnsubValue));
            Assert.That(serverNetConfiguration.GetClientCVar<int>(session.Channel, CVarName), Is.EqualTo(UnsubValue));

            Assert.That(timesRan, Is.EqualTo(1));
            Assert.That(SubscribeValue, Is.EqualTo(NewValue));
        });

        // now check how disconnect subscribe works
        ICommonSession? disconnectSession = null;
        var disconnectTimesRun = 0;
        void OnDisconnect(ICommonSession session)
        {
            disconnectSession = session;
            disconnectTimesRun++;
        }

        server.Post(() =>
        {
            serverNetConfiguration.OnClientCVarChanges<int>(CVarName, ClientValueChanged, OnDisconnect);
        });

        // change value in client
        client.Post(() =>
        {
            clientNetConfiguration.SetCVar(CVarName, DefaultValue);
        });

        await RunTicks(server, client);

        // disconnect event don't fire on changing CVar
        Assert.Multiple(() =>
        {
            Assert.That(disconnectTimesRun, Is.EqualTo(0));
            Assert.That(disconnectSession, Is.EqualTo(null));
        });

        // disconnect
        await client.WaitPost(() => client.Resolve<IClientNetManager>().ClientDisconnect(""));

        await RunTicks(server, client);

        Assert.Multiple(() =>
        {
            Assert.That(disconnectTimesRun, Is.EqualTo(1));
            Assert.That(disconnectSession, Is.EqualTo(session));
        });

        // reset for proper handling assertions and prevent colliding with new session
        disconnectSession = null;
        // again connect client
        Assert.DoesNotThrow(() => client.SetConnectTarget(server));
        await client.WaitPost(() =>
        {
            client.Resolve<IClientNetManager>().ClientConnect(null!, 0, null!);
        });

        await RunTicks(server, client);
        session = server.PlayerMan.Sessions.First();

        // also check if somehow disconnectSession not null and have a strange session
        Assert.Multiple(() =>
        {
            Assert.That(disconnectTimesRun, Is.EqualTo(1));
            Assert.That(disconnectSession, Is.EqualTo(null));
            Assert.That(disconnectSession, Is.Not.EqualTo(session));
        });

        // now unsubscribe
        server.Post(() =>
        {
            serverNetConfiguration.UnsubClientCVarChanges<int>(CVarName, ClientValueChanged, OnDisconnect);
        });

        await RunTicks(server, client);

        // for current logic this shouldn't fire disconnect event.
        Assert.Multiple(() =>
        {
            Assert.That(disconnectTimesRun, Is.EqualTo(1));
            Assert.That(disconnectSession, Is.EqualTo(null));
            Assert.That(disconnectSession, Is.Not.EqualTo(session));
        });

        // disconnect
        await client.WaitPost(() => client.Resolve<IClientNetManager>().ClientDisconnect(""));

        await RunTicks(server, client);

        // assert that unsubscribed event wasn't fired
        Assert.Multiple(() =>
        {
            Assert.That(disconnectTimesRun, Is.EqualTo(1));
            Assert.That(disconnectSession, Is.EqualTo(null));
            Assert.That(disconnectSession, Is.Not.EqualTo(session));
        });

    }

    [Test]
    public async Task TestSubscribeUnsubscribeMultipleClients()
    {
        const int ClientAmount = 4;
        using var server = StartServer();
        ClientIntegrationInstance[] clients = new ClientIntegrationInstance[ClientAmount];

        // CVar def consts
        const string CVarName = "net.foo_bar";
        const CVar CVarFlags = CVar.CLIENT | CVar.REPLICATED;
        const int DefaultValue = -1;

        HashSet<ICommonSession> eventSessions = [];
        Dictionary<ICommonSession, int> clientValues = [];
        void ClientValueChanged(int value, ICommonSession session)
        {
            eventSessions.Add(session);
            clientValues[session] = value;
        }

        HashSet<ICommonSession> eventDisconnectedSessions = [];
        void OnDisconnect(ICommonSession session)
        {
            eventDisconnectedSessions.Add(session);
        }

        // setup debug CVar
        server.Post(() =>
        {
            server.Resolve<INetConfigurationManager>().RegisterCVar(CVarName, DefaultValue, CVarFlags);
            server.Resolve<INetConfigurationManager>().OnClientCVarChanges<int>(CVarName, ClientValueChanged, OnDisconnect);

        });

        // for this collection I need order of adding
        List<ICommonSession> clientSessions = new();
        for (int i = 0; i < ClientAmount; i++)
        {
            var client = StartClient();
            clients[i] = client;

            client.Post(() =>
            {
                client.Resolve<INetConfigurationManager>().RegisterCVar(CVarName, DefaultValue, CVarFlags);
            });

            await Task.WhenAll(client.WaitIdleAsync(), server.WaitIdleAsync());

            Assert.DoesNotThrow(() => client.SetConnectTarget(server));
            await client.WaitPost(() =>
            {
                client.Resolve<IClientNetManager>().ClientConnect(null!, 0, null!);
            });

            await RunTicks(server, client);

            clientSessions.Add(server.PlayerMan.Sessions.Except(clientSessions).First());
        }

        // check that we got correct sessions
        Assert.That(clientSessions.Count, Is.EqualTo(ClientAmount));

        // server invoke subscribed events of replicated CVar on client connect
        Assert.Multiple(() =>
        {
            Assert.That(eventSessions.Count, Is.EqualTo(ClientAmount));
            Assert.That(clientValues.Count, Is.EqualTo(ClientAmount));

            Assert.That(clientValues.Values.Distinct().Count, Is.EqualTo(1));
            Assert.That(clientValues.Values.Distinct().First(), Is.EqualTo(DefaultValue));
        });

        eventSessions.Clear();
        clientValues.Clear();

        // try to change CVar on every client EXCEPT last one
        for (int i = 0; i < ClientAmount - 1; i++)
        {
            var client = clients[i];
            // set new value in client
            Assert.That(i, Is.Not.EqualTo(DefaultValue));
            client.Post(() =>
            {
                client.Resolve<INetConfigurationManager>().SetCVar(CVarName, i);
            });

            await RunTicks(server, client);
        }

        Assert.Multiple(() =>
        {
            // session events worked correctly (reminder: last one haven't changed it CVar)
            Assert.That(eventSessions.Count, Is.EqualTo(ClientAmount - 1));
            Assert.That(eventDisconnectedSessions.Count, Is.EqualTo(0));

            for (int i = 0; i < ClientAmount - 1; i++)
            {
                var currentSession = clientSessions[i];

                int? value = null;
                Assert.DoesNotThrow(() => value = clientValues[currentSession]);

                // check if session wasn't messed up
                Assert.That(value, Is.EqualTo(i));
            }

            var lastSession = clientSessions[ClientAmount - 1];
            Assert.That(clientValues.ContainsKey(lastSession), Is.EqualTo(false));
        });

        for (int i = 0; i < ClientAmount; i++)
        {
            var client = clients[i];

            await client.WaitPost(() => client.Resolve<IClientNetManager>().ClientDisconnect(""));

            await RunTicks(server, client);

            // assert that every disconnect result in event raising
            Assert.That(eventDisconnectedSessions.Count, Is.EqualTo(i + 1));
        }

        // we received same sessions EXCEPT last one
        Assert.That(eventDisconnectedSessions.Except(eventSessions), Is.EqualTo(new HashSet<ICommonSession> { clientSessions[ClientAmount - 1] }));
        Assert.That(clientSessions, Is.EqualTo(eventDisconnectedSessions));
    }

    private async Task RunTicks(IntegrationInstance server, IntegrationInstance client, int numberOfTicks = 5)
    {
        for (int i = 0; i < numberOfTicks; i++)
        {
            await server.WaitRunTicks(1);
            await client.WaitRunTicks(1);
        }
    }
}

