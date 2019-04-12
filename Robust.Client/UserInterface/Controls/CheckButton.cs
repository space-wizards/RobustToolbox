namespace Robust.Client.UserInterface.Controls
{
    [ControlWrap(typeof(Godot.CheckButton))]
    public class CheckButton : Button
    {
        public CheckButton()
        {
        }

        public CheckButton(Godot.CheckButton button) : base(button)
        {
        }
    }
}
