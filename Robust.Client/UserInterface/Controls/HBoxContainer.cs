namespace Robust.Client.UserInterface.Controls
{
    /// <summary>
    ///     Container that lays its children out horizontally: from left to right.
    /// </summary>
    public class HBoxContainer : BoxContainer
    {
        public HBoxContainer() : base()
        {
        }

        public HBoxContainer(string name) : base(name)
        {
        }

        private protected override bool Vertical => false;
    }
}
