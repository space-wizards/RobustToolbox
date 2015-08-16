using SS14.Client.Graphics;
using SS14.Client.Interfaces.GOC;
using SS14.Client.Interfaces.Map;
using SS14.Client.Interfaces.Resource;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.GO;
using SS14.Shared.GO.Component.Particles;
using SS14.Shared.IoC;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using SS14.Shared.Maths;

namespace SS14.Client.GameObjects
{
    public class ParticleSystemComponent : Component, IParticleSystemComponent, IRenderableComponent
    {
        #region Variables.
        private Dictionary<string, ParticleSystem> _emitters = new Dictionary<string, ParticleSystem>(); // List of particle emitters.
        protected IRenderableComponent master;
        protected List<IRenderableComponent> slaves = new List<IRenderableComponent>();

        public DrawDepth DrawDepth { get; set; }
        #endregion

        #region Properties
        public RectangleF AverageAABB
        {
            get { return AABB; }
        }

        public RectangleF AABB
        {
            get { return RectangleF.Empty; }
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
            var offset = new Vector2(args.VectorTo.X, args.VectorTo.Y) -
                         new Vector2(args.VectorFrom.X, args.VectorFrom.Y);
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

        public override ComponentReplyMessage RecieveMessage(object sender, ComponentMessageType type,
                                                             params object[] list)
        {
            ComponentReplyMessage reply = base.RecieveMessage(sender, type, list);

            if (sender == this) //Don't listen to our own messages!
                return ComponentReplyMessage.Empty;

            switch (type)
            {
            }

            return reply;
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            foreach (KeyValuePair<string, ParticleSystem> particleSystem in _emitters)
            {
                particleSystem.Value.Update(frameTime);
            }
        }

        public virtual void Render(Vector2 topLeft, Vector2 bottomRight)
        {
            int tileSize = IoCManager.Resolve<IMapManager>().TileSize;

            Vector2 renderPos =
                    Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position * tileSize;

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
                    _emitters.Add(name, new ParticleSystem(toAdd, Vector2.Zero));
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