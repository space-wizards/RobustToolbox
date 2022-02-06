using System.Collections.Generic;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;

namespace Robust.Client.GameObjects
{
    [UsedImplicitly]
    internal sealed class AppearanceSystem : SharedAppearanceSystem
    {
        private readonly Queue<ClientAppearanceComponent> _queuedUpdates = new();

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ClientAppearanceComponent, ComponentInit>(OnAppearanceInit);
            SubscribeLocalEvent<ClientAppearanceComponent, ComponentHandleState>(OnAppearanceHandleState);
        }

        private void OnAppearanceInit(EntityUid uid, ClientAppearanceComponent component, ComponentInit args)
        {
            foreach (var visual in component.Visualizers)
            {
                visual.InitializeEntity(uid);
            }

            MarkDirty(component);
        }

        private void OnAppearanceHandleState(EntityUid uid, ClientAppearanceComponent component, ref ComponentHandleState args)
        {
            if (args.Current is not AppearanceComponentState actualState)
                return;

            var stateDiff = component.AppearanceData.Count != actualState.Data.Count;

            if (!stateDiff)
            {
                foreach (var (key, value) in component.AppearanceData)
                {
                    if (!actualState.Data.TryGetValue(key, out var stateValue) ||
                        !value.Equals(stateValue))
                    {
                        stateDiff = true;
                        break;
                    }
                }
            }

            if (!stateDiff) return;

            component.AppearanceData = actualState.Data;
            MarkDirty(component);
        }

        public override void MarkDirty(AppearanceComponent component)
        {
            if (component.AppearanceDirty)
                return;

            _queuedUpdates.Enqueue((ClientAppearanceComponent) component);
            component.AppearanceDirty = true;
        }

        internal void UnmarkDirty(ClientAppearanceComponent component)
        {
            component.AppearanceDirty = false;
        }

        public override void FrameUpdate(float frameTime)
        {
            while (_queuedUpdates.TryDequeue(out var appearance))
            {
                if (appearance.Deleted)
                    continue;

                OnChangeData(appearance.Owner, appearance);
                UnmarkDirty(appearance);
            }
        }

        public void OnChangeData(EntityUid uid, ClientAppearanceComponent? appearanceComponent = null)
        {
            if (!Resolve(uid, ref appearanceComponent, false)) return;

            // Give it AppearanceData so we can still keep the friend attribute on the component.
            EntityManager.EventBus.RaiseLocalEvent(uid, new AppearanceChangeEvent
            {
                Component = appearanceComponent,
                AppearanceData = appearanceComponent._appearanceData,
            });

            // Eventually visualizers would be nuked and we'd just make them components instead.
            foreach (var visualizer in appearanceComponent.Visualizers)
            {
                visualizer.OnChangeData(appearanceComponent);
            }
        }
    }

    /// <summary>
    /// Raised whenever the appearance data for an entity changes.
    /// </summary>
    public sealed class AppearanceChangeEvent : EntityEventArgs
    {
        public AppearanceComponent Component = default!;
        public IReadOnlyDictionary<object, object> AppearanceData = default!;
    }
}
