using SFML.Graphics;
using SFML.System;
using SS14.Shared.GameObjects;
using SS14.Shared.IoC;
using System;

namespace SS14.Client.GameObjects
{
    public class HitboxComponent : Component
    {
        public override string Name => "Hitbox";
        public override uint? NetID => NetIDs.HITBOX;
        public FloatRect AABB { get; set; }
        public Vector2f Size
        {
            get
            {
                return new Vector2f(AABB.Width, AABB.Height);
            }
            set
            {
                AABB = new FloatRect(
                    AABB.Left + (AABB.Width - value.X),
                    AABB.Top + (AABB.Height - value.Y),
                    value.X,
                    value.Y
                    );
            }
        }
        public Vector2f Offset
        {
            get
            {
                return new Vector2f(AABB.Left + AABB.Width / 2f, AABB.Top + AABB.Height / 2f);
            }
            set
            {
                AABB = new FloatRect(
                    value.X - AABB.Width / 2f,
                    value.Y - AABB.Height / 2f,
                    AABB.Width,
                    AABB.Height
                    );
            }
        }

        public HitboxComponent()
        {
            Size = new Vector2f();
            Offset = new Vector2f();
        }

        public override Type StateType => typeof(HitboxComponentState);

        /// <inheritdoc />
        public override void HandleComponentState(ComponentState state)
        {
            AABB = ((HitboxComponentState)state).AABB;
        }
    }
}
