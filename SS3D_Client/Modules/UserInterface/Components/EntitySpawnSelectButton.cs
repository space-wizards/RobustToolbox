using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;
using ClientResourceManager;

using SS13.HelperClasses;

using Lidgren.Network;
using CGO;
using SS13_Shared;

namespace SS13.UserInterface
{
    public class EntitySpawnSelectButton : GuiComponent
    {
        public override Point Position
        {
            get
            {
                return base.Position;
            }
            set
            {
                base.Position = value;
            }
        }
        private TextSprite name;
        private Sprite objectSprite;
        private GorgonLibrary.Graphics.Font font;

        public Boolean selected = false;

        public int fixed_width = -1;

        public delegate void EntitySpawnSelectPress(EntitySpawnSelectButton sender, EntityTemplate template, string templateName);
        public event EntitySpawnSelectPress Clicked;

        EntityTemplate associatedTemplate;
        string associatedTemplateName;

        public EntitySpawnSelectButton(EntityTemplate entityTemplate, string templateName)
            : base()
        {
            string SpriteName = (string)entityTemplate.GetBaseSpriteParamaters().FirstOrDefault().Parameter;
            string ObjectName = entityTemplate.Name;

            associatedTemplate = entityTemplate;
            associatedTemplateName = templateName;

            objectSprite = ResMgr.Singleton.GetSprite(SpriteName);

            font = ResMgr.Singleton.GetFont("CALIBRI");
            name = new TextSprite("Label" + SpriteName, "Name", font);
            name.Color = System.Drawing.Color.Black;
            name.Text = ObjectName;
        }

        public override bool MouseDown(MouseInputEventArgs e)
        {
            if (clientArea.Contains(new Point((int)e.Position.X, (int)e.Position.Y)))
            {
                if (Clicked != null) Clicked(this, associatedTemplate, associatedTemplateName);
                return true;
            }
            return false;
        }

        public override bool MouseUp(MouseInputEventArgs e)
        {
            return false;
        }

        public override void Update()
        {
            base.Update();
            objectSprite.Position = new Vector2D(Position.X + 5, Position.Y + 5);
            name.Position = new Vector2D(objectSprite.Position.X + objectSprite.Width + 5, objectSprite.Position.Y);
            ClientArea = new Rectangle(Position, new Size(fixed_width != -1 ? fixed_width : ((int)objectSprite.Width + (int)name.Width + 15), ((int)objectSprite.Height > (int)name.Height ? (int)objectSprite.Height : ((int)name.Height + 5)) + 10));
        }

        public override void Render()
        {
            Gorgon.Screen.FilledRectangle(ClientArea.X, ClientArea.Y, ClientArea.Width, ClientArea.Height, selected ? Color.ForestGreen : Color.FloralWhite);
            Gorgon.Screen.Rectangle(ClientArea.X, ClientArea.Y, ClientArea.Width, ClientArea.Height, Color.Black);
            objectSprite.Draw();
            name.Draw();
        }

    }
}
