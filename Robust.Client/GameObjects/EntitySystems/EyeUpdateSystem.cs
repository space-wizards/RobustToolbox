using System;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.Physics;
using Robust.Client.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

#nullable enable

namespace Robust.Client.GameObjects
{
    /// <summary>
    /// Updates the position of every Eye every frame, so that the camera follows the player around.
    /// </summary>
    [UsedImplicitly]
    internal class EyeUpdateSystem : EntitySystem
    {
        [Dependency] private readonly IEyeManager _eyeManager = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;

        private ITransformComponent? _lastParent;
        private ITransformComponent? _lerpTo;
        private Angle LerpStartRotation;
        private float _accumulator;

        public bool IsLerping = false;

        // How fast the camera rotates in radians / s
        private const float CameraRotateSpeed = MathF.PI;
        // PER THIS AMOUNT OF TIME MILLISECONDS
        private const float CameraRotateTimeUnit = 1.2f;
        // Safety override
        private const float _lerpTimeMax = CameraRotateTimeUnit + 0.4f;

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

            // TODO: Content should have its own way of handling this. We should have a default behavior that they can overwrite.

            var playerTransform = _playerManager.LocalPlayer?.ControlledEntity?.Transform;

            if (playerTransform == null) return;

            var gridId = playerTransform.GridID;

            var parent = gridId != GridId.Invalid && EntityManager.TryGetEntity(_mapManager.GetGrid(gridId).GridEntityId, out var gridEnt) ?
                gridEnt.Transform
                : _mapManager.GetMapEntity(playerTransform.MapID).Transform;

            if (_lastParent is null)
                _lastParent = parent;

            var parentRotation = -parent.WorldRotation;
            if (parent.GridID == GridId.Invalid)
            {
                parentRotation = currentEye.Rotation;
            }

            if (_lastParent != parent)
            {
                IsLerping = true;
                if (parent != _lerpTo) {
                    LerpStartRotation = currentEye.Rotation;
                    _lerpTo = parent;
                }

                _accumulator += frameTime;

                var changeNeeded = (float) (LerpStartRotation - parentRotation).Theta;

                var changeLerp = _accumulator / (Math.Abs(changeNeeded % MathF.PI) / CameraRotateSpeed * CameraRotateTimeUnit);

                System.Console.Out.WriteLine($"+{frameTime} ({_accumulator}): {changeLerp}, {-currentEye.Rotation} {-parentRotation} ");

                currentEye.Rotation = Angle.Lerp(LerpStartRotation, parentRotation, changeLerp);

                if (changeLerp > 1.0f || _accumulator >= _lerpTimeMax)
                {
                    _lastParent = parent;
                    _accumulator = 0f;
                }
            }

            if (_lastParent == parent)
            {
                IsLerping = false;
                currentEye.Rotation = parentRotation;
                LerpStartRotation = parentRotation;
            }

            foreach (var eyeComponent in EntityManager.EntityQuery<EyeComponent>(true))
            {
                eyeComponent.UpdateEyePosition();
            }
        }
    }
}
