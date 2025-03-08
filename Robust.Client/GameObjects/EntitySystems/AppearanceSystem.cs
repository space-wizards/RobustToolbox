using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;
using Robust.Shared.Serialization.Manager;

namespace Robust.Client.GameObjects
{
    [UsedImplicitly]
    public sealed class AppearanceSystem : SharedAppearanceSystem
    {
        private readonly Queue<(EntityUid uid, AppearanceComponent)> _queuedUpdates = new();
        [Dependency] private readonly ISerializationManager _serialization = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<AppearanceComponent, ComponentStartup>(OnAppearanceStartup);
            SubscribeLocalEvent<AppearanceComponent, ComponentHandleState>(OnAppearanceHandleState);
        }

        protected override void OnAppearanceGetState(EntityUid uid, AppearanceComponent component, ref ComponentGetState args)
        {
            // TODO Game State
            // Force the client to serialize & de-serialize implicitly generated component states.
            var clone = CloneAppearanceData(component.AppearanceData);
            args.State = new AppearanceComponentState(clone);
        }

        private void OnAppearanceStartup(EntityUid uid, AppearanceComponent component, ComponentStartup args)
        {
            QueueUpdate(uid, component);
        }

        private void OnAppearanceHandleState(EntityUid uid, AppearanceComponent component, ref ComponentHandleState args)
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

            if (!stateDiff)
            {
                // Even if the appearance hasn't changed, we may need to update if we are re-entering PVS
                if (component.AppearanceDirty)
                    QueueUpdate(uid, component);
            }

            component.AppearanceData = CloneAppearanceData(actualState.Data);
            QueueUpdate(uid, component);
        }

        /// <summary>
        ///     Take in an appearance data dictionary and attempt to clone it.
        /// </summary>
        /// <remarks>
        ///     As some appearance data values are not simple value-type objects, this is not just a shallow clone.
        /// </remarks>
        private Dictionary<Enum, object> CloneAppearanceData(Dictionary<Enum, object> data)
        {
            Dictionary<Enum, object> newDict = new(data.Count);

            foreach (var (key, value) in data)
            {
                object? serializationObject;
                if (value.GetType().IsValueType)
                    newDict[key] = value;
                else if (value is ICloneable cloneable)
                    newDict[key] = cloneable.Clone();
                else if ((serializationObject = _serialization.CreateCopy(value)) != null)
                    newDict[key] = serializationObject;
                else
                    throw new NotSupportedException("Invalid object in appearance data dictionary. Appearance data must be cloneable");
            }

            return newDict;
        }

        public override void QueueUpdate(EntityUid uid, AppearanceComponent component)
        {
            if (component.UpdateQueued)
            {
                DebugTools.Assert(component.AppearanceDirty);
                return;
            }

            _queuedUpdates.Enqueue((uid, component));
            component.AppearanceDirty = true;
            component.UpdateQueued = true;
        }

        public override void FrameUpdate(float frameTime)
        {
            var spriteQuery = GetEntityQuery<SpriteComponent>();
            var metaQuery = GetEntityQuery<MetaDataComponent>();
            while (_queuedUpdates.TryDequeue(out var next))
            {
                var (uid, appearance) = next;
                appearance.UpdateQueued = false;
                if (!appearance.Running)
                    continue;

                // If the entity is no longer within the clients PVS, don't bother updating.
                if ((metaQuery.GetComponent(uid).Flags & MetaDataFlags.Detached) != 0)
                    continue;

                appearance.AppearanceDirty = false;

                // Sprite comp is allowed to be null, so that things like spriteless point-lights can use this system
                spriteQuery.TryGetComponent(uid, out var sprite);
                OnChangeData(uid, sprite, appearance);
            }
        }

        public void OnChangeData(EntityUid uid, SpriteComponent? sprite, AppearanceComponent? appearanceComponent = null)
        {
            if (!Resolve(uid, ref appearanceComponent, false))
                return;

            var ev = new AppearanceChangeEvent
            {
                Component = appearanceComponent,
                AppearanceData = appearanceComponent.AppearanceData,
                Sprite = sprite,
            };

            // Give it AppearanceData so we can still keep the friend attribute on the component.
            RaiseLocalEvent(uid, ref ev);
        }
    }

    /// <summary>
    ///     Raised whenever the appearance data for an entity changes.
    /// </summary>
    [ByRefEvent]
    public struct AppearanceChangeEvent
    {
        public AppearanceComponent Component;
        public IReadOnlyDictionary<Enum, object> AppearanceData;
        public SpriteComponent? Sprite;
    }
}
