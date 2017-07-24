using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.System;
using SS14.Shared.IoC;

namespace SS14.Client.GameObjects.EntitySystems
{
    public class InputSystem : EntitySystem
    {
        public InputSystem()
        {
            EntityQuery = new EntityQuery();
            EntityQuery.OneSet.Add(typeof(KeyBindingInputComponent));
        }

        public override void Update(float frametime)
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
