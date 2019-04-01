using SS14.Client.UserInterface.Controls;

namespace SS14.Client.UserInterface.CustomControls
{
    public sealed class Tooltip : PanelContainer
    {
        private Label _label;

        public string Text
        {
            get => _label.Text;
            set => _label.Text = value;
        }

        protected override void Initialize()
        {
            base.Initialize();
            AddChild(_label = new Label());
        }
    }
}
