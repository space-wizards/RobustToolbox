using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Reflection;
using Robust.Shared.Utility;

namespace Robust.Shared.Console;

/// <summary>
/// Manages registration for "entity" console commands.
/// </summary>
/// <remarks>
/// See <see cref="LocalizedEntityCommands"/> for details on what "entity" console commands are.
/// </remarks>
internal sealed class EntityConsoleHost
{
    [Dependency] private readonly IConsoleHost _consoleHost = default!;
    [Dependency] private readonly IReflectionManager _reflectionManager = default!;
    [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;

    private readonly HashSet<string> _entityCommands = [];

    /// <summary>
    /// If disabled, don't automatically discover commands via reflection.
    /// </summary>
    /// <remarks>
    /// This gets disabled in certain unit tests.
    /// </remarks>
    public bool DiscoverCommands { get; set; } = true;

    public void Startup()
    {
        DebugTools.Assert(_entityCommands.Count == 0);

        if (!DiscoverCommands)
            return;

        var deps = ((EntitySystemManager)_entitySystemManager).SystemDependencyCollection;

        _consoleHost.BeginRegistrationRegion();

        // search for all client commands in all assemblies, and register them
        foreach (var type in _reflectionManager.GetAllChildren<IEntityConsoleCommand>())
        {
            var instance = (IConsoleCommand)Activator.CreateInstance(type)!;
            deps.InjectDependencies(instance, oneOff: true);

            _entityCommands.Add(instance.Command);
            _consoleHost.RegisterCommand(instance);
        }

        _consoleHost.EndRegistrationRegion();
    }

    public void Shutdown()
    {
        foreach (var command in _entityCommands)
        {
            _consoleHost.UnregisterCommand(command);
        }

        _entityCommands.Clear();
    }
}
