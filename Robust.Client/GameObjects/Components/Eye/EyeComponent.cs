using Robust.Client.Graphics;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Robust.Client.GameObjects
{
    [ComponentReference(typeof(SharedEyeComponent))]
    public class EyeComponent : SharedEyeComponent
    {
        [Dependency] private readonly IEyeManager _eyeManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;

        /// <inheritdoc />
        public override string Name => "Eye";

        [ViewVariables]
        private Eye? _eye = default!;

        // Horrible hack to get around ordering issues.
        private bool _setCurrentOnInitialize;
        [DataField("drawFov")]
        private bool _setDrawFovOnInitialize = true;
        [DataField("zoom")]
        private Vector2 _setZoomOnInitialize = Vector2.One;

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
            get => _eye?.Offset ?? default;
            set
            {
                if (_eye != null)
                    _eye.Offset = value;
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
        protected override void Initialize()
        {
            base.Initialize();

            _eye = new Eye
            {
                Position = _entityManager.GetComponent<TransformComponent>(Owner).MapPosition,
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
            VisibilityMask = state.VisibilityMask;
        }

        protected override void OnRemove()
        {
            base.OnRemove();

            Current = false;
        }

        /// <summary>
        /// Updates the Eye of this entity with the transform position. This has to be called every frame to
        /// keep the view following the entity.
        /// </summary>
        public void UpdateEyePosition()
        {
            if (_eye == null) return;
            var mapPos = _entityManager.GetComponent<TransformComponent>(Owner).MapPosition;
            _eye.Position = new MapCoordinates(mapPos.Position, mapPos.MapId);
        }
    }
}
