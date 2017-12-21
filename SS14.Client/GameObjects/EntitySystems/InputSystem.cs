/*
TODO: Godot
Literally ALL this does is change the animation on the player mobs.
WHY is this an entire entity system.
WHY.
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.System;
using System;
using System.Collections.Generic;

namespace SS14.Client.GameObjects.EntitySystems
{
    public class InputSystem : EntitySystem
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
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

        /// <inheritdoc />
        public override void Update(float frameTime)
        {
            var entities = EntityManager.GetEntities(EntityQuery);
            foreach (var entity in entities)
            {
                var inputs = entity.GetComponent<KeyBindingInputComponent>();
                //Animation setting
                if (entity.TryGetComponent<AnimatedSpriteComponent>(out var component))
                {
                    //Char is moving
                    if (inputs.GetKeyState(BoundKeyFunctions.MoveRight) ||
                        inputs.GetKeyState(BoundKeyFunctions.MoveDown) ||
                        inputs.GetKeyState(BoundKeyFunctions.MoveLeft) ||
                        inputs.GetKeyState(BoundKeyFunctions.MoveUp))
                    {
                        component.SetAnimationState(inputs.GetKeyState(BoundKeyFunctions.Run) ? "run" : "walk");
                    }
                    //Char is not moving
                    else
                    {
                        component.SetAnimationState("idle");
                    }
                }
            }
        }
    }
}
*/
