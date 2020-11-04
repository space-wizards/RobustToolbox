using Robust.Client.Graphics.ClientEye;
using Robust.Client.Interfaces.Graphics.ClientEye;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.Eye;
using Robust.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Robust.Client.GameObjects
{
    public class EyeComponent : SharedEyeComponent
    {
        [Dependency] private readonly IEyeManager _eyeManager = default!;

        /// <inheritdoc />
        public override string Name => "Eye";

        [ViewVariables]
        private Eye? _eye = default!;

        // Horrible hack to get around ordering issues.
        private bool setCurrentOnInitialize;
        private bool setDrawFovOnInitialize;
        private Vector2 setZoomOnInitialize = Vector2.One/2f;
        private Vector2 offset = Vector2.Zero;

        public IEye? Eye => _eye;

        [ViewVariables(VVAccess.ReadWrite)]
        public bool Current
        {
            get => _eyeManager.CurrentEye == _eye;
            set
            {
                if (_eye == null)
                {
                    setCurrentOnInitialize = value;
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
            get => _eye?.Zoom ?? setZoomOnInitialize;
            set
            {
                if (_eye == null)
                {
                    setZoomOnInitialize = value;
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
            get => offset;
            set
            {
                if(offset == value)
                    return;

                offset = value;
                UpdateEyePosition();
            }
        }

        public override bool DrawFov
        {
            get => _eye?.DrawFov ?? setDrawFovOnInitialize;
            set
            {
                if (_eye == null)
                {
                    setDrawFovOnInitialize = value;
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
                Zoom = setZoomOnInitialize,
                DrawFov = setDrawFovOnInitialize
            };

            if ((_eyeManager.CurrentEye == _eye) != setCurrentOnInitialize)
            {
                if (setCurrentOnInitialize)
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

            serializer.DataFieldCached(ref setZoomOnInitialize, "zoom", Vector2.One/2f);
            serializer.DataFieldCached(ref setDrawFovOnInitialize, "drawFov", true);
        }

        /// <summary>
        /// Updates the Eye of this entity with the transform position. This has to be called every frame to
        /// keep the view following the entity.
        /// </summary>
        public void UpdateEyePosition()
        {
            if (_eye == null) return;
            var mapPos = Owner.Transform.MapPosition;
            _eye.Position = new MapCoordinates(mapPos.Position + offset, mapPos.MapId);
        }
    }
}
