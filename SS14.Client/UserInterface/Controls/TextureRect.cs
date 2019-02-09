using System;
using SS14.Client.Graphics;
using SS14.Client.Graphics.Drawing;
using SS14.Client.Interfaces.ResourceManagement;
using SS14.Client.ResourceManagement;
using SS14.Client.Utility;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using SS14.Shared.Maths;
using SS14.Shared.Utility;

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

        private Texture _texture;
        public Texture Texture
        {
            // TODO: Maybe store the texture passed in in case it's like a TextureResource or whatever.
            get => _texture ?? (GameController.OnGodot ? new GodotTextureSource((Godot.Texture)SceneControl.Get("texture")) : null);
            set
            {
                if (GameController.OnGodot)
                {
                    SceneControl.Set("texture", value?.GodotTexture);
                }

                _texture = value;
            }
        }

        private protected override Godot.Control SpawnSceneControl()
        {
            return new Godot.TextureRect();
        }

        protected internal override void Draw(DrawingHandleScreen handle)
        {
            base.Draw(handle);

            if (!GameController.OnGodot && _texture != null)
            {
                handle.DrawTextureRect(_texture, UIBox2.FromDimensions(Vector2.Zero, Size), false);
            }
        }

        private protected override void SetGodotProperty(string property, object value, GodotAssetScene context)
        {
            base.SetGodotProperty(property, value, context);

            if (property == "texture")
            {
                var extRef = context.GetExtResource((GodotAsset.TokenExtResource) value);
                ResourcePath godotPathToResourcePath;
                try
                {
                    godotPathToResourcePath = GodotPathUtility.GodotPathToResourcePath(extRef.Path);
                }
                catch (ArgumentException)
                {
                    Logger.Error("TextureRect is referencing non-VFS Godot path {0}.", extRef.Path);
                    return;
                }
                var texture = IoCManager.Resolve<IResourceCache>()
                    .GetResource<TextureResource>(godotPathToResourcePath);
                Texture = _texture;
            }
        }
    }
}
