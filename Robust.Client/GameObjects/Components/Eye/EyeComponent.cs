using Robust.Client.Graphics.ClientEye;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Robust.Client.GameObjects
{
    public class EyeComponent : Component
    {
        public override string Name => "Eye";

        private Eye eye;

        // Horrible hack to get around ordering issues.
        private bool setCurrentOnInitialize;
        private Vector2 setZoomOnInitialize = Vector2.One;
        private Vector2 offset = Vector2.Zero;

        [ViewVariables(VVAccess.ReadWrite)]
        public bool Current
        {
            get => eye.Current;
            set
            {
                if (eye == null)
                {
                    setCurrentOnInitialize = value;
                    return;
                }

                eye.Current = value;
            }
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public Vector2 Zoom
        {
            get => eye?.Zoom ?? setZoomOnInitialize;
            set
            {
                if (eye == null)
                {
                    setZoomOnInitialize = value;
                }
                else
                {
                    eye.Zoom = value;
                }
            }
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public Vector2 Offset
        {
            get => offset;
            set
            {
                offset = value;
                _updateCameraPosition();
            }
        }

        private ITransformComponent transform;

        public override void Initialize()
        {
            base.Initialize();

            transform = Owner.GetComponent<ITransformComponent>();
            eye = new Eye
            {
                Current = setCurrentOnInitialize,
                Position = transform.MapPosition,
                Zoom = setZoomOnInitialize,
            };

            transform.OnMove += Transform_OnMove;

            if (GameController.OnGodot)
            {
                Owner.GetComponent<IGodotTransformComponent>().SceneNode.AddChild(eye.GodotCamera);
            }
        }

        public override void OnRemove()
        {
            base.OnRemove();
            transform.OnMove -= Transform_OnMove;

            eye.Dispose();
            eye = null;
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataFieldCached(ref setZoomOnInitialize, "zoom", Vector2.One);
        }

        private void Transform_OnMove(object sender, Shared.Enums.MoveEventArgs e)
        {
            _updateCameraPosition();
        }

        private void _updateCameraPosition()
        {
            var pos = transform.MapPosition.Position + offset;
            eye.Position = new MapCoordinates(pos, transform.MapPosition.MapId);
        }
    }
}
