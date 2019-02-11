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
    [ControlWrap(typeof(Godot.TextureButton))]
    public class TextureButton : BaseButton
    {
        private Texture _textureNormal;

        public TextureButton()
        {
        }

        public TextureButton(string name) : base(name)
        {
        }

        internal TextureButton(Godot.TextureButton button) : base(button)
        {
        }

        public Texture TextureNormal
        {
            get => _textureNormal ?? (GameController.OnGodot
                       ? new GodotTextureSource((Godot.Texture) SceneControl.Get("texture_normal"))
                       : null);
            set
            {
                if (GameController.OnGodot)
                {
                    SceneControl.Set("texture_normal", value.GodotTexture);
                }

                _textureNormal = value;
            }
        }

        private protected override Godot.Control SpawnSceneControl()
        {
            return new Godot.TextureButton();
        }

        protected internal override void Draw(DrawingHandleScreen handle)
        {
            if (GameController.OnGodot)
            {
                return;
            }

            var texture = TextureNormal;

            if (texture == null)
            {
                return;
            }

            handle.DrawTextureRect(texture, new UIBox2(Vector2.Zero, Size), false);
        }

        private protected override void SetGodotProperty(string property, object value, GodotAssetScene context)
        {
            base.SetGodotProperty(property, value, context);

            if (property == "texture_normal")
            {
                var extRef = context.GetExtResource((GodotAsset.TokenExtResource) value);
                ResourcePath godotPathToResourcePath;
                try
                {
                    godotPathToResourcePath = GodotPathUtility.GodotPathToResourcePath(extRef.Path);
                }
                catch (ArgumentException)
                {
                    Logger.Error("TextureButton is referencing non-VFS Godot path {0}.", extRef.Path);
                    return;
                }
                var texture = IoCManager.Resolve<IResourceCache>()
                    .GetResource<TextureResource>(godotPathToResourcePath);
                TextureNormal = texture;
            }
        }
    }
}
