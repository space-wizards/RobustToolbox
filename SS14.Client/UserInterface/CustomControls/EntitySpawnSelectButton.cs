using System.Linq;
using SS14.Client.Graphics.Input;
using SS14.Client.Graphics.Sprites;
using SS14.Client.ResourceManagement;
using SS14.Client.UserInterface.Controls;
using SS14.Shared.GameObjects;
using SS14.Shared.Maths;

namespace SS14.Client.UserInterface.CustomControls
{
    public class EntitySpawnSelectButton : Control
    {
        public delegate void EntitySpawnSelectPress(EntitySpawnSelectButton sender, EntityPrototype template, string templateName);

        // icon size in px to resize
        private const float IconSize = 32;

        private readonly EntityPrototype _associatedTemplate;
        private readonly string _associatedTemplateName;

        private readonly TextSprite _name;
        private readonly Sprite _sprite;

        public int FixedWidth { get; set; } = -1;
        public bool Selected { get; set; }
        
        public EntitySpawnSelectButton(EntityPrototype entityTemplate, string templateName)
        {
            var spriteNameParam = entityTemplate.GetBaseSpriteParameters().FirstOrDefault();
            var spriteName = "";
            if (spriteNameParam != null)
                spriteName = spriteNameParam.GetValue<string>();
            var objectName = entityTemplate.Name;

            _associatedTemplate = entityTemplate;
            _associatedTemplateName = templateName;

            _sprite = new Sprite(_resourceCache.GetSprite(spriteName));

            Font font = _resourceCache.GetResource<FontResource>(@"Fonts/CALIBRI.TTF");
            _name = new TextSprite("Name", font);
            _name.FillColor = Color.Black;
            _name.Text = objectName;

            DrawBackground = true;
            DrawBorder = true;
        }

        public override bool MouseDown(MouseButtonEventArgs e)
        {
            if (base.MouseDown(e))
                return true;

            if (ClientArea.Translated(Position).Contains(e.X, e.Y))
            {
                Clicked?.Invoke(this, _associatedTemplate, _associatedTemplateName);
                return true;
            }
            return false;
        }

        protected override void OnCalcRect()
        {
            var rect = _sprite.TextureRect; // texture size in px

            var maxDim = rect.Height > rect.Width ? rect.Height : rect.Width;
            _sprite.Scale = new Vector2(IconSize / maxDim, IconSize / maxDim);

            ClientArea = Box2i.FromDimensions(new Vector2i(),
                new Vector2i(
                    FixedWidth != -1
                        ? FixedWidth
                        : (int) (5 + IconSize + 5 + _name.Width + 5),
                    ((int) IconSize > _name.Height
                        ? (int) IconSize
                        : _name.Height + 5) + 10));
        }

        protected override void OnCalcPosition()
        {
            base.OnCalcPosition();

            _sprite.Position = Position + 5;
            _name.Position = Position + new Vector2i((int) (IconSize + 10), ClientArea.Height / 2 - _name.Height / 2);
        }

        public override void Draw()
        {
            BackgroundColor = Selected ? new Color(34, 139, 34) : new Color(255, 250, 240);

            base.Draw();

            _sprite.Draw();
            _name.Draw();
        }

        public event EntitySpawnSelectPress Clicked;
    }
}
