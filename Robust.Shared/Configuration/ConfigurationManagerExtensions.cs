using System;
using System.Collections.Generic;

namespace Robust.Shared.Configuration;

public static class ConfigurationManagerExtensions
{
    /// <summary>
    /// Subscribe to multiple cvar in succession and create single object to unsubscribe from all of them when needed.
    /// </summary>
    public static ConfigurationMultiSubscriptionBuilder SubscribeMultiple(this IConfigurationManager manager)
    {
        return new ConfigurationMultiSubscriptionBuilder(manager);
    }
}

/// <summary> Container for batch-unsubscription of config changed events. </summary>
public sealed class ConfigurationMultiSubscriptionBuilder(IConfigurationManager manager)
{
    private readonly List<Action> _unSubscribeActions = new List<Action>();

    /// <inheritdoc cref="IConfigurationManager.OnValueChanged{T}(CVarDef{T},Action{T},bool)"/>>
    public ConfigurationMultiSubscriptionBuilder OnValueChanged<T>(
        CVarDef<T> cVar,
        CVarChanged<T> onValueChanged,
        bool invokeImmediately = false
    )
        where T : notnull
    {
        manager.OnValueChanged(cVar, onValueChanged, invokeImmediately);

        _unSubscribeActions.Add(() => manager.UnsubValueChanged(cVar, onValueChanged));
        return this;
    }

    /// <inheritdoc cref="IConfigurationManager.OnValueChanged{T}(string,Action{T},bool)"/>>
    public ConfigurationMultiSubscriptionBuilder OnValueChanged<T>(
        string name,
        CVarChanged<T> onValueChanged,
        bool invokeImmediately = false
    )
        where T : notnull
    {
        manager.OnValueChanged(name, onValueChanged, invokeImmediately);

        _unSubscribeActions.Add(() => manager.UnsubValueChanged(name, onValueChanged));
        return this;
    }

    /// <inheritdoc cref="IConfigurationManager.OnValueChanged{T}(CVarDef{T},CVarChanged{T},bool)"/>>
    public ConfigurationMultiSubscriptionBuilder OnValueChanged<T>(
        CVarDef<T> cVar,
        Action<T> onValueChanged,
        bool invokeImmediately = false
    )
        where T : notnull
    {
        manager.OnValueChanged(cVar, onValueChanged, invokeImmediately);

        _unSubscribeActions.Add(() => manager.UnsubValueChanged(cVar, onValueChanged));
        return this;
    }

    /// <inheritdoc cref="IConfigurationManager.OnValueChanged{T}(string,CVarChanged{T},bool)"/>>
    public ConfigurationMultiSubscriptionBuilder OnValueChanged<T>(
        string name,
        Action<T> onValueChanged,
        bool invokeImmediately = false
    )
        where T : notnull
    {
        manager.OnValueChanged(name, onValueChanged, invokeImmediately);

        _unSubscribeActions.Add(() => manager.UnsubValueChanged(name, onValueChanged));

        return this;
    }

    /// <summary>
    /// Return disposable object that will execute unsubscription for each when disposed.
    /// </summary>
    public IDisposable Subscribe()
    {
        return new UnSubscribeActionsDelegates(_unSubscribeActions);
    }

    /// <summary>
    /// Container for batch-unsubscription of config changed events.
    /// </summary>
    private sealed class UnSubscribeActionsDelegates(List<Action> unSubscribeActions) : IDisposable
    {
        /// <inheritdoc />
        public void Dispose()
        {
            foreach (var action in unSubscribeActions)
            {
                action();
            }
        }
    }
}
