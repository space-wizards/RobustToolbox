using OpenTK.Graphics;
using SS14.Client.Graphics;
using SS14.Client.Graphics.Sprites;
using SS14.Client.Interfaces.Resource;
using SS14.Shared.GameObjects;
using SS14.Shared.Maths;
using System.Linq;
using SS14.Client.ResourceManagement;
using Vector2i = SS14.Shared.Maths.Vector2i;
using SS14.Client.Graphics.Input;
using SS14.Client.UserInterface.Controls;

namespace SS14.Client.UserInterface.Components
{
    public class EntitySpawnSelectButton : Control
    {
        #region Delegates

        public delegate void EntitySpawnSelectPress(
            EntitySpawnSelectButton sender, EntityPrototype template, string templateName);

        #endregion Delegates

        private readonly IResourceCache _resourceCache;
        private readonly EntityPrototype associatedTemplate;
        private readonly string associatedTemplateName;
        private readonly Font font;

        private new readonly TextSprite name;
        private readonly Sprite objectSprite;

        public int fixed_width = -1;
        public bool selected = false;

        public EntitySpawnSelectButton(EntityPrototype entityTemplate, string templateName,
                                       IResourceCache resourceCache)
        {
            _resourceCache = resourceCache;

            var spriteNameParam = entityTemplate.GetBaseSpriteParamaters().FirstOrDefault();
            string SpriteName = "";
            if (spriteNameParam != null)
            {
                SpriteName = spriteNameParam.GetValue<string>();
            }
            string ObjectName = entityTemplate.Name;

            associatedTemplate = entityTemplate;
            associatedTemplateName = templateName;

            objectSprite = _resourceCache.GetSprite(SpriteName);

            font = _resourceCache.GetResource<FontResource>(@"Fonts/CALIBRI.TTF").Font;
            name = new TextSprite("Name", font)
            {
                FillColor = Color.Black,
                Text = ObjectName
            };
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

        protected override void OnCalcRect()
        {
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            objectSprite.Position = Position + 5;
            var bounds = objectSprite.LocalBounds;
            name.Position = new Vector2i((int)(objectSprite.Position.X + bounds.Width + 5), (int)objectSprite.Position.Y);
            ClientArea = Box2i.FromDimensions(Position,
                                       new Vector2i(
                                           fixed_width != -1
                                               ? fixed_width
                                               : ((int)bounds.Width + (int)name.Width + 15),
                                           ((int)bounds.Height > (int)name.Height
                                                ? (int)bounds.Height
                                                : ((int)name.Height + 5)) + 10));
        }

        public override void Draw()
        {
            CluwneLib.drawRectangle(ClientArea.Left, ClientArea.Top, ClientArea.Width, ClientArea.Height,
                                                        selected ? new Color4(34, 139, 34, 255) : new Color4(255, 250, 240, 255));

            objectSprite.Draw();
            name.Draw();
        }
    }
}
