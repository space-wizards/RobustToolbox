using System;
using SS14.Client.Graphics;

namespace SS14.Client.UserInterface.Controls
{
    [ControlWrap(typeof(Godot.TextureRect))]
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

        public Texture Texture
        {
            // TODO: Maybe store the texture passed in in case it's like a TextureResource or whatever.
            get => GameController.OnGodot ? (Texture)new GodotTextureSource((Godot.Texture)SceneControl.Get("texture")) : Texture.Transparent;
            set
            {
                if (GameController.OnGodot)
                {
                    SceneControl.Set("texture", value?.GodotTexture);
                }
            }
        }

        private protected override Godot.Control SpawnSceneControl()
        {
            return new Godot.TextureRect();
        }
    }
}
