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
            serverNetConfiguration.OnClientCVarChanges<int>(CVarName, ClientValueChanged);
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
            serverNetConfiguration.UnsubClientCVarChanges<int>(CVarName, ClientValueChanged);
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

        // setup debug CVar
        server.Post(() =>
        {
            server.Resolve<INetConfigurationManager>().RegisterCVar(CVarName, DefaultValue, CVarFlags);
            server.Resolve<INetConfigurationManager>().OnClientCVarChanges<int>(CVarName, ClientValueChanged);

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
            Assert.That(eventSessions, Has.Count.EqualTo(ClientAmount));
            Assert.That(clientValues, Has.Count.EqualTo(ClientAmount));

            Assert.That(clientValues.Values.Distinct().Count(), Is.EqualTo(1));
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
            Assert.That(eventSessions, Has.Count.EqualTo(ClientAmount - 1));

            for (int i = 0; i < ClientAmount - 1; i++)
            {
                var currentSession = clientSessions[i];

                int? value = null;
                Assert.DoesNotThrow(() => value = clientValues[currentSession]);

                // check if session wasn't messed up
                Assert.That(value, Is.EqualTo(i));
            }

            var lastSession = clientSessions[ClientAmount - 1];
            Assert.That(clientValues.ContainsKey(lastSession), Is.False);
        });
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

