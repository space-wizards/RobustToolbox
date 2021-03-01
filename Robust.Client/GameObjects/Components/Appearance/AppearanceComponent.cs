using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;
using YamlDotNet.RepresentationModel;

namespace Robust.Client.GameObjects
{
    public sealed class AppearanceComponent : SharedAppearanceComponent
    {
        [ViewVariables]
        private Dictionary<object, object> data = new();

        [ViewVariables]
        [DataField("visuals")]
        internal List<AppearanceVisualizer> Visualizers = new();

        [Dependency] private readonly IReflectionManager _reflectionManager = default!;

        private static bool _didRegisterSerializer;

        [ViewVariables]
        private bool _appearanceDirty;

        public override void SetData(string key, object value)
        {
            SetData(key, value);
        }

        public override void SetData(Enum key, object value)
        {
            SetData(key, value);
        }

        public override T GetData<T>(string key)
        {
            return (T) data[key];
        }

        public override T GetData<T>(Enum key)
        {
            return (T) data[key];
        }

        internal T GetData<T>(object key)
        {
            return (T) data[key];
        }

        public override bool TryGetData<T>(Enum key, [NotNullWhen(true)] out T data)
        {
            return TryGetData(key, out data);
        }

        public override bool TryGetData<T>(string key, [NotNullWhen(true)] out T data)
        {
            return TryGetData(key, out data);
        }

        internal bool TryGetData<T>(object key, [NotNullWhen(true)] out T data)
        {
            if (this.data.TryGetValue(key, out var dat))
            {
                data = (T) dat;
                return true;
            }

            data = default!;
            return false;
        }

        private void SetData(object key, object value)
        {
            if (data.TryGetValue(key, out var existing) && existing.Equals(value)) return;

            data[key] = value;

            MarkDirty();
        }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            if (curState is not AppearanceComponentState actualState)
                return;

            data = actualState.Data;
            MarkDirty();
        }

        internal void MarkDirty()
        {
            if (_appearanceDirty)
            {
                return;
            }

            EntitySystem.Get<AppearanceSystem>()
                .EnqueueAppearanceUpdate(this);
            _appearanceDirty = true;
        }

        internal void UnmarkDirty()
        {
            _appearanceDirty = false;
        }

        public override void Initialize()
        {
            base.Initialize();

            foreach (var visual in Visualizers)
            {
                visual.InitializeEntity(Owner);
            }

            MarkDirty();
        }


        internal class SpriteLayerToggle : AppearanceVisualizer
        {
            public const string NAME = "sprite_layer_toggle";

            public readonly object Key;
            public readonly int SpriteLayer;

            public SpriteLayerToggle(object key, int spriteLayer)
            {
                Key = key;
                SpriteLayer = spriteLayer;
            }
        }
    }

    /// <summary>
    ///     Handles the visualization of data inside of an appearance component.
    ///     Implementations of this class are NOT bound to a specific entity, they are flyweighted across multiple.
    /// </summary>
    [ImplicitDataDefinitionForInheritors]
    public abstract class AppearanceVisualizer
    {
        /// <summary>
        ///     Initializes an entity to be managed by this appearance controller.
        ///     DO NOT assume this is your only entity. Visualizers are shared.
        /// </summary>
        public virtual void InitializeEntity(IEntity entity)
        {
        }

        /// <summary>
        ///     Called whenever appearance data for an entity changes.
        ///     Update its visuals here.
        /// </summary>
        /// <param name="component">The appearance component of the entity that might need updating.</param>
        public virtual void OnChangeData(AppearanceComponent component)
        {
        }
    }

    sealed class AppearanceTestComponent : Component
    {
        public override string Name => "AppearanceTest";

        float time;
        bool state;

        public void OnUpdate(float frameTime)
        {
            time += frameTime;
            if (time > 1)
            {
                time -= 1;
                Owner.GetComponent<AppearanceComponent>().SetData("test", state = !state);
            }
        }
    }
}
