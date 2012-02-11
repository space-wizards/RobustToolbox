using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using ClientInterfaces;
using ClientInterfaces.GOC;
using GorgonLibrary.Graphics;
using SS13_Shared;

namespace ClientServices.UserInterface.Components
{
    sealed class ExamineWindow : Window
    {
        private readonly IResourceManager _resourceManager;
        private Sprite _entitySprite;
        private readonly Label _entityDescription;

        public ExamineWindow(Size size, IEntity entity, IResourceManager resourceManager)
            : base(entity.Name, size, resourceManager)
        {
            _resourceManager = resourceManager;
            _entityDescription = new Label(entity.GetDescriptionString(), "CALIBRI", _resourceManager);

            components.Add(_entityDescription);

            var replies = new List<ComponentReplyMessage>();
            entity.SendMessage(entity, SS13_Shared.GO.ComponentMessageType.GetSprite, replies);

            SetVisible(true);

            if (replies.Any())
            {
                _entitySprite = (Sprite)replies.First(x => x.MessageType == SS13_Shared.GO.ComponentMessageType.CurrentSprite).ParamsList[0];
                _entityDescription.Position = new Point(10, (int)_entitySprite.Height + _entityDescription.ClientArea.Height + 10);
            }
            else
                _entityDescription.Position = new Point(10, 10);
        }

        public override void Render()
        {
            base.Render();
            if (_entitySprite == null) return;

            var spriteRect = new Rectangle((int)(ClientArea.Width / 2f - _entitySprite.Width / 2f) + ClientArea.X, 10 + ClientArea.Y, (int)_entitySprite.Width, (int)_entitySprite.Height);
            _entitySprite.Draw(spriteRect);
        }

        public override void Dispose()
        {
            _entitySprite = null;
            base.Dispose();
        }
    }
}
