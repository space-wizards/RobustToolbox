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

        private Type associatedType;

        private const int borderSize = 2;
        private const int boxSize = 64; // 64x64

        private GorgonLibrary.Graphics.Font font;

        public EditorAtomButton(Type objectType)
            : base()
        {
            string SpriteName = UiManager.Singleton.GetObjectSpriteName(objectType);
            string ObjectName = UiManager.Singleton.GetAtomName(objectType);
            associatedType = objectType;

            objectSprite = ResMgr.Singleton.GetSprite(SpriteName);
            Position = new Point(0, 0);
            font = ResMgr.Singleton.GetFont("CALIBRI");
            name = new TextSprite("Label" + SpriteName, "Name", font);
            name.Color = System.Drawing.Color.Black;
            name.ShadowColor = System.Drawing.Color.DarkGray;
            name.Shadowed = true;
            name.ShadowOffset = new Vector2D(1, 1);

            name.Text = ObjectName;

            for (int i = 0; i < SpriteName.Length; i++)
            {
                if (i > 0 && i % 9 == 0)
                    name.Text += "\n" + SpriteName.Substring(i, 1);
                else
                    name.Text += SpriteName.Substring(i, 1);
            }
            renderImage = new RenderImage("RI" + SpriteName, boxSize, boxSize, ImageBufferFormats.BufferUnknown);
            renderImage.ClearEachFrame = ClearTargets.None;
            PreRender();
        }

        private void PreRender()
        {
            Point renderPos = new Point(0, 0);

            renderImage.BeginDrawing();
            renderImage.FilledRectangle(0, 0, boxSize, boxSize, System.Drawing.Color.GhostWhite);
            renderImage.FilledRectangle(borderSize, borderSize, boxSize - (2 * borderSize), boxSize - (2 * borderSize), System.Drawing.Color.DimGray);

            Vector2D prevAxis = objectSprite.Axis;
            Vector2D prevScale = objectSprite.Scale;

            objectSprite.Smoothing = Smoothing.Smooth;
            if (objectSprite.Width >= boxSize || objectSprite.Height >= boxSize) objectSprite.SetScale(0.4f, 0.4f);
            objectSprite.SetAxis(objectSprite.Width / 2, objectSprite.Height / 2);
            objectSprite.Position = new Vector2D((boxSize / 2f), (boxSize / 2f));

            objectSprite.Draw();

            objectSprite.Axis = prevAxis;
            objectSprite.Scale = prevScale;
            objectSprite.Smoothing = Smoothing.None;

            name.Position = new Vector2D(position.X + (boxSize / 2f) - (name.Size.X / 2f), position.Y + boxSize - name.Size.Y - borderSize);
            name.Draw();

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
        }

    }
}
