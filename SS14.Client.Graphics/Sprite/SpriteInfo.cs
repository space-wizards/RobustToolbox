using SFML.System;

namespace SS14.Client.Graphics.Sprite
{
    public static class Limits
    {
        public const byte ClickthroughLimit = 64; //default alpha for limiting clickthrough on sprites; will probably be template-dependent later on
    }

    public struct SpriteInfo
    {
        public string Name;
        public Vector2f Offsets;
        public Vector2f Size;
    }
}
