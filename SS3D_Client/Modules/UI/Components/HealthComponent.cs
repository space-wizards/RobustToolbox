using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using GorgonLibrary;
using GorgonLibrary.Graphics;

using SS3D_shared;

namespace SS3D.Modules.UI.Components
{
    public class HumanHealthComponent : IGuiComponent
    {
        public GuiComponent componentClass
        {
            get;
            set;
        }


        private Point position;
        public Point Position
        {
            get
            {
                return position;
            }
            private set
            {
                position = value;
                baseSprite.SetPosition(position.X, position.Y);
            }
        }
        public Sprite baseSprite;

        public HumanHealthComponent()
        {
            componentClass = GuiComponent.HealthComponent;
        
            baseSprite = ResMgr.Singleton.GetSpriteFromImage("healthPanel");
            baseSprite.SetPosition(position.X, position.Y);
        }

        public void Render()
        {
            baseSprite.Draw();
        }
    }
}
