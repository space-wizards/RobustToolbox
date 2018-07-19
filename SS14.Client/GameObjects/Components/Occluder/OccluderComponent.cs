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
using SS14.Shared.Serialization;

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

        const string BoxCache = "occluderbox";

        public override void Initialize()
        {
            base.Initialize();
            lightManager = IoCManager.Resolve<ILightManager>();
            var transform = Owner.GetComponent<IGodotTransformComponent>();

            occluder.ParentTo(transform);
        }

        public override void OnRemove()
        {
            occluder.Dispose();
            occluder = null;

            base.OnRemove();
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            if (lightManager == null)
            {
                // First in the init stack so...
                // FIXME: This is terrible.
                lightManager = IoCManager.Resolve<ILightManager>();
                occluder = lightManager.MakeOccluder();
            }

            base.ExposeData(serializer);

            serializer.DataReadWriteFunction("enabled", true, en => Enabled = en, () => Enabled);

            if (serializer.Writing)
            {
                serializer.DataWriteFunction("box", new Box2(-16, -16, 16, 16), () => BoundingBox);
                return;
            }

            // Shortcut so modifications on-map are read easily.
            if (serializer.TryReadDataFieldCached("box", out Box2 box))
            {
                BoundingBox = box;
            }

            if (serializer.TryGetCacheData(BoxCache, out box))
            {
                BoundingBox = box;
                return;
            }

            var sizeX = serializer.ReadDataField("sizeX", 32f);
            var sizeY = serializer.ReadDataField("sizeY", 32f);
            var offsetX = serializer.ReadDataField("offsetX", -16f);
            var offsetY = serializer.ReadDataField("offsetY", -16f);

            box = Box2.FromDimensions(offsetX, offsetY, sizeX, sizeY);

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
