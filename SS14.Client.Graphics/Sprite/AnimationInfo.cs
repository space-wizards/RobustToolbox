using System;

namespace SS14.Client.Graphics.Sprite
{
    [Serializable]
    public class AnimationInfo
    {
        public string Name { get; set; }
        public int Frames { get; set; }
        public int FPS { get; set; }
    }
}
