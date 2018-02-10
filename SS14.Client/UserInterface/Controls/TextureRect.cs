using SS14.Client.Graphics;

namespace SS14.Client.UserInterface.Controls
{
    public class TextureRect : Control
    {
        public TextureRect() : base()
        {
        }
        public TextureRect(string name) : base(name)
        {
        }
        public TextureRect(Godot.TextureRect button) : base(button)
        {
        }

        public TextureSource Texture
        {
            // TODO: Maybe store the texture passed in in case it's like a TextureResource or whatever.
            get => new GodotTextureSource(SceneControl.Texture);
            set => SceneControl.Texture = value.Texture;
        }

        new private Godot.TextureRect SceneControl;

        protected override Godot.Control SpawnSceneControl()
        {
            return new Godot.TextureRect();
        }

        protected override void SetSceneControl(Godot.Control control)
        {
            base.SetSceneControl(control);
            SceneControl = (Godot.TextureRect)control;
        }
    }
}
