using Robust.Client.UserInterface.Controls;

namespace Robust.Client.UserInterface.CustomControls
{
    public sealed class Tooltip : PanelContainer
    {
        private readonly Label _label;

        public string? Text
        {
            get => _label.Text;
            set => _label.Text = value;
        }

        public Tooltip()
        {
            AddChild(_label = new Label());
        }
    }
}
