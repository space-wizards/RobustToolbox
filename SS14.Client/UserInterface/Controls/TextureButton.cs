namespace SS14.Client.UserInterface.Controls
{
    [ControlWrap("TextureButton")]
    public class TextureButton : BaseButton
    {
        public TextureButton() : base()
        {
        }
        public TextureButton(string name) : base(name)
        {
        }
        #if GODOT
        internal TextureButton(Godot.TextureButton button) : base(button)
        {
        }

        private protected override Godot.Control SpawnSceneControl()
        {
            return new Godot.TextureButton();
        }
        #endif
    }
}
