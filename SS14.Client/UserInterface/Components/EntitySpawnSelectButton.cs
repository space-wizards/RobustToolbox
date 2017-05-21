using SFML.Graphics;
using SFML.System;
using SFML.Window;
using SS14.Client.Graphics;
using SS14.Client.Graphics.Sprite;
using SS14.Client.Interfaces.Resource;
using SS14.Shared.GameObjects;
using SS14.Shared.Maths;
using System.Linq;

namespace SS14.Client.UserInterface.Components
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

        private new readonly TextSprite name;
        private readonly Sprite objectSprite;

        public int fixed_width = -1;
        public bool selected = false;

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

        public override bool MouseDown(MouseButtonEventArgs e)
        {
            if (ClientArea.Contains(e.X, e.Y))
            {
                if (Clicked != null) Clicked(this, associatedTemplate, associatedTemplateName);
                return true;
            }
            return false;
        }

        public override bool MouseUp(MouseButtonEventArgs e)
        {
            return false;
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            objectSprite.Position = new Vector2f(Position.X + 5, Position.Y + 5);
            var bounds = objectSprite.GetLocalBounds();
            name.Position = new Vector2f(objectSprite.Position.X + bounds.Width + 5, objectSprite.Position.Y).Round();
            ClientArea = new IntRect(Position,
                                       new Vector2i(
                                           fixed_width != -1
                                               ? fixed_width
                                               : ((int)bounds.Width + (int) name.Width + 15),
                                           ((int)bounds.Height > (int) name.Height
                                                ? (int)bounds.Height
                                                : ((int) name.Height + 5)) + 10));
        }

        public override void Render()
        {
           CluwneLib.drawRectangle(ClientArea.Left, ClientArea.Top, ClientArea.Width, ClientArea.Height,
                                                       selected ? new SFML.Graphics.Color(34, 139, 34) : new SFML.Graphics.Color(255, 250, 240));

            objectSprite.Draw();
            name.Draw();
        }
    }
}
