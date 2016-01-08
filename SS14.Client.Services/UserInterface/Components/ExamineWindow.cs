using SS14.Client.Interfaces.Resource;
using SS14.Shared.GameObjects;
using SS14.Shared.GO;
using System.Drawing;
using SS14.Client.Graphics.Sprite;
using SFML.Graphics;
using SS14.Client.Graphics;
using SFML.System;

namespace SS14.Client.Services.UserInterface.Components
{
    internal sealed class ExamineWindow : Window
    {
        private readonly Label _entityDescription;
        private readonly IResourceManager _resourceManager;
        private Sprite _entitySprite;

        public ExamineWindow(Vector2i size, Entity entity, IResourceManager resourceManager)
            : base(entity.Name, size, resourceManager)
        {
            _resourceManager = resourceManager;
            _entityDescription = new Label(entity.GetDescriptionString(), "CALIBRI", _resourceManager);

            components.Add(_entityDescription);

            ComponentReplyMessage reply = entity.SendMessage(entity, ComponentFamily.Renderable,
                                                             ComponentMessageType.GetSprite);

            SetVisible(true);

            if (reply.MessageType == ComponentMessageType.CurrentSprite)
            {
                _entitySprite = (Sprite) reply.ParamsList[0];
                _entityDescription.Position = new Vector2i(10,
                                                        (int)_entitySprite.GetLocalBounds().Height +
                                                        _entityDescription.ClientArea.Height + 10);
            }
            else
                _entityDescription.Position = new Vector2i(10, 10);
        }

        public override void Render()
        {
            base.Render();
            if (_entitySprite == null) return;

            var bounds = _entitySprite.GetLocalBounds();
            var spriteRect = new IntRect((int) (ClientArea.Width/2f - bounds.Width/2f) + ClientArea.Left,
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