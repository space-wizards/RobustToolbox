using SS14.Client.Graphics.ClientEye;
using SS14.Client.Interfaces.GameObjects.Components;
using SS14.Shared.GameObjects;

namespace SS14.Client.GameObjects
{
    public class EyeComponent : Component
    {
        public override string Name => "Eye";

        private Eye eye;

        // Horrible hack to get around ordering issues.
        private bool setCurrentOnInitialize = false;
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

        public override void Initialize()
        {
            base.Initialize();
            var transform = Owner.GetComponent<IClientTransformComponent>();
            eye = new Eye();
            transform.SceneNode.AddChild(eye.GodotCamera);
            eye.Current = setCurrentOnInitialize;
        }

        public override void OnRemove()
        {
            base.OnRemove();
            eye.Dispose();
        }
    }
}
