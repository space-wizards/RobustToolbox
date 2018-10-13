#if GODOT
using System;
using SS14.Client.Graphics.ClientEye;
using SS14.Client.Interfaces;
using SS14.Client.Interfaces.GameObjects.Components;
using SS14.Client.Utility;
using SS14.Shared.GameObjects.Components.Transform;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.IoC;
using SS14.Shared.Maths;

namespace SS14.Client.GameObjects
{
    internal class GodotTransformComponent : TransformComponent, IGodotTransformComponent
    {
        public Godot.Node2D SceneNode { get; private set; }

        IGodotTransformComponent IGodotTransformComponent.Parent => (IGodotTransformComponent)Parent;

        bool visibleWhileParented = false;
        public override bool VisibleWhileParented
        {
            get => visibleWhileParented;
            set
            {
                visibleWhileParented = value;
                UpdateSceneVisibility();
            }
        }

        protected override void SetPosition(Vector2 position)
        {
            base.SetPosition(position);
            SceneNode.Position = (position * EyeManager.PIXELSPERMETER * new Vector2(1, -1)).Rounded().Convert();
        }

        protected override void SetRotation(Angle rotation)
        {
            base.SetRotation(rotation);
            SceneNode.Rotation = -(float) rotation + MathHelper.PiOver2;
        }

        private void UpdateSceneVisibility()
        {
            SceneNode.Visible = VisibleWhileParented || IsMapTransform;
        }

        public override void AttachParent(ITransformComponent parent)
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

        public override void DetachParent()
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
            SceneNode = new Godot.Node2D
            {
                Name = $"Transform {Owner.Uid} ({Owner.Name})",
                Rotation = MathHelper.PiOver2
            };
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
#endif
