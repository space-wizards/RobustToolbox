using System;
using SS14.Client.Interfaces.GameObjects.Components;
using SS14.Client.Interfaces.Graphics.Lighting;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.IoC;
using SS14.Shared.Utility;
using YamlDotNet.RepresentationModel;
using SS14.Client.Graphics.Lighting;
using SS14.Shared.Maths;

namespace SS14.Client.GameObjects
{
    public class OccluderComponent : Component
    {
        public override string Name => "Occluder";
        public Box2 BoundingBox { get; private set; } = new Box2(-16, -16, 16, 16);
        public bool Enabled
        {
            get => occluder.Enabled;
            set => occluder.Enabled = value;
        }

        private IOccluder occluder;
        private ILightManager lightManager;

        public override void Initialize()
        {
            base.Initialize();
            lightManager = IoCManager.Resolve<ILightManager>();
            var transform = Owner.GetComponent<IClientTransformComponent>();

            occluder.ParentTo(transform);
        }

        public override void Spawned()
        {
            lightManager = IoCManager.Resolve<ILightManager>();
            occluder = lightManager.MakeOccluder();
        }

        public override void OnRemove()
        {
            occluder.Dispose();
            occluder = null;

            base.OnRemove();
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
                Enabled = node.AsBool();
            }

            var poly = new Godot.Vector2[4]
            {
                new Godot.Vector2(BoundingBox.Left, BoundingBox.Top),
                new Godot.Vector2(BoundingBox.Right, BoundingBox.Top),
                new Godot.Vector2(BoundingBox.Right, BoundingBox.Bottom),
                new Godot.Vector2(BoundingBox.Left, BoundingBox.Bottom),
            };

            occluder.SetGodotPolygon(poly);
        }
    }
}
