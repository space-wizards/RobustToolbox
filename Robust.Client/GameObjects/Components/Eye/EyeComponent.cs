using Robust.Client.Graphics.ClientEye;
using Robust.Client.Interfaces.Graphics.ClientEye;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Robust.Client.GameObjects
{
    public class EyeComponent : Component
    {
#pragma warning disable 649
        [Dependency] private readonly IEyeManager _eyeManager;
#pragma warning restore 649

        /// <inheritdoc />
        public override string Name => "Eye";

        private Eye _eye;

        // Horrible hack to get around ordering issues.
        private bool setCurrentOnInitialize;
        private bool setDrawFovOnInitialize;
        private Vector2 setZoomOnInitialize = Vector2.One;
        private Vector2 offset = Vector2.Zero;

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

                _eyeManager.CurrentEye = value ? _eye : null;
            }
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public Vector2 Zoom
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

        [ViewVariables(VVAccess.ReadWrite)]
        public Vector2 Offset
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

        [ViewVariables(VVAccess.ReadWrite)]
        public bool DrawFov
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

            if (_eyeManager.CurrentEye == _eye != setCurrentOnInitialize)
            {
                _eyeManager.CurrentEye = setCurrentOnInitialize ? _eye : null;
            }
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

            serializer.DataFieldCached(ref setZoomOnInitialize, "zoom", Vector2.One);
            serializer.DataFieldCached(ref setDrawFovOnInitialize, "drawFov", true);
        }

        /// <summary>
        /// Updates the Eye of this entity with the transform position. This has to be called every frame to
        /// keep the view following the entity.
        /// </summary>
        public void UpdateEyePosition()
        {
            var mapPos = Owner.Transform.MapPosition;
            _eye.Position = new MapCoordinates(mapPos.Position + offset, mapPos.MapId);
        }
    }
}
