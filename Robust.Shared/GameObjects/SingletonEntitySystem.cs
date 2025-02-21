using System.Diagnostics.CodeAnalysis;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Utility;

namespace Robust.Shared.GameObjects;

/// <summary>
/// Extension of <see cref="EntitySystem"/> for use with singleton components.
/// When accessed, an entity will be spawned in nullspace with an attached <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">Type of the attached component.</typeparam>
public abstract partial class SingletonEntitySystem<T> : EntitySystem where T : Component, new()
{
    [Dependency] protected readonly MetaDataSystem Meta = default!;
    [Dependency] protected readonly INetManager NetMan = default!;

    private Entity<T>? _cachedEntity;

    /// <summary>
    /// Is the singleton instance ready to access?
    /// </summary>
    /// <remarks>
    /// This allows checking whether the instance reference has been created
    /// without forcing it to spawn if it has not yet.
    /// </remarks>
    public bool IsReady => _cachedEntity != null;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<T, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<T, ComponentShutdown>(OnComponentShutdown);
    }

    [MustCallBase]
    protected virtual void OnComponentInit(Entity<T> entity, ref ComponentInit args)
    {
        DebugTools.Assert(_cachedEntity == null);

        _cachedEntity = entity;
    }

    [MustCallBase]
    protected virtual void OnComponentShutdown(Entity<T> entity, ref ComponentShutdown args)
    {
        _cachedEntity = null;
    }

    private Entity<T>? FindOrCreateHolder()
    {
        var query = EntityQueryEnumerator<T>();
        while (query.MoveNext(out var uid, out var comp))
        {
            return (uid, comp);
        }

        if (NetMan.IsClient)
            return null;

        return CreateHolder();
    }

    private Entity<T> CreateHolder()
    {
        var uid = Spawn(null, MapCoordinates.Nullspace);
        var comp = AddComp<T>(uid);
        Meta.SetEntityName(uid, $"{typeof(T).Name} Holder");
        return (uid, comp);
    }

    /// <summary>
    /// Attempts to get the singleton Entity{<typeparamref name="T"/>} instance.
    /// This always succeeds on the server, where the instance will be created if
    /// if does not exist yet. On the client, this can return false if attempting
    /// to access the instance before it has been sent from the server.
    /// </summary>
    /// <param name="instance">The singleton Entity{<typeparamref name="T"/>} instance.</param>
    /// <returns>True if the instance was found or created, otherwise false</returns>
    public bool TryGetInstance([NotNullWhen(true)] out Entity<T>? instance)
    {
        instance = FindOrCreateHolder();
        return instance != null;
    }
}
