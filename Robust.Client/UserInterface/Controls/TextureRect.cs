using System;
using Robust.Client.Graphics;
using Robust.Client.Graphics.Drawing;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.ResourceManagement;
using Robust.Client.Utility;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Client.UserInterface.Controls
{
    [ControlWrap("TextureRect")]
    public class TextureRect : Control
    {
        private bool _canShrink;
        private StretchMode _stretch = StretchMode.Keep;

        public TextureRect()
        {
        }

        public TextureRect(string name) : base(name)
        {
        }

        private Texture _texture;

        public Texture Texture
        {
            get => _texture;
            set
            {
                _texture = value;
                MinimumSizeChanged();
            }
        }

        protected internal override void Draw(DrawingHandleScreen handle)
        {
            base.Draw(handle);

            if (_texture == null)
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
                    handle.DrawTextureRectRegion(_texture, UIBox2.FromDimensions(Vector2.Zero, _texture.Size));
                    break;
                case StretchMode.KeepCentered:
                {
                    var position = (PixelSize - _texture.Size) / 2;
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

                    handle.DrawTextureRectRegion(_texture, UIBox2.FromDimensions(position, size));
                    break;
                }
                case StretchMode.KeepAspectCovered:
                    // Calculate the scale necessary to fit width and height to control size.
                    var (scaleX, scaleY) = Size / _texture.Size;
                    // Use whichever scale is greater.
                    var scale = Math.Max(scaleX, scaleY);
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
            get => _canShrink;
            set
            {
                _canShrink = value;
                MinimumSizeChanged();
            }
        }

        public StretchMode Stretch
        {
            get => _stretch;
            set
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
            if (_texture == null || CanShrink)
            {
                return Vector2.Zero;
            }

            return Texture.Size;
        }
    }
}
