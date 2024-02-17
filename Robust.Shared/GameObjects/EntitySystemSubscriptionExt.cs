using System;
using Robust.Shared.Configuration;

namespace Robust.Shared.GameObjects;

/// <summary>
/// Extra subscription helpers for <see cref="EntitySystem"/> that are not part of the core entity system behavior.
/// </summary>
public static class EntitySystemSubscriptionExt
{
    /// <summary>
    /// Listen for an event for if the config value changes.
    /// </summary>
    /// <remarks>
    /// This is an O(n) operation.
    /// </remarks>
    /// <param name="subs">
    /// The entity system subscriptions.
    /// Call this with <see cref="EntitySystem.Subscriptions"/>.
    /// </param>
    /// <param name="cfg">The configuration manager.</param>
    /// <param name="name">The name of the CVar to listen for.</param>
    /// <param name="onValueChanged">The delegate to run when the value was changed.</param>
    /// <param name="invokeImmediately">
    /// Whether to run the callback immediately inw this method. Can help reduce boilerplate
    /// </param>
    /// <typeparam name="T">The type of value contained in this CVar.</typeparam>
    public static void CVar<T>(
        this EntitySystem.Subscriptions subs,
        IConfigurationManager cfg,
        string name,
        Action<T> onValueChanged,
        bool invokeImmediately = false)
        where T : notnull
    {
        cfg.OnValueChanged(name, onValueChanged, invokeImmediately);

        subs.RegisterUnsubscription(() => cfg.UnsubValueChanged(name, onValueChanged));
    }

    /// <summary>
    /// Listen for an event for if the config value changes.
    /// </summary>
    /// <remarks>
    /// This is an O(n) operation.
    /// </remarks>
    /// <param name="subs">
    /// The entity system subscriptions.
    /// Call this with <see cref="EntitySystem.Subscriptions"/>.
    /// </param>
    /// <param name="cfg">The configuration manager.</param>
    /// <param name="cVar">The CVar to listen for.</param>
    /// <param name="onValueChanged">The delegate to run when the value was changed.</param>
    /// <param name="invokeImmediately">
    /// Whether to run the callback immediately in this method. Can help reduce boilerplate
    /// </param>
    /// <typeparam name="T">The type of value contained in this CVar.</typeparam>
    public static void CVar<T>(
        this EntitySystem.Subscriptions subs,
        IConfigurationManager cfg,
        CVarDef<T> cVar,
        Action<T> onValueChanged,
        bool invokeImmediately = false)
        where T : notnull
    {
        cfg.OnValueChanged(cVar, onValueChanged, invokeImmediately);

        subs.RegisterUnsubscription(() => cfg.UnsubValueChanged(cVar, onValueChanged));
    }

    /// <summary>
    /// Listen for an event for if the config value changes.
    /// </summary>
    /// <remarks>
    /// This is an O(n) operation.
    /// </remarks>
    /// <param name="subs">
    /// The entity system subscriptions.
    /// Call this with <see cref="EntitySystem.Subscriptions"/>.
    /// </param>
    /// <param name="cfg">The configuration manager.</param>
    /// <param name="name">The name of the CVar to listen for.</param>
    /// <param name="onValueChanged">The delegate to run when the value was changed.</param>
    /// <param name="invokeImmediately">
    /// Whether to run the callback immediately in this method. Can help reduce boilerplate
    /// </param>
    /// <typeparam name="T">The type of value contained in this CVar.</typeparam>
    public static void CVar<T>(
        this EntitySystem.Subscriptions subs,
        IConfigurationManager cfg,
        string name,
        CVarChanged<T> onValueChanged,
        bool invokeImmediately = false)
        where T : notnull
    {
        cfg.OnValueChanged(name, onValueChanged, invokeImmediately);

        subs.RegisterUnsubscription(() => cfg.UnsubValueChanged(name, onValueChanged));
    }

    /// <summary>
    /// Listen for an event for if the config value changes.
    /// </summary>
    /// <remarks>
    /// This is an O(n) operation.
    /// </remarks>
    /// <param name="subs">
    /// The entity system subscriptions.
    /// Call this with <see cref="EntitySystem.Subscriptions"/>.
    /// </param>
    /// <param name="cfg">The configuration manager.</param>
    /// <param name="cVar">The CVar to listen for.</param>
    /// <param name="onValueChanged">The delegate to run when the value was changed.</param>
    /// <param name="invokeImmediately">
    /// Whether to run the callback immediately in this method. Can help reduce boilerplate
    /// </param>
    /// <typeparam name="T">The type of value contained in this CVar.</typeparam>
    public static void CVar<T>(
        this EntitySystem.Subscriptions subs,
        IConfigurationManager cfg,
        CVarDef<T> cVar,
        CVarChanged<T> onValueChanged,
        bool invokeImmediately = false)
        where T : notnull
    {
        cfg.OnValueChanged(cVar, onValueChanged, invokeImmediately);

        subs.RegisterUnsubscription(() => cfg.UnsubValueChanged(cVar, onValueChanged));
    }
}
