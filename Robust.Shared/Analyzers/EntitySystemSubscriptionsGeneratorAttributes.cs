using System;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Robust.Shared.Analyzers;

[AttributeUsage(AttributeTargets.Method)]
public sealed class CallAfterSubscriptionsAttribute : Attribute;

[AttributeUsage(AttributeTargets.Method)]
public sealed class LocalEventSubscriptionAttribute : Attribute;

[AttributeUsage(AttributeTargets.Method)]
public sealed class NetworkEventSubscriptionAttribute : Attribute;

[AttributeUsage(AttributeTargets.Method)]
public sealed class EventSubscriptionAttribute : Attribute;
