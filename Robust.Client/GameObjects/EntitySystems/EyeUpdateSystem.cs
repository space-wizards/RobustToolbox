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

        private TransformComponent? _lastParent;
        private TransformComponent? _lerpTo;
        private Angle LerpStartRotation;
        private float _accumulator;

        public bool IsLerping { get => _lerpTo != null; }

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

            EntityUid tempQualifier = _playerManager.LocalPlayer?.ControlledEntity ?? EntityUid.Invalid;
            var playerTransform = (tempQualifier != EntityUid.Invalid ? EntityManager.GetComponent<TransformComponent>(tempQualifier) : null);

            if (playerTransform == null) return;

            var gridId = playerTransform.GridID;

            TransformComponent parent;
            if (gridId != GridId.Invalid &&
                _mapManager.GetGrid(gridId).GridEntityId is var gridEnt &&
                EntityManager.EntityExists(gridEnt))
                parent = EntityManager.GetComponent<TransformComponent>(gridEnt);
            else
            {
                parent = EntityManager.GetComponent<TransformComponent>(
                    _mapManager.GetMapEntityId(playerTransform.MapID));
            }

            // Make sure that we don't fire the vomit carousel when we spawn in
            if (_lastParent is null)
                _lastParent = parent;

            // Set a default for target rotation
            var parentRotation = -parent.WorldRotation;
            // Reuse current rotation when stepping into space
            if (parent.GridID == GridId.Invalid)
                parentRotation = currentEye.Rotation;

            // Handle grid change in general
            if (_lastParent != parent)
                _lerpTo = parent;

            // And we are up and running!
            if (_lerpTo is not null)
            {
                // Handle a case where we have beeing spinning around, but suddenly got off onto a different grid
                if (parent != _lerpTo) {
                    LerpStartRotation = currentEye.Rotation;
                    _lerpTo = parent;
                    _accumulator = 0f;
                }

                _accumulator += frameTime;

                var changeNeeded = (float) (LerpStartRotation - parentRotation).Theta;

                var changeLerp = _accumulator / (Math.Abs(changeNeeded % MathF.PI) / CameraRotateSpeed * CameraRotateTimeUnit);

                currentEye.Rotation = Angle.Lerp(LerpStartRotation, parentRotation, changeLerp);

                // Either we have overshot, or we have taken way too long on this, emergency reset time
                if (changeLerp > 1.0f || _accumulator >= _lerpTimeMax)
                {
                    _lastParent = parent;
                    _lerpTo = null;
                    _accumulator = 0f;
                }
            }

            // We are just fine, or we finished a lerp (and probably overshot)
            if (_lerpTo is null)
            {
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
