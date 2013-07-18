using System;
using System.Drawing;
using ClientInterfaces;
using ClientInterfaces.Resource;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;

namespace ClientServices.UserInterface.Components
{
    class SimpleImage : GuiComponent
    {
        private readonly IResourceManager _resourceManager; //TODO Make simpleimagebutton and other ui classes use this.

        Sprite drawingSprite;

        public SimpleImage(IResourceManager resourceManager, string spriteName)
        {
            _resourceManager = resourceManager;
            drawingSprite = _resourceManager.GetSprite(spriteName);
            Update(0);
        }

        public override void Update(float frameTime)
        {
            ClientArea = new Rectangle(this.Position, new Size((int)drawingSprite.Width, (int)drawingSprite.Height));
        }

        public override void Render()
        {
            drawingSprite.Draw(ClientArea);
        }

        public override void Dispose()
        {
            drawingSprite = null;
            base.Dispose();
            GC.SuppressFinalize(this);
        }

        public override bool MouseDown(MouseInputEventArgs e)
        {
            return false;
        }

        public override bool MouseUp(MouseInputEventArgs e)
        {
            return false;
        }
    }
}
