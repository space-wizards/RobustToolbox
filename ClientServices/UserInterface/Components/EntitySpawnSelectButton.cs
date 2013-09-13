using System;
using System.Drawing;
using System.Linq;
using ClientInterfaces.Resource;
using GameObject;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;
using Font = GorgonLibrary.Graphics.Font;

namespace ClientServices.UserInterface.Components
{
    public class EntitySpawnSelectButton : GuiComponent
    {
        #region Delegates

        public delegate void EntitySpawnSelectPress(
            EntitySpawnSelectButton sender, EntityTemplate template, string templateName);

        #endregion

        private readonly IResourceManager _resourceManager;
        private readonly EntityTemplate associatedTemplate;
        private readonly string associatedTemplateName;
        private readonly Font font;

        private readonly TextSprite name;
        private readonly Sprite objectSprite;

        public int fixed_width = -1;
        public Boolean selected = false;

        public EntitySpawnSelectButton(EntityTemplate entityTemplate, string templateName,
                                       IResourceManager resourceManager)
        {
            _resourceManager = resourceManager;

            var spriteNameParam = entityTemplate.GetBaseSpriteParamaters().FirstOrDefault();
            string SpriteName = "";
            if (spriteNameParam != null)
            {
                SpriteName = spriteNameParam.GetValue<string>();
            }
            string ObjectName = entityTemplate.Name;

            associatedTemplate = entityTemplate;
            associatedTemplateName = templateName;

            objectSprite = _resourceManager.GetSprite(SpriteName);

            font = _resourceManager.GetFont("CALIBRI");
            name = new TextSprite("Label" + SpriteName, "Name", font);
            name.Color = Color.Black;
            name.Text = ObjectName;
        }

        public event EntitySpawnSelectPress Clicked;

        public override bool MouseDown(MouseInputEventArgs e)
        {
            if (ClientArea.Contains(new Point((int) e.Position.X, (int) e.Position.Y)))
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

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            objectSprite.Position = new Vector2D(Position.X + 5, Position.Y + 5);
            name.Position = new Vector2D(objectSprite.Position.X + objectSprite.Width + 5, objectSprite.Position.Y);
            ClientArea = new Rectangle(Position,
                                       new Size(
                                           fixed_width != -1
                                               ? fixed_width
                                               : ((int) objectSprite.Width + (int) name.Width + 15),
                                           ((int) objectSprite.Height > (int) name.Height
                                                ? (int) objectSprite.Height
                                                : ((int) name.Height + 5)) + 10));
        }

        public override void Render()
        {
            Gorgon.CurrentRenderTarget.FilledRectangle(ClientArea.X, ClientArea.Y, ClientArea.Width, ClientArea.Height,
                                                       selected ? Color.ForestGreen : Color.FloralWhite);
            Gorgon.CurrentRenderTarget.Rectangle(ClientArea.X, ClientArea.Y, ClientArea.Width, ClientArea.Height,
                                                 Color.Black);
            objectSprite.Draw();
            name.Draw();
        }
    }
}