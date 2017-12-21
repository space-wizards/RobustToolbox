/*
TODO: Godot
using System;
using OpenTK;
using SS14.Shared.GameObjects;
using SS14.Shared.Utility;
using YamlDotNet.RepresentationModel;
using SS14.Shared.IoC;
using SS14.Shared.Interfaces.GameObjects.Components;

namespace SS14.Client.GameObjects
{
    public class OccluderComponent : Component
    {
        public override string Name => "Occluder";

        public Box2 BoundingBox { get; private set; } = new Box2(-16, -16, 16, 16);
        private bool enabled = true;
        public bool Enabled
        {
            get => enabled;
            set
            {
                if (value == enabled)
                {
                    return;
                }

                enabled = value;
                RedrawRelevantLights();
            }
        }

        private ITransformComponent transform;

        public override void Initialize()
        {
            base.Initialize();
            transform = Owner.GetComponent<ITransformComponent>();
            transform.OnMove += Transform_OnMove;
        }

        private void Transform_OnMove(object sender, Shared.MoveEventArgs e)
        {
            var oldpos = e.OldPosition.Grid.ConvertToWorld(e.OldPosition.Position);
            var newpos = e.NewPosition.Grid.ConvertToWorld(e.NewPosition.Position);
            RedrawRelevantLights(newpos);
            RedrawRelevantLights(oldpos);
        }

        public override void OnRemove()
        {
            base.OnRemove();
            // Tell lights to update for our deletion.
            Enabled = false;
            transform.OnMove -= Transform_OnMove;
            transform = null;
        }

        private void RedrawRelevantLights() => RedrawRelevantLights(transform.WorldPosition);
        private void RedrawRelevantLights(Vector2 position)
        {
            var mgr = IoCManager.Resolve<ILightManager>();
            mgr.RecalculateLightsInView(BoundingBox.Translated(position));
        }

        public override void LoadParameters(YamlMappingNode mapping)
        {
            base.LoadParameters(mapping);

            YamlNode node;
            if (mapping.TryGetNode("sizeX", out node))
            {
                var width = node.AsFloat();
                BoundingBox = Box2.FromDimensions(BoundingBox.Left + (BoundingBox.Width - width) / 2f, BoundingBox.Top, width, BoundingBox.Height);
            }

            if (mapping.TryGetNode("sizeY", out node))
            {
                var height = node.AsFloat();
                BoundingBox = Box2.FromDimensions(BoundingBox.Left, BoundingBox.Top + (BoundingBox.Height - height) / 2f, BoundingBox.Width, height);
            }

            if (mapping.TryGetNode("offsetX", out node))
            {
                var x = node.AsFloat();
                BoundingBox = Box2.FromDimensions(x - BoundingBox.Width / 2f, BoundingBox.Top, BoundingBox.Width, BoundingBox.Height);
            }

            if (mapping.TryGetNode("offsetY", out node))
            {
                var y = node.AsFloat();
                BoundingBox = Box2.FromDimensions(BoundingBox.Left, y - BoundingBox.Height / 2f, BoundingBox.Width, BoundingBox.Height);
            }

            if (mapping.TryGetNode("enabled", out node))
            {
                enabled = node.AsBool();
            }
        }
    }
}
*/
