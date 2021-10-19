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
        // How fast the camera rotates in radians
        private const float CameraRotateSpeed = MathF.PI;

        [Dependency] private readonly IEyeManager _eyeManager = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;

        private bool _isLerping = false;
        private ITransformComponent? _lastParent;

        private float _accumulator;

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

            if (parent != _lastParent)
            {
                _accumulator += frameTime;

                if (_accumulator >= 0.3f)
                {
                    _accumulator = 0f;
                    _lastParent = parent;
                }
            }
            else
            {
                _lastParent = parent;
            }

            if (_lastParent == parent)
            {
                // TODO: Detect parent change and start lerping
                var parentRotation = parent.WorldRotation;
                currentEye.Rotation = -parentRotation;
            }

            foreach (var eyeComponent in EntityManager.EntityQuery<EyeComponent>(true))
            {
                eyeComponent.UpdateEyePosition();
            }
        }
    }
}
