using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SS13_Shared
{
    [Serializable]
    public class AnimationInfo
    {
        public string Name { get; set; }
        public string Track { get; set; }
        public int Frames { get; set; }
        public int FPS { get; set; }
    }
}
