using GorgonLibrary.Graphics;
using SS14.Client.Interfaces.Resource;
using SS14.Shared.GameObjects;
using SS14.Shared.GO;
using System.Drawing;

namespace SS14.Client.Services.UserInterface.Components
{
    internal sealed class ExamineWindow : Window
    {
        private readonly Label _entityDescription;
        private readonly IResourceManager _resourceManager;
        private Sprite _entitySprite;

        public ExamineWindow(Size size, Entity entity, IResourceManager resourceManager)
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
                _entityDescription.Position = new Point(10,
                                                        (int) _entitySprite.Height +
                                                        _entityDescription.ClientArea.Height + 10);
            }
            else
                _entityDescription.Position = new Point(10, 10);
        }

        public override void Render()
        {
            base.Render();
            if (_entitySprite == null) return;

            var spriteRect = new Rectangle((int) (ClientArea.Width/2f - _entitySprite.Width/2f) + ClientArea.X,
                                           10 + ClientArea.Y, (int) _entitySprite.Width, (int) _entitySprite.Height);
            _entitySprite.Draw(spriteRect);
        }

        public override void Dispose()
        {
            _entitySprite = null;
            base.Dispose();
        }
    }
}