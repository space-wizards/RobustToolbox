using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Robust.Shared.GameObjects;

/// <summary>
/// Extension of <see cref="EntitySystem"/> for use with singleton components.
/// When accessed, an entity will be spawned in nullspace with an attached <typeparamref name="T"/>.
/// The <typeparamref name="T"/> can be accessed through the <see cref="Instance"/> property.
/// </summary>
/// <typeparam name="T">Type of the attached component.</typeparam>
public abstract partial class SingletonEntitySystem<T> : EntitySystem where T : Component, new()
{
    [Dependency] protected readonly MetaDataSystem Meta = default!;

    private Entity<T>? _cachedEntity;

    /// <summary>
    /// Reference to the singleton instance of <typeparamref name="T"/>.
    /// </summary>
    protected T Instance => _cachedEntity ??= FindOrCreateHolder();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<T, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<T, ComponentShutdown>(OnComponentShutdown);
    }

    protected virtual void OnComponentInit(Entity<T> entity, ref ComponentInit args)
    {
        DebugTools.Assert(_cachedEntity == null);

        _cachedEntity = entity;
    }

    protected virtual void OnComponentShutdown(Entity<T> entity, ref ComponentShutdown args)
    {
        _cachedEntity = null;
    }

    private Entity<T> FindOrCreateHolder()
    {
        var query = EntityQueryEnumerator<T>();
        while (query.MoveNext(out var uid, out var comp))
        {
            return (uid, comp);
        }

        return CreateHolder();
    }

    private Entity<T> CreateHolder()
    {
        var uid = Spawn(null, MapCoordinates.Nullspace);
        var comp = AddComp<T>(uid);
        Meta.SetEntityName(uid, $"{typeof(T).Name} Holder");
        return (uid, comp);
    }
}
