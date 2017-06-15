using SS14.Client.Graphics.Sprite;
using System;
using System.Collections.Generic;

namespace SS14.Client.Graphics.Collection
{
    [Serializable]
    public class AnimationCollection
    {
        public string Name { get; set; }
        public List<AnimationInfo> Animations { get; set; } 

        public AnimationCollection()
        {
            Animations = new List<AnimationInfo>();
        }
    }
}
