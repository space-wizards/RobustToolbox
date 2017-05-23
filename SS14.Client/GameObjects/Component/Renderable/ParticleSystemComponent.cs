
using SFML.Graphics;
using SFML.System;
using SS14.Client.Graphics;
using SS14.Client.Interfaces.GOC;
using SS14.Client.Interfaces.Resource;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.Components.Particles;
using SS14.Shared.IoC;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SS14.Client.GameObjects
{
    [IoCTarget]
    [Component("ParticleSystem")]
    public class ParticleSystemComponent : Component, IParticleSystemComponent, IRenderableComponent
    {
        #region Variables.
        private Dictionary<string, ParticleSystem> _emitters = new Dictionary<string, ParticleSystem>(); // List of particle emitters.
        protected IRenderableComponent master;
        protected List<IRenderableComponent> slaves = new List<IRenderableComponent>();

        public DrawDepth DrawDepth { get; set; }
        #endregion

        #region Properties
        public FloatRect AverageAABB
        {
            get { return AABB; }
        }

        public FloatRect AABB
        {
            get { return new FloatRect(); }
        }
        #endregion

        public ParticleSystemComponent()
        {
            Family = ComponentFamily.Particles;
            DrawDepth = DrawDepth.ItemsOnTables;
        }

        public override Type StateType
        {
            get { return typeof(ParticleSystemComponentState); }
        }

        public void OnMove(object sender, VectorEventArgs args)
        {
            var offset = new Vector2f(args.VectorTo.X, args.VectorTo.Y) -
                         new Vector2f(args.VectorFrom.X, args.VectorFrom.Y);
            foreach (KeyValuePair<string, ParticleSystem> particleSystem in _emitters)
            {
                particleSystem.Value.MoveEmitter(particleSystem.Value.EmitterPosition + offset);
            }
            //_emitter.MoveEmitter(_emitter.EmitterPosition + offset);
        }

        public override void OnAdd(Entity owner)
        {
            base.OnAdd(owner);
            var transform = Owner.GetComponent<TransformComponent>(ComponentFamily.Transform);
            transform.OnMove += OnMove;
        }

        public override void OnRemove()
        {
            var transform = Owner.GetComponent<TransformComponent>(ComponentFamily.Transform);
            transform.OnMove -= OnMove;
            base.OnRemove();
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            foreach (KeyValuePair<string, ParticleSystem> particleSystem in _emitters)
            {
                particleSystem.Value.Update(frameTime);
            }
        }

        public virtual void Render(Vector2f topLeft, Vector2f bottomRight)
        {
            Vector2f renderPos = CluwneLib.WorldToScreen(
                    Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position);

            foreach (KeyValuePair<string, ParticleSystem> particleSystem in _emitters)
            {
                particleSystem.Value.Move(renderPos);
                particleSystem.Value.Render();
            }
        }

        public float Bottom
        {
            get
            {
                return Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position.Y;
                //return Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position.Y +
                //       (_particleSprite.Height / 2);
            }
        }

        public bool IsSlaved()
        {
            return master != null;
        }

        public void SetMaster(Entity m)
        {
            if (!m.HasComponent(ComponentFamily.Renderable))
                return;
            var mastercompo = m.GetComponent<SpriteComponent>(ComponentFamily.Renderable);
            //If there's no sprite component, then FUCK IT
            if (mastercompo == null)
                return;

            mastercompo.AddSlave(this);
            master = mastercompo;
        }

        public void UnsetMaster()
        {
            if (master == null)
                return;
            master.RemoveSlave(this);
            master = null;
        }

        public void AddSlave(IRenderableComponent slavecompo)
        {
            slaves.Add(slavecompo);
        }

        public void RemoveSlave(IRenderableComponent slavecompo)
        {
            if (slaves.Contains(slavecompo))
                slaves.Remove(slavecompo);
        }

        public void AddParticleSystem(string name, bool active)
        {
            if (!_emitters.ContainsKey(name))
            {
                ParticleSettings toAdd = IoCManager.Resolve<IResourceManager>().GetParticles(name);
                if (toAdd != null)
                {
                    _emitters.Add(name, new ParticleSystem(toAdd, new Vector2f()));
                    _emitters[name].Emit = active;
                }
            }

        }

        public void RemoveParticleSystem(string name)
        {
            if (_emitters.ContainsKey(name))
                _emitters.Remove(name);
        }

        public void SetParticleSystemActive(string name, bool active)
        {
            if (_emitters.ContainsKey(name))
                _emitters[name].Emit = active;
        }

        public override void HandleComponentState(dynamic _state)
        {
            ParticleSystemComponentState state = (ParticleSystemComponentState)_state;

            foreach (var a in state.emitters)
            {
                if (_emitters.ContainsKey(a.Key))
                    SetParticleSystemActive(a.Key, a.Value);
                else
                    AddParticleSystem(a.Key, a.Value);
            }

            foreach (var toRemove in new List<string>(_emitters.Keys.Except<string>(state.emitters.Keys))) //Remove emitters that are not in the new state.
                RemoveParticleSystem(toRemove);
        }
    }
}
