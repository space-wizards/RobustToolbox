using System.Numerics;
using Robust.Client.Graphics;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Robust.Client.GameObjects
{
    [RegisterComponent, ComponentReference(typeof(SharedEyeComponent))]
    public sealed class EyeComponent : SharedEyeComponent
    {
        [Dependency] private readonly IEyeManager _eyeManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;

        [ViewVariables] internal Eye? _eye = default!;

        // Horrible hack to get around ordering issues.
        internal bool _setCurrentOnInitialize;
        [DataField("drawFov")] internal bool _setDrawFovOnInitialize = true;
        [DataField("zoom")] internal Vector2 _setZoomOnInitialize = Vector2.One;

        /// <summary>
        ///     If not null, this entity is used to update the eye's position instead of just using the component's owner.
        /// </summary>
        /// <remarks>
        ///     This is useful for things like vehicles that effectively need to hijack the eye. This allows them to do
        ///     that without messing with the main viewport's eye. This is important as there are some overlays that are
        ///     only be drawn if that viewport's eye belongs to the currently controlled entity.
        /// </remarks>
        [ViewVariables]
        public EntityUid? Target;

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

        /// <summary>
        /// Updates the Eye of this entity with the transform position. This has to be called every frame to
        /// keep the view following the entity.
        /// </summary>
        public void UpdateEyePosition()
        {
            if (_eye == null) return;

            if (!_entityManager.TryGetComponent(Target, out TransformComponent? xform))
            {
                xform = _entityManager.GetComponent<TransformComponent>(Owner);
                Target = null;
            }

            _eye.Position = xform.MapPosition;
        }
    }
}
