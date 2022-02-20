using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;

namespace Robust.Client.GameObjects
{
    [UsedImplicitly]
    public sealed class AppearanceSystem : SharedAppearanceSystem
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

            component.AppearanceData = CloneAppearanceData(actualState.Data);
            MarkDirty(component);
        }

        /// <summary>
        ///     Take in an appearance data dictionary and attempt to clone it.
        /// </summary>
        /// <remarks>
        ///     As some appearance data values are not simple value-type objects, this is not just a shallow clone.
        /// </remarks>
        private Dictionary<object, object> CloneAppearanceData(Dictionary<object, object> data)
        {
            Dictionary<object, object> newDict = new(data.Count);

            foreach (var (key, value) in data)
            {
                if (value.GetType().IsValueType)
                    newDict[key] = value;
                else if (value is ICloneable cloneable)
                    newDict[key] = cloneable.Clone();
                else
                    throw new NotSupportedException("Invalid object in appearance data dictionary. Appearance data must be cloneable");
            }

            return newDict;
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

            var ev = new AppearanceChangeEvent
            {
                Component = appearanceComponent,
                AppearanceData = appearanceComponent.AppearanceData,
            };

            // Give it AppearanceData so we can still keep the friend attribute on the component.
            EntityManager.EventBus.RaiseLocalEvent(uid, ref ev);

            // Eventually visualizers would be nuked and we'd just make them components instead.
            foreach (var visualizer in appearanceComponent.Visualizers)
            {
                visualizer.OnChangeData(appearanceComponent);
            }
        }
    }

    /// <summary>
    ///     Raised whenever the appearance data for an entity changes.
    /// </summary>
    [ByRefEvent]
    public struct AppearanceChangeEvent
    {
        public AppearanceComponent Component;
        public IReadOnlyDictionary<object, object> AppearanceData;
    }
}
