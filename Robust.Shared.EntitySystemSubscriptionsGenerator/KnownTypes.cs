namespace Robust.Shared.EntitySystemSubscriptionsGenerator;

public static class KnownTypes
{
    public const string IEntitySystemTypeName = "Robust.Shared.GameObjects.IEntitySystem";
    public const string EntityUidTypeName = "Robust.Shared.GameObjects.EntityUid";
    public const string EntityTypeName = "Robust.Shared.GameObjects.Entity<T>";
    public const string EntitySessionEventArgsTypeName = "Robust.Shared.GameObjects.EntitySessionEventArgs";
    public const string IComponentTypeName = "Robust.Shared.GameObjects.IComponent";

    public const string LocalSubscriptionMemberAttributeName =
        "Robust.Shared.Analyzers.LocalEventSubscriptionAttribute";

    public const string NetworkSubscriptionMemberAttributeName =
        "Robust.Shared.Analyzers.NetworkEventSubscriptionAttribute";

    public const string AllSubscriptionMemberAttributeName = "Robust.Shared.Analyzers.EventSubscriptionAttribute";

    public const string CallAfterSubscriptionsAttributeName =
        "Robust.Shared.Analyzers.CallAfterSubscriptionsAttribute";

    public static readonly string ComponentSubscriptionHandlerTypes = string.Join(
        ", ",
        "Robust.Shared.GameObjects.ComponentEventHandler",
        "Robust.Shared.GameObjects.ComponentEventRefHandler",
        "Robust.Shared.GameObjects.EntityEventRefHandler"
    );

    public static readonly string NonComponentSubscriptionHandlerTypes = string.Join(
        ", ",
        "Robust.Shared.GameObjects.EntityEventHandler",
        "Robust.Shared.GameObjects.EntityEventRefHandler",
        "Robust.Shared.GameObjects.EntitySessionEventHandler"
    );

    public const string CallAfterSubscriptionsHandlerTypes = nameof(Action);

    public static SubscriptionType? ToSubscriptionType(this string annotation)
    {
        return annotation switch
        {
            AllSubscriptionMemberAttributeName => SubscriptionType.All,
            NetworkSubscriptionMemberAttributeName => SubscriptionType.Network,
            LocalSubscriptionMemberAttributeName => SubscriptionType.Local,
            _ => null
        };
    }

    public static string ToSubscriptionMethod(this SubscriptionType type) => type switch
    {
        SubscriptionType.All => "SubscribeAllEvent",
        SubscriptionType.Network => "SubscribeNetworkEvent",
        SubscriptionType.Local => "SubscribeLocalEvent",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };
}

public enum SubscriptionType
{
    All,
    Network,
    Local,
}
