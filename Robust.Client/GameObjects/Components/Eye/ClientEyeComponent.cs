using System;
using Robust.Client.Graphics;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Shared.Players;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Robust.Client.GameObjects
{
    [RegisterComponent]
    [ComponentReference(typeof(EyeComponent))]
    internal class ClientEyeComponent : EyeComponent
    {
        [Dependency] private readonly IEyeManager _eyeManager = default!;

        /// <summary>
        /// Maximum rate of magnitude restore towards 0 kick.
        /// </summary>
        private const float RestoreRateMax = 15f;

        /// <summary>
        /// Minimum rate of magnitude restore towards 0 kick.
        /// </summary>
        private const float RestoreRateMin = 1f;

        /// <summary>
        /// Time in seconds since the last kick that lerps RestoreRateMin and RestoreRateMax
        /// </summary>
        private const float RestoreRateRamp = 0.1f;

        /// <summary>
        /// The maximum magnitude of the kick applied to the camera at any point.
        /// </summary>
        private const float KickMagnitudeMax = 2f;

        private Vector2 _currentKick;
        private float _lastKickTime;

        /// <inheritdoc />
        public override string Name => "Eye";

        [ViewVariables]
        private Eye? _eye;

        // Horrible hack to get around ordering issues.
        private bool _setCurrentOnInitialize;
        private bool _setDrawFovOnInitialize;
        private Vector2 _setZoomOnInitialize = Vector2.One/2f;
        private Vector2 _offset = Vector2.Zero;

        public IEye? Eye => _eye;

        [ViewVariables(VVAccess.ReadWrite)]
        public bool Current
        {
            get => _eyeManager.CurrentEye == _eye;
            set
            {
                if (_eye == null)
                {
                    _setCurrentOnInitialize = value;
                    return;
                }

                if (_eyeManager.CurrentEye == _eye == value)
                    return;

                if (value)
                {
                    _eyeManager.CurrentEye = _eye;
                }
                else
                {
                    _eyeManager.ClearCurrentEye();
                }
            }
        }

        public override Vector2 Zoom
        {
            get => _eye?.Zoom ?? _setZoomOnInitialize;
            set
            {
                if (_eye == null)
                {
                    _setZoomOnInitialize = value;
                }
                else
                {
                    _eye.Zoom = value;
                }
            }
        }

        public override Angle Rotation
        {
            get => _eye?.Rotation ?? Angle.Zero;
            set
            {
                if (_eye != null)
                    _eye.Rotation = value;
            }
        }

        public override Vector2 Offset
        {
            get => _offset;
            set
            {
                if(_offset.EqualsApprox(value))
                    return;

                _offset = value;
                UpdateEyePosition();
            }
        }

        public override bool DrawFov
        {
            get => _eye?.DrawFov ?? _setDrawFovOnInitialize;
            set
            {
                if (_eye == null)
                {
                    _setDrawFovOnInitialize = value;
                }
                else
                {
                    _eye.DrawFov = value;
                }
            }
        }

        [ViewVariables]
        public MapCoordinates? Position => _eye?.Position;

        /// <inheritdoc />
        public override void Initialize()
        {
            base.Initialize();

            _eye = new Eye
            {
                Position = Owner.Transform.MapPosition,
                Zoom = _setZoomOnInitialize,
                DrawFov = _setDrawFovOnInitialize
            };

            if ((_eyeManager.CurrentEye == _eye) != _setCurrentOnInitialize)
            {
                if (_setCurrentOnInitialize)
                {
                    _eyeManager.ClearCurrentEye();
                }
                else
                {
                    _eyeManager.CurrentEye = _eye;
                }
            }
        }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (!(curState is EyeComponentState state))
            {
                return;
            }

            DrawFov = state.DrawFov;
            Zoom = state.Zoom;
            Offset = state.Offset;
            Rotation = state.Rotation;
        }

        public override void OnRemove()
        {
            base.OnRemove();

            Current = false;
        }

        /// <inheritdoc />
        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataFieldCached(ref _setZoomOnInitialize, "zoom", Vector2.One/2f);
            serializer.DataFieldCached(ref _setDrawFovOnInitialize, "drawFov", true);
        }

        /// <summary>
        /// Updates the Eye of this entity with the transform position. This has to be called every frame to
        /// keep the view following the entity.
        /// </summary>
        public void UpdateEyePosition()
        {
            if (_eye == null) return;
            var mapPos = Owner.Transform.MapPosition;
            _eye.Position = new MapCoordinates(mapPos.Position + _offset, mapPos.MapId);
        }

        public void UpdateViewKick(float frameTime)
        {
            var magnitude = _currentKick.Length;
            if (magnitude <= 0.005f)
            {
                _currentKick = Vector2.Zero;
                _updateEye();
                return;
            }

            // Continually restore camera to 0.
            var normalized = _currentKick.Normalized;
            _lastKickTime += frameTime;
            var restoreRate = MathHelper.Lerp(RestoreRateMin, RestoreRateMax, Math.Min(1, _lastKickTime/RestoreRateRamp));
            var restore = normalized * restoreRate * frameTime;
            var (x, y) = _currentKick - restore;
            if (Math.Sign(x) != Math.Sign(_currentKick.X))
            {
                x = 0;
            }

            if (Math.Sign(y) != Math.Sign(_currentKick.Y))
            {
                y = 0;
            }

            _currentKick = (x, y);

            _updateEye();
        }

        private void _updateEye()
        {
            Offset = BaseOffset + _currentKick;
        }

        public override void Kick(Vector2 recoil)
        {
            if (float.IsNaN(recoil.X) || float.IsNaN(recoil.Y))
            {
                Logger.Error($"CameraRecoilComponent on entity {Owner.Uid} passed a NaN recoil value. Ignoring.");
                return;
            }

            // Use really bad math to "dampen" kicks when we're already kicked.
            var existing = _currentKick.Length;
            var dampen = existing/KickMagnitudeMax;
            _currentKick += recoil * (1-dampen);
            if (_currentKick.Length > KickMagnitudeMax)
            {
                _currentKick = _currentKick.Normalized * KickMagnitudeMax;
            }

            _lastKickTime = 0;
            _updateEye();
        }

        public override void HandleNetworkMessage(ComponentMessage message, INetChannel channel, ICommonSession? session = null)
        {
            base.HandleNetworkMessage(message, channel, session);

            switch (message)
            {
                case RecoilKickMessage msg:
                    Kick(msg.Recoil);
                    break;
            }
        }
    }
}
