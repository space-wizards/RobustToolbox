using SS14.Client.Graphics.Sprites;
using SS14.Client.Interfaces.GameObjects;
using SS14.Client.Interfaces.Resource;
using SS14.Client.UserInterface.Controls;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Maths;
using Vector2i = SS14.Shared.Maths.Vector2i;

namespace SS14.Client.UserInterface.Components
{
    internal sealed class ExamineWindow : Window
    {
        private readonly Label _entityDescription;
        private Sprite _entitySprite;

        public ExamineWindow(Vector2i size, IEntity entity, IResourceCache resourceCache)
            : base(entity.Name, size)
        {
            _entityDescription = new Label(entity.GetDescriptionString(), "CALIBRI");

            Components.Add(_entityDescription);

            SetVisible(true);

            if (entity.TryGetComponent<ISpriteRenderableComponent>(out var component))
            {
                _entitySprite = component.GetCurrentSprite();
                _entityDescription.Position = new Vector2i(10,
                                        (int)_entitySprite.LocalBounds.Height +
                                        _entityDescription.ClientArea.Height + 10);
            }
            else
            {
                _entityDescription.Position = new Vector2i(10, 10);
            }
        }

        public override void Draw()
        {
            base.Draw();
            if (_entitySprite == null) return;

            var bounds = _entitySprite.LocalBounds;
            var spriteRect = Box2i.FromDimensions((int)(ClientArea.Width / 2f - bounds.Width / 2f) + ClientArea.Left,
                                           10 + ClientArea.Top, (int)bounds.Width, (int)bounds.Height);
            _entitySprite.SetTransformToRect(spriteRect);
            _entitySprite.Draw();
        }

        public override void Dispose()
        {
            _entitySprite = null;
            base.Dispose();
        }
    }
}
