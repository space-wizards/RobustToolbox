using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.UnitTesting.Pool;

/// <summary>
/// This object wraps a pooled server+client pair.
/// </summary>
public abstract partial class TestPair<TServer, TClient> : ITestPair, IAsyncDisposable
    where TServer : IServerIntegrationInstance
    where TClient : IClientIntegrationInstance
{
    public int Id { get; internal set; }
    protected BasePoolManager Manager = default!;
    public PairState State { get; private set; } = PairState.Ready;
    public bool Initialized { get; private set; }
    protected TextWriter TestOut = default!;
    public Stopwatch Watch { get; } = new();
    public List<string> TestHistory { get; } = new();
    public PairSettings Settings { get; set; } = default!;

    public readonly PoolTestLogHandler ServerLogHandler = new("SERVER");
    public readonly PoolTestLogHandler ClientLogHandler = new("CLIENT");
    public TestMapData? TestMap;

    private int _nextServerSeed;
    private int _nextClientSeed;

    public int ServerSeed { get; set; }
    public int ClientSeed { get; set; }

    public TServer Server { get; private set; } = default!;
    public TClient Client { get; private set; } = default!;

    public ICommonSession? Player => Server.PlayerMan.SessionsDict.GetValueOrDefault(Client.User ?? default);

    private Dictionary<Type, HashSet<string>> _loadedPrototypes = new();
    private HashSet<string> _loadedEntityPrototypes = new();
    protected readonly Dictionary<string, object> ModifiedClientCvars = new();
    protected readonly Dictionary<string, object> ModifiedServerCvars = new();

    public async Task LoadPrototypes(List<string> prototypes)
    {
        await LoadPrototypes(Server, prototypes);
        await LoadPrototypes(Client, prototypes);
    }

    public async Task Init(
        int id,
        BasePoolManager manager,
        PairSettings settings,
        TextWriter testOut)
    {
        if (Initialized)
            throw new InvalidOperationException("Already initialized");

        Id = id;
        Manager = manager;
        Settings = settings;
        Initialized = true;

        ClientLogHandler.ActivateContext(testOut);
        ServerLogHandler.ActivateContext(testOut);
        Client = await GenerateClient();
        Server = await GenerateServer();
        ActivateContext(testOut);
        await ApplySettings(settings);

        Client.CfgMan.OnCVarValueChanged += OnClientCvarChanged;
        Server.CfgMan.OnCVarValueChanged += OnServerCvarChanged;

        if (!settings.NoLoadTestPrototypes)
            await LoadPrototypes(Manager.TestPrototypes);

        var cRand = Client.Resolve<IRobustRandom>();
        var sRand = Server.Resolve<IRobustRandom>();
        _nextClientSeed = cRand.Next();
        _nextServerSeed = sRand.Next();

        await Initialize();

        // Always initially connect clients.
        // This is done in case the server does randomization when client first connects
        // This is to try and prevent issues where if the first test that connects the client is consistently some test
        // that uses a fixed seed, it would effectively prevent the initial configuration from being randomized.
        await Connect();

        if (!Settings.Connected)
            await Disconnect("Initial disconnect");
    }

    protected virtual Task Initialize()
    {
        return Task.CompletedTask;
    }

    protected abstract Task<TClient> GenerateClient();
    protected abstract Task<TServer> GenerateServer();

    public void Kill()
    {
        State = PairState.Dead;
        ServerLogHandler.ShuttingDown = true;
        ClientLogHandler.ShuttingDown = true;
        Server.Dispose();
        Client.Dispose();
    }

    private void ClearContext()
    {
        TestOut = default!;
        ServerLogHandler.ClearContext();
        ClientLogHandler.ClearContext();
    }

    public void ActivateContext(TextWriter testOut)
    {
        TestOut = testOut;
        ServerLogHandler.ActivateContext(testOut);
        ClientLogHandler.ActivateContext(testOut);
    }

    public void Use()
    {
        if (State != PairState.Ready)
            throw new InvalidOperationException($"Pair is not ready to use. State: {State}");
        State = PairState.InUse;
    }

    public void SetupSeed()
    {
        var sRand = Server.Resolve<IRobustRandom>();
        if (Settings.ServerSeed is { } severSeed)
        {
            ServerSeed = severSeed;
            sRand.SetSeed(ServerSeed);
        }
        else
        {
            ServerSeed = _nextServerSeed;
            sRand.SetSeed(ServerSeed);
            _nextServerSeed = sRand.Next();
        }

        var cRand = Client.Resolve<IRobustRandom>();
        if (Settings.ClientSeed is { } clientSeed)
        {
            ClientSeed = clientSeed;
            cRand.SetSeed(ClientSeed);
        }
        else
        {
            ClientSeed = _nextClientSeed;
            cRand.SetSeed(ClientSeed);
            _nextClientSeed = cRand.Next();
        }
    }

    private async Task LoadPrototypes(IIntegrationInstance instance, List<string> prototypes)
    {
        var changed = new Dictionary<Type, HashSet<string>>();
        foreach (var file in prototypes)
        {
            instance.ProtoMan.LoadString(file, changed: changed);
        }

        await instance.WaitPost(() => instance.ProtoMan.ReloadPrototypes(changed));

        foreach (var (kind, ids) in changed)
        {
            _loadedPrototypes.GetOrNew(kind).UnionWith(ids);
        }

        if (_loadedPrototypes.TryGetValue(typeof(EntityPrototype), out var entIds))
            _loadedEntityPrototypes.UnionWith(entIds);
    }

    public void Deconstruct(out TServer server, out TClient client)
    {
        server = Server;
        client = Client;
    }

    private void OnServerCvarChanged(CVarChangeInfo args)
    {
        ModifiedServerCvars.TryAdd(args.Name, args.OldValue);
    }

    private void OnClientCvarChanged(CVarChangeInfo args)
    {
        ModifiedClientCvars.TryAdd(args.Name, args.OldValue);
    }

    public void ClearModifiedCvars()
    {
        ModifiedClientCvars.Clear();
        ModifiedServerCvars.Clear();
    }

    /// <summary>
    /// Reverts any cvars that were modified during a test back to their original values.
    /// </summary>
    public virtual async Task RevertModifiedCvars()
    {
        await Server.WaitPost(() =>
        {
            foreach (var (name, value) in ModifiedServerCvars)
            {
                if (Server.CfgMan.GetCVar(name).Equals(value))
                    continue;

                Server.Log.Info($"Resetting cvar {name} to {value}");
                Server.CfgMan.SetCVar(name, value);
            }

        });

        await Client.WaitPost(() =>
        {
            foreach (var (name, value) in ModifiedClientCvars)
            {
                if (Client.CfgMan.GetCVar(name).Equals(value))
                    continue;

                var flags = Client.CfgMan.GetCVarFlags(name);
                if (flags.HasFlag(CVar.REPLICATED) && flags.HasFlag(CVar.SERVER))
                    continue;

                Client.Log.Info($"Resetting cvar {name} to {value}");
                Client.CfgMan.SetCVar(name, value);
            }
        });

        ClearModifiedCvars();
    }
}
