/*
using OpenTK;
using SS14.Client.Graphics;
using SS14.Client.Interfaces.GameObjects;
using SS14.Client.Interfaces.Resource;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.IoC;
using System;
using System.Collections.Generic;
using System.Linq;
using SS14.Shared.Utility;
using SS14.Shared.Map;
using Vector2 = SS14.Shared.Maths.Vector2;

namespace SS14.Client.GameObjects
{
    public class ParticleSystemComponent : Component, IParticleSystemComponent, IRenderableComponent
    {
        public override string Name => "ParticleSystem";
        public override uint? NetID => NetIDs.PARTICLE_SYSTEM;
        #region Variables.
        private Dictionary<string, ParticleSystem> _emitters = new Dictionary<string, ParticleSystem>(); // List of particle emitters.
        protected IRenderableComponent master;
        protected List<IRenderableComponent> slaves = new List<IRenderableComponent>();
        public int MapID { get; private set; }

        public DrawDepth DrawDepth { get; set; } = DrawDepth.ItemsOnTables;
        #endregion Variables.

        #region Properties
        public Box2 AverageAABB => LocalAABB;

        public Box2 LocalAABB => new Box2();

        #endregion Properties


        public override Type StateType => typeof(ParticleSystemComponentState);

        public void OnMove(object sender, MoveEventArgs args)
        {
            var offset = new Vector2(args.NewPosition.Position.X, args.NewPosition.Position.Y) -
                         new Vector2(args.OldPosition.Position.X, args.OldPosition.Position.Y);
            foreach (KeyValuePair<string, ParticleSystem> particleSystem in _emitters)
            {
                particleSystem.Value.MoveEmitter(particleSystem.Value.EmitterPosition + offset);
            }
            MapID = args.NewPosition.MapID;
        }

        public override void OnAdd(IEntity owner)
        {
            base.OnAdd(owner);
            var transform = Owner.GetComponent<ITransformComponent>();
            transform.OnMove += OnMove;
        }

        public override void Shutdown()
        {
            var transform = Owner.GetComponent<ITransformComponent>();
            transform.OnMove -= OnMove;
            base.Shutdown();
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
            ScreenCoordinates renderPos = CluwneLib.WorldToScreen(Owner.GetComponent<ITransformComponent>().LocalPosition);

            foreach (KeyValuePair<string, ParticleSystem> particleSystem in _emitters)
            {
                particleSystem.Value.Move(renderPos.Position);
                particleSystem.Value.Render();
            }
        }

        public float Bottom
        {
            get
            {
                return Owner.GetComponent<ITransformComponent>().WorldPosition.Y;
            }
        }

        public bool IsSlaved()
        {
            return master != null;
        }

        public void SetMaster(IEntity m)
        {
            if (!m.HasComponent<IRenderableComponent>())
                return;
            var mastercompo = m.GetComponent<IRenderableComponent>();
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
                ParticleSettings toAdd = IoCManager.Resolve<IResourceCache>().GetParticles(name);
                if (toAdd != null)
                {
                    _emitters.Add(name, new ParticleSystem(toAdd, new Vector2()));
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

        /// <inheritdoc />
        public override void HandleComponentState(ComponentState state)
        {
            var newState = (ParticleSystemComponentState) state;

            foreach (var a in newState.emitters)
                if (_emitters.ContainsKey(a.Key))
                    SetParticleSystemActive(a.Key, a.Value);
                else
                    AddParticleSystem(a.Key, a.Value);

            //Remove emitters that are not in the new state.
            foreach (var toRemove in new List<string>(_emitters.Keys.Except(newState.emitters.Keys)))
                RemoveParticleSystem(toRemove);
        }
    }
}
*/
