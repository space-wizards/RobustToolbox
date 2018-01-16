using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.System;
using SS14.Shared.IoC;
using System;
using System.Collections.Generic;
using SS14.Shared.Enums;

namespace SS14.Server.GameObjects.EntitySystems
{
    public class InputSystem : EntitySystem
    {
        public InputSystem()
        {
            EntityQuery = new ComponentEntityQuery()
            {
                OneSet = new List<Type>()
                {
                    typeof(KeyBindingInputComponent),
                },
            };
        }

        public override void Update(float frametime)
        {
            var entities = EntityManager.GetEntities(EntityQuery);
            foreach (var entity in entities)
            {
                var inputs = entity.GetComponent<KeyBindingInputComponent>();

                //Animation setting
                if (entity.TryGetComponent<AnimatedSpriteComponent>(out var animation))
                {
                    if (inputs.GetKeyState(BoundKeyFunctions.MoveRight) ||
                        inputs.GetKeyState(BoundKeyFunctions.MoveDown) ||
                        inputs.GetKeyState(BoundKeyFunctions.MoveLeft) ||
                        inputs.GetKeyState(BoundKeyFunctions.MoveUp))
                    {
                        if (inputs.GetKeyState(BoundKeyFunctions.Run))
                        {
                            animation.SetAnimationState("run");
                        }
                        else
                        {
                            animation.SetAnimationState("walk");
                        }
                    }
                    else
                    {
                        animation.SetAnimationState("idle");
                    }
                }
            }
        }
    }
}
