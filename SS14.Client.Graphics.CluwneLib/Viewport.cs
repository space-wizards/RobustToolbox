using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SS14.Client.Graphics.CluwneLib
{
    public class Viewport
    {
        private int p1;
        private int p2;
        private uint p3;
        private uint p4;

        public Viewport(int p1, int p2, uint p3, uint p4)
        {
            // TODO: Complete member initialization
            this.p1 = p1;
            this.p2 = p2;
            this.p3 = p3;
            this.p4 = p4;
        }
        public int Width { get; set; }
        public int Height { get; set; }

    }
}
