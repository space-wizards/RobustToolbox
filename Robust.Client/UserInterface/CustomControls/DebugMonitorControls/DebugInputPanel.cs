using System.Text;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Client.UserInterface.CustomControls.DebugMonitorControls
{
    internal sealed class DebugInputPanel : PanelContainer
    {
        [Dependency] private readonly IInputManager _inputManager = default!;

        private readonly Label _label;

        private readonly StringBuilder _textBuilder = new();
        private readonly char[] _textBuffer = new char[512];


        public DebugInputPanel()
        {
            IoCManager.InjectDependencies(this);

            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = new Color(67, 105, 255, 138),
            };

            PanelOverride.SetContentMarginOverride(StyleBox.Margin.All, 5);

            AddChild(_label = new Label());
        }

        protected override void FrameUpdate(FrameEventArgs args)
        {
            base.FrameUpdate(args);

            if (!VisibleInTree)
            {
                return;
            }

            _textBuilder.Clear();

            _textBuilder.Append($"Input context: {_inputManager.Contexts.ActiveContext.Name}");
            foreach (var func in _inputManager.DownKeyFunctions)
            {
                _textBuilder.Append($"\n  {func.FunctionName}");
            }

            _label.TextMemory = FormatHelpers.BuilderToMemory(_textBuilder, _textBuffer);
        }
    }
}
