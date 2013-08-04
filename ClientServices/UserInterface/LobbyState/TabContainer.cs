using System.Drawing;
using ClientInterfaces.Resource;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;

namespace ClientServices.UserInterface.Components
{
    internal class TabContainer : ScrollableContainer
    {
        public string tabName = "";
        public Sprite tabSprite = null;

        public TabContainer(string uniqueName, Size size, IResourceManager resourceManager)
            : base(uniqueName, size, resourceManager)
        {
            DrawBorder = false;
        }

        public string tabSpriteName
        {
            get { return tabSprite != null ? tabSprite.Name : ""; }
            set { tabSprite = _resourceManager.GetSprite(value); }
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
        }

        public override void Render()
        {
            base.Render();
        }

        public override void Dispose()
        {
            base.Dispose();
        }

        public override bool MouseDown(MouseInputEventArgs e)
        {
            return base.MouseDown(e);
        }

        public override bool MouseUp(MouseInputEventArgs e)
        {
            return base.MouseUp(e);
        }

        public override void MouseMove(MouseInputEventArgs e)
        {
            base.MouseMove(e);
        }
    }
}