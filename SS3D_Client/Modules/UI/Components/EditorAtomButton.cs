using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;

using Lidgren.Network;

using SS3D_shared;

namespace SS3D.Modules.UI.Components
{
    public class EditorAtomButton : GuiComponent
    {
        public override Point Position
        {
            get
            {
                return base.Position;
            }
            set
            {
                base.Position = value;
            }
        }

        private RenderImage renderImage;

        private TextSprite name;
        private Sprite objectSprite;

        private GorgonLibrary.Graphics.Font font;

        public EditorAtomButton(PlayerController _playerController, string SpriteName)
            : base(_playerController)
        {
            objectSprite = ResMgr.Singleton.GetSprite(SpriteName);

            Position = new Point(0, 0);

            font = ResMgr.Singleton.GetFont("CALIBRI");
            name = new TextSprite("Label" + SpriteName, "Name", font);
            //name.Position 
            name.Color = System.Drawing.Color.Green;

            renderImage = new RenderImage("RI" + SpriteName, 64, 64, ImageBufferFormats.BufferUnknown);
            renderImage.ClearEachFrame = ClearTargets.None;
            PreRender();
        }

        private void PreRender()
        {
            Point renderPos = new Point(0, 0);

            renderImage.BeginDrawing();
            renderImage.Rectangle(0, 0, 64, 64, System.Drawing.Color.Black);
            renderImage.Rectangle(2, 2, 60, 60, System.Drawing.Color.GhostWhite);
            objectSprite.Position = new Vector2D(0 + 32, 0 + 32);
            renderImage.EndDrawing();
        }


        public override void HandleNetworkMessage(NetIncomingMessage message)
        {
        }

        public override bool MouseDown(MouseInputEventArgs e)
        {
            return false;
        }

        public override bool MouseUp(MouseInputEventArgs e)
        {
            return false;
        }

        public override void Render()
        {
            renderImage.Blit(Position.X, Position.Y);
            name.Text = ConfigManager.Singleton.Configuration.PlayerName;
            name.Draw();
        }

    }
}
