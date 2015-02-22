using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SFML.Graphics;
namespace SS14.Client.Graphics.CluwneLib.Sprite
{
    public static class BMExtensions
    {
        public static BlendMode toBlendMode(this BlendingMode B)
        {
            switch (B)
            {
                case BlendingMode.None:
                    return BlendMode.None;
                case BlendingMode.Alpha:
                    return BlendMode.Alpha;
                case BlendingMode.Add:
                    return BlendMode.Add;
                case BlendingMode.Multiply:
                    return BlendMode.Multiply;
            }
            return BlendMode.None;
        }
    }
    public enum BlendingMode
    {
        None,
        Add,
        Alpha,
        Multiply
    }
}
