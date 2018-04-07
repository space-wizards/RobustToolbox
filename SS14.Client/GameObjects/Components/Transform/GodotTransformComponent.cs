using SS14.Client.Graphics.ClientEye;
using SS14.Client.Interfaces;
using SS14.Client.Interfaces.GameObjects.Components;
using SS14.Client.Utility;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.IoC;
using SS14.Shared.Maths;


namespace SS14.Client.GameObjects
{
    public class GodotTransformComponent : ClientTransformComponent, IGodotTransformComponent
    {
        public Godot.Node2D SceneNode { get; private set; }

        IGodotTransformComponent IGodotTransformComponent.Parent => (IGodotTransformComponent)Parent;

        protected override void SetPosition(Vector2 position)
        {
            base.SetPosition(position);
            SceneNode.Position = (position * EyeManager.PIXELSPERMETER).Rounded().Convert();
        }

        private void UpdateSceneVisibility()
        {
            SceneNode.Visible = IsMapTransform;
        }

        protected override void AttachParent(ITransformComponent parent)
        {
            if (parent == null)
            {
                return;
            }

            base.AttachParent(parent);
            SceneNode.GetParent().RemoveChild(SceneNode);
            ((IGodotTransformComponent)parent).SceneNode.AddChild(SceneNode);
            UpdateSceneVisibility();
        }

        protected override void DetachParent()
        {
            if (Parent == null)
            {
                return;
            }

            ((IGodotTransformComponent)Parent).SceneNode.RemoveChild(SceneNode);
            base.DetachParent();
            var holder = IoCManager.Resolve<ISceneTreeHolder>();
            holder.WorldRoot.AddChild(SceneNode);
            UpdateSceneVisibility();
        }

        public override void OnAdd()
        {
            base.OnAdd();
            var holder = IoCManager.Resolve<ISceneTreeHolder>();
            SceneNode = new Godot.Node2D();
            SceneNode.SetName($"Transform {Owner.Uid} ({Owner.Name})");
            holder.WorldRoot.AddChild(SceneNode);
        }

        public override void OnRemove()
        {
            base.OnRemove();

            SceneNode.QueueFree();
            SceneNode.Dispose();
            SceneNode = null;
        }
    }
}
