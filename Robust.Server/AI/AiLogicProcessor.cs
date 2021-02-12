using System;
using Robust.Shared.GameObjects;

namespace Robust.Server.AI
{
    /// <summary>
    ///     Base class for all AI Processors.
    /// </summary>
    public abstract class AiLogicProcessor : IEquatable<AiLogicProcessor>
    {
        /// <summary>
        ///     Radius in meters that the AI can "see".
        /// </summary>
        public float VisionRadius { get; set; }

        /// <summary>
        ///     Entity this AI is controlling.
        /// </summary>
        public IEntity SelfEntity
        {
            get => _selfEntity;
            set
            {
                if (_selfEntity == value)
                    return;
                
                if (_selfEntity != default)
                    throw new InvalidOperationException();

                _selfEntity = value;
            }
        }

        private IEntity _selfEntity = default!;

        /// <summary>
        ///     One-Time setup when the processor is created.
        /// </summary>
        public virtual void Setup() { }

        /// <summary>
        /// One-Time shutdown when processor is done
        /// </summary>
        public virtual void Shutdown() {}

        /// <summary>
        ///     Gives life to the AI.
        /// </summary>
        /// <param name="frameTime">Time since last update in seconds.</param>
        public abstract void Update(float frameTime);

        public bool Equals(AiLogicProcessor? other)
        {
            if (other == null)
                return false;
            
            return SelfEntity.Uid.Equals(other.SelfEntity.Uid);
        }
        public override int GetHashCode()
        {
            // SelfEntity should never be set after the initial one anyway.
            // Long-term more stuff will be moved to yaml and this is subject to a refactor.
            return SelfEntity.Uid.GetHashCode();
        }
    }
}
