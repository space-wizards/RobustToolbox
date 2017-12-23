using System.Collections.Generic;
using OpenTK.Graphics;
using SS14.Client.Console;
using SS14.Client.Graphics.Input;
using SS14.Client.Interfaces.Console;
using SS14.Client.UserInterface.Controls;
using SS14.Shared;
using SS14.Shared.Console;
using SS14.Shared.IoC;
using Vector2i = SS14.Shared.Maths.Vector2i;

namespace SS14.Client.UserInterface.CustomControls
{
    public class DebugConsole : ScrollableContainer, IDebugConsole
    {
        private readonly IClientConsole _console;
        private readonly Textbox _txtInput;
        private readonly ListPanel _historyList;

        private int _lastY;

        public override bool Visible
        {
            get => base.Visible;
            set
            {
                base.Visible = value;
                _console.SendServerCommandRequest();
            }
        }

        public DebugConsole(Vector2i size)
            : base(size)
        {
            _console = IoCManager.Resolve<IClientConsole>();
            _txtInput = new Textbox(size.X)
            {
                ClearFocusOnSubmit = false,
                BackgroundColor = new Color4(64, 64, 64, 100),
                ForegroundColor = new Color4(255, 250, 240, 255)
            };
            _txtInput.OnSubmit += TxtInputOnSubmit;

            _historyList = new ListPanel();
            Container.AddControl(_historyList);

            BackgroundColor = new Color4(64, 64, 64, 200);
            DrawBackground = true;
            DrawBorder = true;
            
            _console.AddString += (sender, args) => AddLine(args.Text, args.Channel, args.Color);
            _console.ClearText += (sender, args) => Clear();
        }

        public IReadOnlyDictionary<string, IConsoleCommand> Commands => _console.Commands;

        public void AddLine(string text, ChatChannel channel, Color4 color)
        {
            var atBottom = ScrollbarV.Value >= ScrollbarV.Max;
            var newLabel = new Label(text, "CALIBRI")
            {
                Position = new Vector2i(5, _lastY),
                ForegroundColor = color
            };
            
            _lastY = newLabel.ClientArea.Bottom;
            _historyList.AddControl(newLabel);
            _historyList.DoLayout();


            if (atBottom)
            {
                Update(0);
                ScrollbarV.Value = ScrollbarV.Max;
            }
        }

        public void Clear()
        {
            _historyList.DisposeAllChildren();
            _historyList.DoLayout();
            _lastY = 0;
            ScrollbarV.Value = 0;
        }

        public override void DoLayout()
        {
            base.DoLayout();

            _txtInput.LocalPosition = Position + new Vector2i(ClientArea.Left, ClientArea.Bottom);
            _txtInput.DoLayout();
        }
        
        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            _txtInput.Update(frameTime);
        }

        public override void Draw()
        {
            base.Draw();
            _txtInput.Draw();
        }

        public override void Dispose()
        {
            _txtInput.Dispose();
            _console.Dispose();
            base.Dispose();
        }

        public override bool MouseDown(MouseButtonEventArgs e)
        {
            if (!base.MouseDown(e))
                if (_txtInput.MouseDown(e))
                    return true;
            return false;
        }

        public override bool MouseUp(MouseButtonEventArgs e)
        {
            if (!base.MouseUp(e))
                return _txtInput.MouseUp(e);
            return false;
        }

        public override void MouseMove(MouseMoveEventArgs e)
        {
            base.MouseMove(e);
            _txtInput.MouseMove(e);
        }

        public override bool KeyDown(KeyEventArgs e)
        {
            if (!base.KeyDown(e))
                return _txtInput.KeyDown(e);
            return false;
        }

        public override bool TextEntered(TextEventArgs e)
        {
            if (!base.TextEntered(e))
                return _txtInput.TextEntered(e);
            return false;
        }

        private void TxtInputOnSubmit(Textbox sender, string text)
        {
            // debugConsole input is not prefixed with slash
            if(!string.IsNullOrWhiteSpace(text))
                _console.ProcessCommand(text);
        }
    }
}
