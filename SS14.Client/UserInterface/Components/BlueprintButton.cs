using SFML.Graphics;
using SFML.System;
using SFML.Window;
using SS14.Client.Graphics;
using SS14.Client.Graphics.Sprite;
using SS14.Client.Interfaces.Resource;
using System;

namespace SS14.Client.UserInterface.Components
{
    internal class BlueprintButton : GuiComponent
    {
        #region Delegates

        public delegate void BlueprintButtonPressHandler(BlueprintButton sender);

        #endregion

        private readonly IResourceManager _resourceManager;

        public string Compo1;
        public string Compo1Name;

        public string Compo2;
        public string Compo2Name;
        public TextSprite Label;

        public string Result;
        public string ResultName;

        private SFML.Graphics.Color _bgcol = Color.Transparent;
        private Sprite _icon;

        public BlueprintButton(string c1, string c1N, string c2, string c2N, string res, string resname,
                               IResourceManager resourceManager)
        {
            _resourceManager = resourceManager;

            Compo1 = c1;
            Compo1Name = c1N;

            Compo2 = c2;
            Compo2Name = c2N;

            Result = res;
            ResultName = resname;

            _icon = _resourceManager.GetSprite("blueprint");

            Label = new TextSprite("blueprinttext", "", _resourceManager.GetFont("CALIBRI"))
                        {
                            Color = new SFML.Graphics.Color(248, 248, 255),
                            ShadowColor = new SFML.Graphics.Color(105, 105, 105),
                            ShadowOffset = new Vector2f(1, 1),
                            Shadowed = true
                        };

            Update(0);
        }

        public event BlueprintButtonPressHandler Clicked;

        public override void Update(float frameTime)
        {
            var bounds = _icon.GetLocalBounds();
            ClientArea = new IntRect(Position,
                                       new Vector2i((int) (Label.Width + bounds.Width),
                                                (int) Math.Max(Label.Height, bounds.Height)));
            Label.Position = new Vector2i(Position.X + (int)bounds.Width, Position.Y);
            _icon.Position = new Vector2f(Position.X, Position.Y + (Label.Height / 2f - bounds.Height / 2f));
            Label.Text = Compo1Name + " + " + Compo2Name + " = " + ResultName;
        }

        public override void Render()
        {
            if (_bgcol != Color.Transparent)
            CluwneLib.drawRectangle(ClientArea.Left, ClientArea.Top, ClientArea.Width,
                                                           ClientArea.Height, _bgcol);
            _icon.Draw();
            Label.Draw();
        }

        public override void Dispose()
        {
            Label = null;
            _icon = null;
            Clicked = null;
            base.Dispose();
            GC.SuppressFinalize(this);
        }

        public override bool MouseDown(MouseButtonEventArgs e)
        {
            if (ClientArea.Contains(e.X, e.Y))
            {
                if (Clicked != null) Clicked(this);
                return true;
            }
            return false;
        }

        public override bool MouseUp(MouseButtonEventArgs e)
        {
            return false;
        }

        public override void MouseMove(MouseMoveEventArgs e)
        {
            _bgcol = ClientArea.Contains(e.X, e.Y)
                         ? new SFML.Graphics.Color(70, 130, 180)
                         : Color.Transparent;
        }
    }
}