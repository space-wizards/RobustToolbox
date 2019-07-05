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
    [ControlWrap("TextureButton")]
    public class TextureButton : BaseButton
    {
        public const string StylePropertyTexture = "texture";
        public const string StylePseudoClassNormal = "normal";
        public const string StylePseudoClassHover = "hover";
        public const string StylePseudoClassDisabled = "disabled";
        public const string StylePseudoClassPressed = "pressed";

        public TextureButton()
        {
        }

        public TextureButton(string name) : base(name)
        {
        }

        public Texture TextureNormal { get; set; }
        public Texture TextureHover { get; set; }

        protected override void Initialize()
        {
            base.Initialize();

            DrawModeChanged();
        }

        protected override void DrawModeChanged()
        {
            switch (DrawMode)
            {
                case DrawModeEnum.Normal:
                    StylePseudoClass = StylePseudoClassNormal;
                    break;
                case DrawModeEnum.Pressed:
                    StylePseudoClass = StylePseudoClassPressed;
                    break;
                case DrawModeEnum.Hover:
                    StylePseudoClass = StylePseudoClassHover;
                    break;
                case DrawModeEnum.Disabled:
                    StylePseudoClass = StylePseudoClassDisabled;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        protected internal override void Draw(DrawingHandleScreen handle)
        {
            var texture = TextureNormal;

            if (IsHovered && TextureHover != null)
            {
                texture = TextureHover;
            }

            if (texture == null)
            {
                TryGetStyleProperty(StylePropertyTexture, out texture);
                if (texture == null)
                {
                    return;
                }
            }

            handle.DrawTextureRectRegion(texture, PixelSizeBox);
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
