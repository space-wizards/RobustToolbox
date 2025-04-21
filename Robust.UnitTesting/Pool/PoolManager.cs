using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Shared;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.UnitTesting.Pool;

public abstract class BasePoolManager
{
    internal abstract void Return(ITestPair pair);
    public abstract Assembly[] ClientAssemblies { get; }
    public abstract Assembly[] ServerAssemblies { get; }
    public readonly List<string> TestPrototypes = new();

    // default cvar overrides to use when creating test pairs.
    public readonly List<(string cvar, string value)> DefaultCvars =
    [
        (CVars.NetPVS.Name, "false"),
        (CVars.ThreadParallelCount.Name, "1"),
        (CVars.ReplayClientRecordingEnabled.Name, "false"),
        (CVars.ReplayServerRecordingEnabled.Name, "false"),
        (CVars.NetBufferSize.Name, "0")
    ];
}

[Virtual]
public class PoolManager<TPair> : BasePoolManager where TPair : class, ITestPair, new()
{
    private int _nextPairId;
    private readonly Lock _pairLock = new();
    private bool _initialized;

    /// <summary>
    /// Set of all pairs, and whether they are currently in-use
    /// </summary>
    protected readonly Dictionary<TPair, bool> Pairs = new();
    private bool _dead;
    private Exception? _poolFailureReason;

    private Assembly[] _clientAssemblies = [];
    private Assembly[] _serverAssemblies = [];

    public override Assembly[] ClientAssemblies => _clientAssemblies;
    public override Assembly[] ServerAssemblies => _serverAssemblies;

    /// <summary>
    /// Initialize the pool manager. Override this to configure what assemblies should get loaded.
    /// </summary>
    public virtual void Startup(params Assembly[] extraAssemblies)
    {
        // By default, load no content assemblies, but make both server & client load the testing assembly.
        Startup([], [], extraAssemblies);
    }

    protected void Startup(Assembly[] clientAssemblies, Assembly[] serverAssemblies, Assembly[] sharedAssemblies)
    {
        if (_initialized)
            throw new InvalidOperationException("Already initialized");

        DebugTools.AssertEqual(clientAssemblies.Intersect(sharedAssemblies).Count(), 0);
        DebugTools.AssertEqual(serverAssemblies.Intersect(sharedAssemblies).Count(), 0);
        DebugTools.AssertEqual(serverAssemblies.Intersect(clientAssemblies).Count(), 0);

        foreach (var assembly in sharedAssemblies)
        {
            DiscoverTestPrototypes(assembly);
        }

        foreach (var assembly in clientAssemblies)
        {
            DiscoverTestPrototypes(assembly);
        }

        foreach (var assembly in serverAssemblies)
        {
            DiscoverTestPrototypes(assembly);
        }

        _initialized = true;
        _clientAssemblies = clientAssemblies.Concat(sharedAssemblies).ToArray();
        _serverAssemblies = serverAssemblies.Concat(sharedAssemblies).ToArray();
    }

    /// <summary>
    /// This shuts down the pool, and disposes all the server/client pairs.
    /// This is a one time operation to be used when the testing program is exiting.
    /// </summary>
    public void Shutdown()
    {
        List<TPair> localPairs;
        lock (_pairLock)
        {
            if (_dead)
                return;
            _dead = true;
            localPairs = Pairs.Keys.ToList();
        }

        foreach (var pair in localPairs)
        {
            pair.Kill();
        }

        _initialized = false;
        TestPrototypes.Clear();
    }

    protected virtual string GetDefaultTestName(TestContext testContext)
    {
        return testContext.Test.FullName.Replace("Robust.UnitTesting.", "");
    }

    public string DeathReport()
    {
        lock (_pairLock)
        {
            var builder = new StringBuilder();
            var pairs = Pairs.Keys.OrderBy(pair => pair.Id);
            foreach (var pair in pairs)
            {
                var borrowed = Pairs[pair];
                builder.AppendLine($"Pair {pair.Id}, Tests Run: {pair.TestHistory.Count}, Borrowed: {borrowed}");
                for (var i = 0; i < pair.TestHistory.Count; i++)
                {
                    builder.AppendLine($"#{i}: {pair.TestHistory[i]}");
                }
            }

            return builder.ToString();
        }
    }

    public virtual PairSettings DefaultSettings => new();

    public async Task<TPair> GetPair(PairSettings? settings = null)
    {
        if (!_initialized)
            throw new InvalidOperationException($"Pool manager has not been initialized");

        settings ??= DefaultSettings;

        // Trust issues with the AsyncLocal that backs this.
        var testContext = TestContext.CurrentContext;
        var testOut = TestContext.Out;

        DieIfPoolFailure();
        var currentTestName = settings.TestName ?? GetDefaultTestName(testContext);
        var watch = new Stopwatch();
        await testOut.WriteLineAsync($"{nameof(GetPair)}: Called by test {currentTestName}");
        TPair? pair = null;
        try
        {
            watch.Start();
            if (settings.MustBeNew)
            {
                await testOut.WriteLineAsync(
                    $"{nameof(GetPair)}: Creating pair, because settings of pool settings");
                pair = await CreateServerClientPair(settings, testOut);
            }
            else
            {
                await testOut.WriteLineAsync($"{nameof(GetPair)}: Looking in pool for a suitable pair");
                pair = GrabOptimalPair(settings);
                if (pair != null)
                {
                    pair.ActivateContext(testOut);
                    await testOut.WriteLineAsync($"{nameof(GetPair)}: Suitable pair found");

                    if (pair.Settings.CanFastRecycle(settings))
                    {
                        await testOut.WriteLineAsync($"{nameof(GetPair)}: Cleanup not needed, Skipping cleanup of pair");
                        await pair.ApplySettings(settings);
                    }
                    else
                    {
                        await testOut.WriteLineAsync($"{nameof(GetPair)}: Cleaning existing pair");
                        await pair.RecycleInternal(settings, testOut);
                    }

                    await pair.RunTicksSync(5);
                    await pair.SyncTicks(targetDelta: 1);
                }
                else
                {
                    await testOut.WriteLineAsync($"{nameof(GetPair)}: Creating a new pair, no suitable pair found in pool");
                    pair = await CreateServerClientPair(settings, testOut);
                }
            }
        }
        finally
        {
            if (pair != null && pair.TestHistory.Count > 0)
            {
                await testOut.WriteLineAsync($"{nameof(GetPair)}: Pair {pair.Id} Test History Start");
                for (var i = 0; i < pair.TestHistory.Count; i++)
                {
                    await testOut.WriteLineAsync($"- Pair {pair.Id} Test #{i}: {pair.TestHistory[i]}");
                }
                await testOut.WriteLineAsync($"{nameof(GetPair)}: Pair {pair.Id} Test History End");
            }
        }

        await testOut.WriteLineAsync($"{nameof(GetPair)}: Retrieving pair {pair.Id} from pool took {watch.Elapsed.TotalMilliseconds} ms");

        pair.ValidateSettings(settings);
        pair.ClearModifiedCvars();
        pair.Settings = settings;
        pair.TestHistory.Add(currentTestName);
        pair.SetupSeed();

        await testOut.WriteLineAsync($"{nameof(GetPair)}: Returning pair {pair.Id} with client/server seeds: {pair.ClientSeed}/{pair.ServerSeed}");

        pair.Watch.Restart();
        return pair;
    }

    private TPair? GrabOptimalPair(PairSettings poolSettings)
    {
        lock (_pairLock)
        {
            TPair? fallback = null;
            foreach (var pair in Pairs.Keys)
            {
                if (Pairs[pair])
                    continue;

                if (!pair.Settings.CanFastRecycle(poolSettings))
                {
                    fallback = pair;
                    continue;
                }

                pair.Use();
                Pairs[pair] = true;
                return pair;
            }

            if (fallback == null)
                return null;

            fallback.Use();
            Pairs[fallback!] = true;
            return fallback;
        }
    }

    /// <summary>
    /// Used by TestPair after checking the server/client pair, Don't use this.
    /// </summary>
    internal override void Return(ITestPair pair)
    {
        lock (_pairLock)
        {
            if (pair.State == PairState.Dead)
                Pairs.Remove((TPair)pair);
            else if (pair.State == PairState.Ready)
                Pairs[(TPair) pair] = false;
            else
                throw new InvalidOperationException($"Attempted to return a pair in an invalid state. Pair: {pair.Id}. State: {pair.State}.");
        }
    }

    private void DieIfPoolFailure()
    {
        if (_poolFailureReason != null)
        {
            // If the _poolFailureReason is not null, we can assume at least one test failed.
            // So we say inconclusive so we don't add more failed tests to search through.
            Assert.Inconclusive(@$"
In a different test, the pool manager had an exception when trying to create a server/client pair.
Instead of risking that the pool manager will fail at creating a server/client pairs for every single test,
we are just going to end this here to save a lot of time. This is the exception that started this:\n {_poolFailureReason}");
        }

        if (_dead)
        {
            // If Pairs is null, we ran out of time, we can't assume a test failed.
            // So we are going to tell it all future tests are a failure.
            Assert.Fail("The pool was shut down");
        }
    }

    private async Task<TPair> CreateServerClientPair(PairSettings settings, TextWriter testOut)
    {
        try
        {
            var id = Interlocked.Increment(ref _nextPairId);
            var pair = new TPair();
            await pair.Init(id, this, settings, testOut);
            pair.Use();
            await pair.RunTicksSync(5);
            await pair.SyncTicks(targetDelta: 1);
            return pair;
        }
        catch (Exception ex)
        {
            _poolFailureReason = ex;
            throw;
        }
    }

    private void DiscoverTestPrototypes(Assembly assembly)
    {
        const BindingFlags flags = BindingFlags.Static
                                   | BindingFlags.NonPublic
                                   | BindingFlags.Public
                                   | BindingFlags.DeclaredOnly;

        foreach (var type in assembly.GetTypes())
        {
            foreach (var field in type.GetFields(flags))
            {
                if (!field.HasCustomAttribute<TestPrototypesAttribute>())
                    continue;

                var val = field.GetValue(null);
                if (val is not string str)
                    throw new Exception($"{nameof(TestPrototypesAttribute)} is only valid on non-null string fields");

                TestPrototypes.Add(str);
            }
        }
    }
}
