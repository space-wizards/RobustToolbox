using System;
using Robust.Shared.GameObjects;

namespace Robust.Shared.Analyzers;

// These annotations direct the operation of `Robust.Shared.EntitySystemSubscriptionsGenerator`'s
// `EntitySystemSubscriptionGenerator` and `EntitySystemSubscriptionGeneratorErrorAnalyser`.

/// This attribute indicates that the annotated method is a handler for an event subscription. Methods annotated with
/// this attribute will have a <c>EntitySystem.SubscribeLocalEvent</c> call generated, using the method as the handler,
/// with the event type (and component, as relevant) inferred from the method signature.
/// <br/>
/// For this to work, the annotated method must be compatible with one of the following delegate types:
/// <ul>
/// <li><see cref="EntityEventHandler{TEvent}"/></li>
/// <li><see cref="EntityEventRefHandler{TComp,TEvent}"/></li>
/// <li><see cref="ComponentEventHandler{TComp,TEvent}"/></li>
/// <li><see cref="ComponentEventRefHandler{TComp,TEvent}"/></li>
/// </ul>
/// <br/>
/// Note that this is <b>not</b> any different from the normal requirements to use <c>EntitySystem.SubscribeLocalEvent</c>.
[AttributeUsage(AttributeTargets.Method)]
public sealed class LocalEventSubscriptionAttribute : Attribute;

/// This attribute indicates that the annotated method is a handler for an event subscription. Methods annotated with
/// this attribute will have a <c>EntitySystem.SubscribeNetworkEvent</c> call generated, using the method as the handler,
/// with the event type inferred from the method signature.
/// <br/>
/// For this to work, the annotated method must be compatible with one of the following delegate types:
/// <ul>
/// <li><see cref="EntityEventHandler{TEvent}"/></li>
/// <li><see cref="EntitySessionEventHandler{TEvent}"/></li>
/// </ul>
/// <br/>
/// Note that this is <b>not</b> any different from the normal requirements to use <c>EntitySystem.SubscribeNetworkEvent</c>.
[AttributeUsage(AttributeTargets.Method)]
public sealed class NetworkEventSubscriptionAttribute : Attribute;

/// This attribute indicates that the annotated method is a handler for an event subscription. Methods annotated with
/// this attribute will have a <c>EntitySystem.SubscribeAllEvent</c> call generated, using the method as the handler,
/// with the event type inferred from the method signature.
/// <br/>
/// For this to work, the annotated method must be compatible with one of the following delegate types:
/// <ul>
/// <li><see cref="EntityEventHandler{TEvent}"/></li>
/// <li><see cref="EntitySessionEventHandler{TEvent}"/></li>
/// </ul>
/// <br/>
/// Note that this is <b>not</b> any different from the normal requirements to use <c>EntitySystem.SubscribeAllEvent</c>.
[AttributeUsage(AttributeTargets.Method)]
public sealed class EventSubscriptionAttribute : Attribute;
