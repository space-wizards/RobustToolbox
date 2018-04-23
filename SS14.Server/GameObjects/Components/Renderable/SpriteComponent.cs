using SS14.Server.Interfaces.GameObjects;
using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.Serialization;
using SS14.Shared.Maths;
using SS14.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace SS14.Server.GameObjects
{
    public class SpriteComponent : Component, ISpriteRenderableComponent
    {
        public override string Name => "Sprite";
        public override uint? NetID => NetIDs.SPRITE;

        private bool _visible;
        private DrawDepth _drawDepth;
        private Vector2 _scale;
        private Vector2 _offset;
        private Color _color;
        private bool _directional;
        private string _baseRSIPath;

        public DrawDepth DrawDepth
        {
            get => _drawDepth;
            set => _drawDepth = value;
        }

        public bool Visible
        {
            get => _visible;
            set => _visible = value;
        }

        public Vector2 Scale { get => _scale; set => _scale = value; }

        public Angle Rotation { get; set; }

        public Vector2 Offset { get => _offset; set => _offset = value; }

        public Color Color { get => _color; set => _color = value; }

        public bool Directional { get => _directional; set => _directional = value; }

        public string BaseRSIPath { get => _baseRSIPath; set => _baseRSIPath = value; }

        public override void ExposeData(EntitySerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _visible, "visible", true);
            serializer.DataField(ref _drawDepth, "depth", DrawDepth.FloorTiles);
            serializer.DataField(ref _offset, "offset", Vector2.Zero);
            serializer.DataField(ref _scale, "scale", Vector2.One);
            serializer.DataField(ref _color, "color", Color.White);
            serializer.DataField(ref _directional, "directional", true);
            serializer.DataField(ref _baseRSIPath, "sprite", null);
        }

        public override void LoadParameters(YamlMappingNode mapping)
        {
            base.LoadParameters(mapping);

            if (mapping.TryGetNode("rotation", out var node))
            {
                Rotation = Angle.FromDegrees(node.AsFloat());
            }
        }

        public override ComponentState GetComponentState()
        {
            return new SpriteComponentState(Visible, DrawDepth, Scale, Rotation, Offset, Color, Directional, BaseRSIPath, null);
        }
    }
}
