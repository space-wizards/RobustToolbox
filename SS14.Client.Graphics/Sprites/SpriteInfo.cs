using SS14.Shared.Maths;

namespace SS14.Client.Graphics.Sprites
{
    public static class Limits
    {
        public const byte ClickthroughLimit = 64; //default alpha for limiting clickthrough on sprites; will probably be template-dependent later on
    }

    public struct SpriteInfo
    {
        private string name;
        private Vector2 offsets;
        private Vector2 size;

        public Vector2 Offsets { get => offsets; set => offsets = value; }
        public Vector2 Size { get => size; set => size = value; }
        public string Name { get => name; set => name = value; }
    }
}
