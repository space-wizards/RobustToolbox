using System;
using System.Collections.Generic;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Timing;

namespace Robust.UnitTesting;

/// <summary>
/// Creates implementations of common engine interfaces for use in unit tests.
/// </summary>
public static class MockInterfaces
{
    /// <summary>
    /// Creates an instance of <see cref="IConfigurationManager"/>.
    /// </summary>
    /// <param name="gameTiming">The timing service used.</param>
    /// <param name="logManager">The log service used.</param>
    /// <param name="isServer">
    /// Whether this configuration manager will treat itself as server or client.
    /// This is only relevant for CVars that are defined as <see cref="CVar.CLIENTONLY"/> or <see cref="CVar.SERVERONLY"/>.
    /// </param>
    /// <param name="loadCvarsFromTypes">
    /// A list of additional types to pull CVar definitions from.
    /// These normally have <see cref="CVarDefsAttribute"/>.
    /// </param>
    public static IConfigurationManager MakeConfigurationManager(
        IGameTiming gameTiming,
        ILogManager logManager,
        bool isServer = true,
        IEnumerable<Type>? loadCvarsFromTypes = null)
    {
        var deps = new DependencyCollection();
        deps.RegisterInstance<IGameTiming>(gameTiming);
        deps.RegisterInstance<ILogManager>(logManager);
        deps.Register<ConfigurationManager>();
        deps.BuildGraph();

        var cfg = deps.Resolve<ConfigurationManager>();
        cfg.Initialize(isServer);

        if (loadCvarsFromTypes != null)
        {
            foreach (var loadFromType in loadCvarsFromTypes)
            {
                cfg.LoadCVarsFromType(loadFromType);
            }
        }

        return cfg;
    }
}
