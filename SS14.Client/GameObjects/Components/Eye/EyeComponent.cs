using SS14.Client.Graphics.ClientEye;
using SS14.Client.Interfaces.GameObjects.Components;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.Maths;
using SS14.Shared.Serialization;
using SS14.Shared.ViewVariables;

namespace SS14.Client.GameObjects
{
    public class EyeComponent : Component
    {
        public override string Name => "Eye";

        private Eye eye;

        // Horrible hack to get around ordering issues.
        private bool setCurrentOnInitialize = false;
        private Vector2 setZoomOnInitialize = Vector2.One;

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
            eye.Position = transform.MapPosition;
        }
    }
}
