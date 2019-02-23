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
        private bool _canShrink = false;
        private StretchMode _stretch = StretchMode.Keep;

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
            get => _texture ?? (GameController.OnGodot
                       ? new GodotTextureSource((Godot.Texture) SceneControl.Get("texture"))
                       : null);
            set
            {
                if (GameController.OnGodot)
                {
                    SceneControl.Set("texture", value?.GodotTexture);
                }

                _texture = value;
                MinimumSizeChanged();
            }
        }

        private protected override Godot.Control SpawnSceneControl()
        {
            return new Godot.TextureRect();
        }

        protected override void Initialize()
        {
            base.Initialize();

#pragma warning disable 618
            if (GameController.OnGodot && Stretch == StretchMode.ScaleOnExpand)
#pragma warning restore 618
            {
                // Turn the old compat mode into the non deprecated mode.
                if (CanShrink)
                {
                    Stretch = StretchMode.Scale;
                }
                else
                {
                    Stretch = StretchMode.Keep;
                }
            }
        }

        protected internal override void Draw(DrawingHandleScreen handle)
        {
            base.Draw(handle);

            if (_texture == null || GameController.OnGodot)
            {
                return;
            }

            switch (Stretch)
            {
                case StretchMode.Scale:
                    handle.DrawTexture(_texture, Vector2.Zero);
                    break;
                case StretchMode.Tile:
                // TODO: Implement Tile.
                case StretchMode.Keep:
                    handle.DrawTextureRect(_texture, UIBox2.FromDimensions(Vector2.Zero, _texture.Size), false);
                    break;
                case StretchMode.KeepCentered:
                {
                    var position = (Vector2i) (Size - _texture.Size) / 2;
                    handle.DrawTexture(_texture, position);
                    break;
                }
                case StretchMode.KeepAspect:
                case StretchMode.KeepAspectCentered:
                {
                    var width = _texture.Width * (Size.Y / _texture.Height);
                    var height = Size.Y;
                    if (width > Size.X)
                    {
                        width = Size.X;
                        height = _texture.Height * (Size.X / _texture.Width);
                    }

                    var size = new Vector2(width, height);
                    var position = Vector2.Zero;
                    if (Stretch == StretchMode.KeepAspectCentered)
                    {
                        position = (Size - size) / 2;
                    }

                    handle.DrawTextureRect(_texture, UIBox2.FromDimensions(position, size), false);
                    break;
                }
                case StretchMode.KeepAspectCovered:
                    // Calculate the scale necessary to fit width and height to control size.
                    var (scaleX, scaleY) = Size / _texture.Size;
                    // Use whichever scale is greater.
                    var scale = Math.Max(scaleX, scaleY);
                    var texSize = _texture.Size * scale;
                    // Offset inside the actual texture.
                    var offset = (_texture.Size - Size) / scale / 2f;
                    handle.DrawTextureRectRegion(_texture, SizeBox, UIBox2.FromDimensions(offset, Size / scale));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
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
                Texture = texture;
            }
        }

        public bool CanShrink
        {
            get => GameController.OnGodot ? (bool) SceneControl.Get("expand") : _canShrink;
            set
            {
                if (GameController.OnGodot)
                {
                    SceneControl.Set("expand", value);
                }
                else
                {
                    _canShrink = value;
                    MinimumSizeChanged();
                }
            }
        }

        public StretchMode Stretch
        {
            get => GameController.OnGodot ? (StretchMode) SceneControl.Get("stretch_mode") : _stretch;
            set
            {
                if (GameController.OnGodot)
                {
                    SceneControl.Set("stretch_mode", (Godot.TextureRect.StretchModeEnum) value);
                }
                else
                {
#pragma warning disable 618
                    if (value == StretchMode.ScaleOnExpand)
#pragma warning restore 618
                    {
                        throw new ArgumentException("ScaleOnExpand is a deprecated holdover from Godot, do not use it.",
                            nameof(value));
                    }

                    _stretch = value;
                }
            }
        }

        public enum StretchMode
        {
            Scale = 1,
            Tile = 2,
            Keep = 3,
            KeepCentered = 4,
            KeepAspect = 5,
            KeepAspectCentered = 7,
            KeepAspectCovered = 8,

            [Obsolete("This is a deprecated Godot thing for compatibility, do not use.")]
            ScaleOnExpand = 0,
        }

        protected override Vector2 CalculateMinimumSize()
        {
            if (GameController.OnGodot || _texture == null || CanShrink)
            {
                return Vector2.Zero;
            }

            return Texture.Size;
        }
    }
}
