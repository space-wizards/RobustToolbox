using Robust.Shared.Utility;

namespace Robust.Shared.GameObjects;

public partial class EntityManager
{
    private static readonly ComponentAdd CompAddInstance = new();
    private static readonly ComponentInit CompInitInstance = new();
    private static readonly ComponentStartup CompStartupInstance = new();
    private static readonly ComponentShutdown CompShutdownInstance = new();
    private static readonly ComponentRemove CompRemoveInstance = new();

    /// <summary>
    /// Increases the life stage from <see cref="ComponentLifeStage.PreAdd" /> to <see cref="ComponentLifeStage.Added" />,
    /// after raising a <see cref="ComponentAdd"/> event.
    /// </summary>
    internal void LifeAddToEntity<T>(EntityUid uid, T component, CompIdx idx) where T : IComponent
    {
        DebugTools.Assert(!_deleteSet.Contains(component));
        DebugTools.Assert(component.LifeStage == ComponentLifeStage.PreAdd);

#pragma warning disable CS0618 // Type or member is obsolete
        component.LifeStage = ComponentLifeStage.Adding;
        component.CreationTick = CurrentTick;
        // networked components are assumed to be dirty when added to entities. See also: ClearTicks()
        component.LastModifiedTick = CurrentTick;
        EventBus.RaiseComponentEvent(uid, component, idx, CompAddInstance);
        component.LifeStage = ComponentLifeStage.Added;
#pragma warning restore CS0618 // Type or member is obsolete
    }

    /// <summary>
    /// Increases the life stage from <see cref="ComponentLifeStage.Added" /> to <see cref="ComponentLifeStage.Initialized" />,
    /// calling <see cref="Initialize" />.
    /// </summary>
    internal void LifeInitialize<T>(EntityUid uid, T component, CompIdx idx) where T : IComponent
    {
        DebugTools.Assert(!_deleteSet.Contains(component));
        DebugTools.Assert(component.LifeStage == ComponentLifeStage.Added);

        component.LifeStage = ComponentLifeStage.Initializing;
        EventBus.RaiseComponentEvent(uid, component, idx, CompInitInstance);
        component.LifeStage = ComponentLifeStage.Initialized;
    }

    /// <summary>
    /// Increases the life stage from <see cref="ComponentLifeStage.Initialized" /> to
    /// <see cref="ComponentLifeStage.Running" />, calling <see cref="Startup" />.
    /// </summary>
    internal void LifeStartup<T>(EntityUid uid, T component, CompIdx idx) where T : IComponent
    {
        DebugTools.Assert(!_deleteSet.Contains(component));
        DebugTools.Assert(component.LifeStage == ComponentLifeStage.Initialized);

        component.LifeStage = ComponentLifeStage.Starting;
        EventBus.RaiseComponentEvent(uid, component, idx, CompStartupInstance);
        component.LifeStage = ComponentLifeStage.Running;
    }

    /// <summary>
    /// Increases the life stage from <see cref="ComponentLifeStage.Running" /> to <see cref="ComponentLifeStage.Stopped" />,
    /// calling <see cref="Shutdown" />.
    /// </summary>
    /// <remarks>
    /// Components are allowed to remove themselves in their own Startup function.
    /// </remarks>
    internal void LifeShutdown<T>(EntityUid uid, T component, CompIdx idx) where T : IComponent
    {
        DebugTools.Assert(component.LifeStage is >= ComponentLifeStage.Initializing and < ComponentLifeStage.Stopping);

        if (component.LifeStage <= ComponentLifeStage.Initialized)
        {
            // Component was never started, no shutdown logic necessary. Simply mark it as stopped.
            component.LifeStage = ComponentLifeStage.Stopped;
            return;
        }

        component.LifeStage = ComponentLifeStage.Stopping;
        EventBus.RaiseComponentEvent(uid, component, idx, CompShutdownInstance);
        component.LifeStage = ComponentLifeStage.Stopped;
    }

    /// <summary>
    /// Increases the life stage from <see cref="ComponentLifeStage.Stopped" /> to <see cref="ComponentLifeStage.Deleted" />,
    /// calling <see cref="Component.OnRemove" />.
    /// </summary>
    internal void LifeRemoveFromEntity<T>(EntityUid uid, T component, CompIdx idx) where T : IComponent
    {
        // can be called at any time after PreAdd, including inside other life stage events.
        DebugTools.Assert(component.LifeStage != ComponentLifeStage.PreAdd);

        component.LifeStage = ComponentLifeStage.Removing;
        EventBus.RaiseComponentEvent(uid, component, idx, CompRemoveInstance);
        component.LifeStage = ComponentLifeStage.Deleted;
    }

    internal virtual void SetLifeStage(MetaDataComponent meta, EntityLifeStage stage)
    {
        meta.EntityLifeStage = stage;
    }
}
