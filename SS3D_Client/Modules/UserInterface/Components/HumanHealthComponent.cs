using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Reflection;
using SS3D.HelperClasses;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;
using SS3D.Modules;
using Lidgren.Network;
using CGO;
using SS3D_shared.GO;
using SS3D_shared;
using ClientResourceManager;


namespace SS3D.UserInterface
{
    public class HumanHealthComponent : GuiComponent
    {
        public override Point Position
        {
            get
            {
                return position;
            }
            set
            {
                position = value;
                greenSprite.SetPosition(position.X, position.Y);
                yellowSprite.SetPosition(position.X, position.Y);
                redSprite.SetPosition(position.X, position.Y);
                healthAmount.SetPosition(position.X + 7, position.Y + 16);
            }
        }
        private Sprite baseSprite;
        private Sprite greenSprite;
        private Sprite yellowSprite;
        private Sprite redSprite;
        private TextSprite healthAmount;
        private GorgonLibrary.Graphics.Font healthDisplayFont;


        public HumanHealthComponent(PlayerController _playerController)
            :base(_playerController)
        {
            componentClass = GuiComponentType.HealthComponent;
        
            greenSprite = ResMgr.Singleton.GetSpriteFromImage("healthgreen");
            yellowSprite = ResMgr.Singleton.GetSpriteFromImage("healthyellow");
            redSprite = ResMgr.Singleton.GetSpriteFromImage("healthred");
            baseSprite = greenSprite;

            healthDisplayFont = new GorgonLibrary.Graphics.Font("Arial8pt", "Arial", 8.0f, true, true);
            healthAmount = new TextSprite("healthAmount", "100%", healthDisplayFont);
            healthAmount.Color = System.Drawing.Color.Black;

            Position = new Point(Gorgon.Screen.Width - 42, Gorgon.Screen.Height - 99);
        }

        public override void Render()
        {
            baseSprite.Draw();
            healthAmount.Draw();
        }

        public override void Update()
        {
            var entity = (Entity)playerController.controlledAtom;
            HealthComponent comp = (HealthComponent)entity.GetComponent(ComponentFamily.Damageable);
            healthAmount.Text = comp.GetHealth().ToString();
        }

        public override bool MouseDown(MouseInputEventArgs e)
        {
            System.Drawing.RectangleF mouseAABB = new System.Drawing.RectangleF(e.Position.X, e.Position.Y, 1, 1);
            if (baseSprite.AABB.IntersectsWith(mouseAABB))
            {
                if (baseSprite == greenSprite)
                    baseSprite = yellowSprite;
                else if (baseSprite == yellowSprite)
                    baseSprite = redSprite;
                else if (baseSprite == redSprite)
                    baseSprite = greenSprite;
                return true;
            }
            return false;
        }
    }
}
