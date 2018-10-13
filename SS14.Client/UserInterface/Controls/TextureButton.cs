namespace SS14.Client.UserInterface.Controls
{
    #if GODOT
    [ControlWrap(typeof(Godot.TextureButton))]
    #endif
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
