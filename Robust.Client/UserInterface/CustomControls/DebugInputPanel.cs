using Robust.Client.Graphics.Drawing;
using Robust.Client.Interfaces.Input;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Robust.Client.UserInterface.CustomControls
{
    internal class DebugInputPanel : PanelContainer
    {
#pragma warning disable 649
        [Dependency] private readonly IInputManager _inputManager;
#pragma warning restore 649

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

            _label.Text = string.Join("\n", _inputManager.DownKeyFunctions);
        }
    }
}
