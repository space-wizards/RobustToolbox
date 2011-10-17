using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GorgonLibrary;
using GorgonLibrary.Graphics;

namespace ClientInterfaces
{
    public interface ILightManager
    {
        void ApplyLightsToSprite(List<ILight> lights, Sprite sprite, Vector2D screenOffset);
    }
}
