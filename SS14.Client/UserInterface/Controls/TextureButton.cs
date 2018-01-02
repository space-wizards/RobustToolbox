namespace SS14.Client.UserInterface
{
    public class TextureButton : BaseButton
    {
        public TextureButton() : base()
        {
        }
        public TextureButton(string name) : base(name)
        {
        }
        public TextureButton(Godot.TextureButton button) : base(button)
        {
        }

        protected override Godot.Control SpawnSceneControl()
        {
            return new Godot.TextureButton();
        }
    }
}
