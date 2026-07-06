using System;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Robust.Shared.Analyzers;

// These annotations direct the operation of `Robust.Shared.EntitySystemSubscriptionsGenerator`'s
// `EntitySystemSubscriptionGenerator` and `EntitySystemSubscriptionGeneratorErrorAnalyser`.

/// <summary>
/// This attribute indicates that the annotated method is a handler for an event subscription. Methods annotated with
/// this attribute will have a <see cref="EntitySystem.SubscribeLocalEvent"/> call generated, using the method as the handler,
/// with the event type (and component, as relevant) inferred from the method signature.
/// </summary>
/// <remarks>
/// For this to work, the annotated method must be compatible with one of the following delegate types:
/// <list type="bullet">
/// <item><see cref="EntityEventHandler{TEvent}"/></item>
/// <item><see cref="EntityEventRefHandler{TComp,TEvent}"/></item>
/// <item><see cref="ComponentEventHandler{TComp,TEvent}"/></item>
/// <item><see cref="ComponentEventRefHandler{TComp,TEvent}"/></item>
/// </list>
/// Note that this is <b>not</b> any different from the normal requirements to use <see cref="EntitySystem.SubscribeLocalEvent"/>.
/// </remarks>
[AttributeUsage(AttributeTargets.Method)]
[MeansImplicitUse]
public sealed class SubscribeLocalEventAttribute : Attribute;

/// <summary>
/// This attribute indicates that the annotated method is a handler for an event subscription. Methods annotated with
/// this attribute will have a <see cref="EntitySystem.SubscribeNetworkEvent"/> call generated, using the method as the handler,
/// with the event type inferred from the method signature.
/// </summary>
/// <remarks>
/// For this to work, the annotated method must be compatible with one of the following delegate types:
/// <list type="bullet">
/// <item><see cref="EntityEventHandler{TEvent}"/></item>
/// <item><see cref="EntitySessionEventHandler{TEvent}"/></item>
/// </list>
/// Note that this is <b>not</b> any different from the normal requirements to use <see cref="EntitySystem.SubscribeNetworkEvent"/>.
/// </remarks>
[AttributeUsage(AttributeTargets.Method)]
[MeansImplicitUse]
public sealed class SubscribeNetworkEventAttribute : Attribute;

/// <summary>
/// This attribute indicates that the annotated method is a handler for an event subscription. Methods annotated with
/// this attribute will have a <see cref="EntitySystem.SubscribeAllEvent"/> call generated, using the method as the handler,
/// with the event type inferred from the method signature.
/// </summary>
/// <remarks>
/// For this to work, the annotated method must be compatible with one of the following delegate types:
/// <list type="bullet">
/// <item><see cref="EntityEventHandler{TEvent}"/></item>
/// <item><see cref="EntitySessionEventHandler{TEvent}"/></item>
/// </list>
/// Note that this is <b>not</b> any different from the normal requirements to use <see cref="EntitySystem.SubscribeAllEvent"/>.
/// </remarks>
[AttributeUsage(AttributeTargets.Method)]
[MeansImplicitUse]
public sealed class EventSubscriptionAttribute : Attribute;
