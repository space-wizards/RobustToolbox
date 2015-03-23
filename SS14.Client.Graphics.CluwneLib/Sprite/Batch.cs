using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SS14.Client.Graphics.CluwneLib.Sprite
{
    public class Batch
    {
        private string p1;
        private int p2;

        public Batch(string p1, int p2)
        {
            // TODO: Complete member initialization
            this.p1 = p1;
            this.p2 = p2;
        }
        public int Count { get; set; }

        public void Draw()
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }
        public void AddClone (CluwneSprite CSprite)
        {
            throw new NotImplementedException();
        }


        public AlphaBlendOperation SourceBlend { get; set; }

        public AlphaBlendOperation DestinationBlend { get; set; }

        public AlphaBlendOperation SourceBlendAlpha { get; set; }

        public AlphaBlendOperation DestinationBlendAlpha { get; set; }
    }
}
