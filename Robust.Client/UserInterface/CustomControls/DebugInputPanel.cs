using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Robust.Client.UserInterface.CustomControls
{
    internal class DebugInputPanel : PanelContainer
    {
        [Dependency] private readonly IInputManager _inputManager = default!;

        private readonly Label _label;

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

            var functionsText = string.Join("\n", _inputManager.DownKeyFunctions);
            _label.Text = $"Context: {_inputManager.Contexts.ActiveContext.Name}\n{functionsText}";
        }
    }
}
