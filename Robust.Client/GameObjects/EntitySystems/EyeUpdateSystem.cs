using System;
using JetBrains.Annotations;
using Robust.Client.Interfaces.Graphics.ClientEye;
using Robust.Client.Physics;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

#nullable enable

namespace Robust.Client.GameObjects.EntitySystems
{
    /// <summary>
    /// Updates the position of every Eye every frame, so that the camera follows the player around.
    /// </summary>
    [UsedImplicitly]
    internal class EyeUpdateSystem : EntitySystem
    {
        // How fast the camera rotates in radians
        private const float CameraRotateSpeed = MathF.PI;
        private const float CameraSnapTolerance = 0.01f;

#pragma warning disable 649, CS8618
        // ReSharper disable once NotNullMemberIsNotInitialized
        [Dependency] private readonly IEyeManager _eyeManager;
#pragma warning restore 649, CS8618

        /// <inheritdoc />
        public override void Initialize()
        {
            base.Initialize();

            //WARN: Tightly couples this system with InputSystem, and assumes InputSystem exists and  is initialized
            CommandBinds.Builder
                .Bind(EngineKeyFunctions.CameraRotateRight, new NullInputCmdHandler())
                .Bind(EngineKeyFunctions.CameraRotateLeft, new NullInputCmdHandler())
                .Register<EyeUpdateSystem>();

            // Make sure this runs *after* entities have been moved by interpolation and movement.
            UpdatesAfter.Add(typeof(TransformSystem));
            UpdatesAfter.Add(typeof(PhysicsSystem));
        }

        /// <inheritdoc />
        public override void Shutdown()
        {
            //WARN: Tightly couples this system with InputSystem, and assumes InputSystem exists and is initialized
            CommandBinds.Unregister<EyeUpdateSystem>();
            base.Shutdown();
        }

        /// <inheritdoc />
        public override void FrameUpdate(float frameTime)
        {
            var currentEye = _eyeManager.CurrentEye;
            var inputSystem = EntitySystemManager.GetEntitySystem<InputSystem>();

            var direction = 0;
            if (inputSystem.CmdStates[EngineKeyFunctions.CameraRotateRight] == BoundKeyState.Down)
            {
                direction += 1;
            }

            if (inputSystem.CmdStates[EngineKeyFunctions.CameraRotateLeft] == BoundKeyState.Down)
            {
                direction -= 1;
            }

            // apply camera rotation
            if(direction != 0)
            {
                currentEye.Rotation += CameraRotateSpeed * frameTime * direction;
                currentEye.Rotation = currentEye.Rotation.Reduced();
            }
            else
            {
                // snap to cardinal directions
                var closestDir = currentEye.Rotation.GetCardinalDir().ToVec();
                var currentDir = currentEye.Rotation.ToVec();

                var dot = Vector2.Dot(closestDir, currentDir);
                if (MathHelper.CloseTo(dot, 1, CameraSnapTolerance))
                {
                    currentEye.Rotation = closestDir.ToAngle();
                }
            }

            foreach (var eyeComponent in EntityManager.ComponentManager.EntityQuery<EyeComponent>(true))
            {
                eyeComponent.UpdateEyePosition();
            }
        }
    }
}
