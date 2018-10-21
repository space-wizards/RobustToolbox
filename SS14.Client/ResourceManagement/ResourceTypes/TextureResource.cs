using SS14.Client.Graphics;
using SS14.Client.Interfaces.ResourceManagement;
using SS14.Shared.Log;
using SS14.Shared.Utility;
using System;
using System.IO;

namespace SS14.Client.ResourceManagement
{
    public class TextureResource : BaseResource
    {
        public override ResourcePath Fallback { get; } = new ResourcePath("/Textures/noSprite.png");
        public Texture Texture { get; private set; }
        private Godot.ImageTexture godotTexture;

        public override void Load(IResourceCache cache, ResourcePath path)
        {
            if (!GameController.OnGodot)
            {
                Texture = new BlankTexture();
                return;
            }

            if (!cache.ContentFileExists(path))
            {
                throw new FileNotFoundException("Content file does not exist for texture");
            }

            if (!cache.TryGetDiskFilePath(path, out string diskPath))
            {
                throw new InvalidOperationException("Textures can only be loaded from disk.");
            }

            godotTexture = new Godot.ImageTexture();
            godotTexture.Load(diskPath);
            // If it fails to load it won't change the texture dimensions, so they'll still be at zero.
            if (godotTexture.GetWidth() == 0)
            {
                throw new InvalidDataException();
            }

            // Disable filter by default because pixel art.
            godotTexture.SetFlags(godotTexture.GetFlags() & ~(int) Godot.Texture.FlagsEnum.Filter);
            Texture = new GodotTextureSource(godotTexture);
            // Primarily for tracking down iCCP sRGB errors in the image files.
            Logger.DebugS("res.tex", $"Loaded texture {Path.GetFullPath(diskPath)}.");
        }

        public static implicit operator Texture(TextureResource res)
        {
            return res?.Texture;
        }

        public override void Dispose()
        {
            if (GameController.OnGodot)
            {
                godotTexture.Dispose();
            }
        }
    }
}
