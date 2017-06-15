using System;
using System.Collections.Generic;

namespace SS14.Client.Graphics
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
