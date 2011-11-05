using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO;
using System.Xml;
using System.Xml.Serialization;

using System.Windows.Forms;

using GorgonLibrary;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;
using GorgonLibrary.FileSystems;
using GorgonLibrary.Framework;
using GorgonLibrary.GUI;
using GorgonLibrary.Graphics.Utilities;

using System.Globalization;
using System.Drawing;
using System.Text.RegularExpressions;

using ClientConfigManager;

using ICSharpCode.SharpZipLib;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;

using Image = GorgonLibrary.Graphics.Image;
using Font = GorgonLibrary.Graphics.Font;

namespace ClientResourceManager
{
    public class ResMgr
    {
        private const int zipBufferSize = 4096;
        private readonly List<string> supportedImageExtensions = new List<string> { ".png" };

        private Dictionary<string, Image> Images = new Dictionary<string, Image>();
        private Dictionary<string, FXShader> Shaders = new Dictionary<string, FXShader>();
        private Dictionary<string, Font> Fonts = new Dictionary<string, Font>();
        private Dictionary<string, SpriteInfo> SpriteInfos = new Dictionary<string, SpriteInfo>();
        private Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>();

        private Dictionary<string, GUIElement> createdGuiElements = new Dictionary<string, GUIElement>();

        //  LEGACY METHOD.
        /// <summary>
        ///  Retrieves information describing a Gorgon ui element. Returns an empty rectangle if not found.
        /// </summary>
        public System.Drawing.Rectangle GetGUIInfo(string key)
        {
            if (createdGuiElements.ContainsKey(key))
            {
                return new Rectangle(createdGuiElements[key].Dimensions.X, createdGuiElements[key].Dimensions.Y, createdGuiElements[key].Dimensions.Width, createdGuiElements[key].Dimensions.Height);
            }
            else
            {
                GUIElement tryCreate = GetGUIElement(key);
                if(tryCreate != null)
                {
                    return new Rectangle(tryCreate.Dimensions.X, tryCreate.Dimensions.Y, tryCreate.Dimensions.Width, tryCreate.Dimensions.Height);
                }
                else return System.Drawing.Rectangle.Empty;
            }
        }

        #region Singleton
        private static ResMgr singleton;

        private ResMgr() { }

        public static ResMgr Singleton
        {
            get
            {
                if (singleton == null)
                {
                    singleton = new ResMgr();
                }
                return singleton;
            }
        } 
        #endregion

        #region Resource Loading & Disposal

        /// <summary>
        ///  <para>Loads all Resources from given Zip into the respective Resource Lists and Caches</para>
        /// </summary>
        public void LoadResourceZip(string zipPath, string password = null)
        {
            if (!File.Exists(zipPath)) throw new FileNotFoundException("Specified Zip does not exist: " + zipPath);

            FileStream zipFileStream = File.OpenRead(zipPath);
            ZipFile zipFile = new ZipFile(zipFileStream);

            if (!string.IsNullOrWhiteSpace(password)) zipFile.Password = password;

            var filesInZip = from ZipEntry e in zipFile
                             where e.IsFile
                             orderby supportedImageExtensions.Contains(Path.GetExtension(e.Name).ToLowerInvariant()) descending //Loading images first so the TAI files that come after can be loaded correctly.
                             select e;

            foreach (ZipEntry entry in filesInZip)
            {
                if (supportedImageExtensions.Contains(Path.GetExtension(entry.Name).ToLowerInvariant()))
                {
                    Image loadedImg = LoadImageFrom(zipFile, entry);
                    if (loadedImg == null) continue;
                    else Images.Add(loadedImg.Name, loadedImg);
                }
                else
                {
                    switch (Path.GetExtension(entry.Name).ToLowerInvariant())
                    {
                        case ".fx":
                            FXShader loadedShader = LoadShaderFrom(zipFile, entry);
                            if (loadedShader == null) continue;
                            else Shaders.Add(loadedShader.Name, loadedShader);
                            break;

                        case ".tai":
                            List<Sprite> loadedSprites = LoadSpritesFrom(zipFile, entry);
                            foreach (Sprite current in loadedSprites)
                                if (!Sprites.ContainsKey(current.Name)) Sprites.Add(current.Name, current);
                            break;

                        case ".ttf":
                            Font loadedFont = LoadFontFrom(zipFile, entry);
                            if (loadedFont == null) continue;
                            else Fonts.Add(loadedFont.Name, loadedFont);
                            break;
                    }
                }
            }

            zipFile.Close();
            zipFileStream.Close();
            zipFileStream.Dispose();

            GC.Collect();
        }

        /// <summary>
        ///  <para>Loads Image from given Zip-File and Entry.</para>
        /// </summary>
        private Image LoadImageFrom(ZipFile zipFile, ZipEntry imageEntry)
        {
            string ResourceName = Path.GetFileNameWithoutExtension(imageEntry.Name).ToLowerInvariant();

            if (ImageCache.Images.Contains(ResourceName))
                return null;

            byte[] byteBuffer = new byte[zipBufferSize];

            Stream zipStream = zipFile.GetInputStream(imageEntry); //Will throw exception is missing or wrong password. Handle this.

            MemoryStream memStream = new MemoryStream();

            StreamUtils.Copy(zipStream, memStream, byteBuffer);
            memStream.Position = 0;

            Image loadedImg = Image.FromStream(ResourceName, memStream, (int)memStream.Length);

            memStream.Close();
            zipStream.Close();
            memStream.Dispose();
            zipStream.Dispose();

            return loadedImg;
        }

        /// <summary>
        ///  <para>Loads Shader from given Zip-File and Entry.</para>
        /// </summary>
        private FXShader LoadShaderFrom(ZipFile zipFile, ZipEntry shaderEntry)
        {
            string ResourceName = Path.GetFileNameWithoutExtension(shaderEntry.Name).ToLowerInvariant();

            if (ShaderCache.Shaders.Contains(ResourceName))
                return null;

            byte[] byteBuffer = new byte[zipBufferSize];

            Stream zipStream = zipFile.GetInputStream(shaderEntry); //Will throw exception is missing or wrong password. Handle this.

            MemoryStream memStream = new MemoryStream();

            StreamUtils.Copy(zipStream, memStream, byteBuffer);
            memStream.Position = 0;

            FXShader loadedShader = FXShader.FromStream(ResourceName, memStream, ShaderCompileOptions.None, (int)memStream.Length, false);

            memStream.Close();
            zipStream.Close();
            memStream.Dispose();
            zipStream.Dispose();

            return loadedShader;
        }

        /// <summary>
        ///  <para>Loads Font from given Zip-File and Entry.</para>
        /// </summary>
        private Font LoadFontFrom(ZipFile zipFile, ZipEntry fontEntry)
        {
            string ResourceName = Path.GetFileNameWithoutExtension(fontEntry.Name).ToLowerInvariant();

            if (FontCache.Fonts.Contains(ResourceName))
                return null;

            byte[] byteBuffer = new byte[zipBufferSize];

            Stream zipStream = zipFile.GetInputStream(fontEntry); //Will throw exception is missing or wrong password. Handle this.

            MemoryStream memStream = new MemoryStream();

            StreamUtils.Copy(zipStream, memStream, byteBuffer);
            memStream.Position = 0;

            Font loadedFont = Font.FromStream(ResourceName, memStream, (int)memStream.Length, 10, false);

            memStream.Close();
            zipStream.Close();
            memStream.Dispose();
            zipStream.Dispose();

            return loadedFont;
        }

        /// <summary>
        ///  <para>Loads TAI from given Zip-File and Entry and creates & loads Sprites from it.</para>
        /// </summary>
        private List<Sprite> LoadSpritesFrom(ZipFile zipFile, ZipEntry taiEntry)
        {
            string ResourceName = Path.GetFileNameWithoutExtension(taiEntry.Name).ToLowerInvariant();

            List<Sprite> loadedSprites = new List<Sprite>();

            byte[] byteBuffer = new byte[zipBufferSize];

            Stream zipStream = zipFile.GetInputStream(taiEntry); //Will throw exception is missing or wrong password. Handle this.

            MemoryStream memStream = new MemoryStream();

            StreamUtils.Copy(zipStream, memStream, byteBuffer);
            memStream.Position = 0;

            StreamReader taiReader = new StreamReader(memStream, true);
            String loadedTAI = taiReader.ReadToEnd();

            memStream.Close();
            zipStream.Close();
            taiReader.Close();
            memStream.Dispose();
            zipStream.Dispose();
            taiReader.Dispose();

            string[] splitContents = Regex.Split(loadedTAI, "\r\n"); //Split by newlines.

            foreach (string line in splitContents)
            {
                if (String.IsNullOrWhiteSpace(line)) continue;

                string[] splitLine = line.Split(',');
                string[] fullPath = Regex.Split(splitLine[0], "\t");

                string originalName = Path.GetFileNameWithoutExtension(fullPath[0]).ToLowerInvariant();
                //The name of the original picture without extension, before it became part of the atlas. 
                //This will be the name we can find this under in our Resource lists.

                string[] splitResourceName = fullPath[2].Split('.');

                string imageName = splitResourceName[0].ToLowerInvariant();

                if (!ImageCache.Images.Contains(splitResourceName[0]))
                    continue; //Image for this sprite does not exist. Possibly set to defered later.

                Image atlasTex = ImageCache.Images[splitResourceName[0]]; //Grab the image for the sprite from the cache.

                SpriteInfo info = new SpriteInfo();
                info.name = originalName;

                float offsetX = 0;
                float offsetY = 0;
                float sizeX = 0;
                float sizeY = 0;

                if (splitLine.Length > 8) //Separated with ','. This causes some problems and happens on some EU PCs.
                {
                    offsetX = float.Parse(splitLine[3] + "." + splitLine[4], CultureInfo.InvariantCulture);
                    offsetY = float.Parse(splitLine[5] + "." + splitLine[6], CultureInfo.InvariantCulture);
                    sizeX = float.Parse(splitLine[8] + "." + splitLine[9], CultureInfo.InvariantCulture);
                    sizeY = float.Parse(splitLine[10] + "." + splitLine[11], CultureInfo.InvariantCulture);
                }
                else
                {
                    offsetX = float.Parse(splitLine[3], CultureInfo.InvariantCulture);
                    offsetY = float.Parse(splitLine[4], CultureInfo.InvariantCulture);
                    sizeX = float.Parse(splitLine[6], CultureInfo.InvariantCulture);
                    sizeY = float.Parse(splitLine[7], CultureInfo.InvariantCulture);
                }

                info.Offsets = new Vector2D((float)Math.Round(offsetX * (float)atlasTex.Width, 1), (float)Math.Round(offsetY * (float)atlasTex.Height, 1));
                info.Size = new Vector2D((float)Math.Round(sizeX * (float)atlasTex.Width, 1), (float)Math.Round(sizeY * (float)atlasTex.Height, 1));

                if (!SpriteInfos.ContainsKey(originalName)) SpriteInfos.Add(originalName, info);

                loadedSprites.Add(new Sprite(originalName, atlasTex, info.Offsets, info.Size));
            }

            return loadedSprites;
        }

        /// <summary>
        ///  <para>Clears all Resource lists</para>
        /// </summary>
        public void ClearLists()
        {
            Images.Clear();
            Shaders.Clear();
            Fonts.Clear();
            SpriteInfos.Clear();
            Sprites.Clear();
        }

        #endregion

        #region Resource Retrieval

        /// <summary>
        ///  Creates and retrieves Sprite or Image as Gorgon UI Element.
        /// </summary>
        public GUIElement GetGUIElement(string key)
        {
            key = key.ToLowerInvariant();

            if (createdGuiElements.ContainsKey(key)) return createdGuiElements[key];

            Sprite sprite;

            if (Sprites.ContainsKey(key)) sprite = Sprites[key];
            else sprite = GetSpriteFromImage(key);

            GUIElement newElement = new GUIElement(key, sprite);

            if(!createdGuiElements.ContainsKey(key)) createdGuiElements.Add(key, newElement);

            return newElement;
        }

        /// <summary>
        ///  <para>Retrieves the Image with the given key from the Resource list and returns it as a Sprite.</para>
        ///  <para>If a sprite has been created before using this method, it will return that Sprite. Returns error Sprite if not found.</para>
        /// </summary>
        public Sprite GetSpriteFromImage(string key)
        {
            key = key.ToLowerInvariant();
            if (Images.ContainsKey(key))
            {
                if (Sprites.ContainsKey(key))
                {
                    return Sprites[key];
                }
                else
                {
                    Sprite newSprite = new Sprite(key, Images[key]);
                    Sprites.Add(key, newSprite);
                    return newSprite;
                }
            }
            else return Sprites["nosprite"];
        }

        /// <summary>
        ///  Retrieves the Sprite with the given key from the Resource List. Returns error Sprite if not found.
        /// </summary>
        public Sprite GetSprite(string key)
        {
            key = key.ToLowerInvariant();
            if (Sprites.ContainsKey(key))
            {
                Sprites[key].Color = Color.White;
                return Sprites[key];
            }
            else return GetSpriteFromImage(key);
        }

        /// <summary>
        /// Checks if a sprite with the given key is in the Resource List.
        /// </summary>
        /// <param name="key">key to check</param>
        /// <returns></returns>
        public bool SpriteExists(string key)
        {
            key = key.ToLowerInvariant();
            return Sprites.ContainsKey(key);
        }

        /// <summary>
        /// Checks if an Image with the given key is in the Resource List.
        /// </summary>
        /// <param name="key">key to check</param>
        /// <returns></returns>
        public bool ImageExists(string key)
        {
            key = key.ToLowerInvariant();
            return Images.ContainsKey(key);
        }

        /// <summary>
        ///  Retrieves the SpriteInfo with the given key from the Resource List. Returns null if not found.
        /// </summary>
        public SpriteInfo? GetSpriteInfo(string key)
        {
            key = key.ToLowerInvariant();
            if (SpriteInfos.ContainsKey(key)) return SpriteInfos[key];
            else return null;
        }

        /// <summary>
        ///  Retrieves the Shader with the given key from the Resource List. Returns null if not found.
        /// </summary>
        public FXShader GetShader(string key)
        {
            key = key.ToLowerInvariant();
            if (Shaders.ContainsKey(key)) return Shaders[key];
            else return null;
        }

        /// <summary>
        ///  Retrieves the Image with the given key from the Resource List. Returns error Image if not found.
        /// </summary>
        public Image GetImage(string key)
        {
            key = key.ToLowerInvariant();
            if (Images.ContainsKey(key)) return Images[key];
            else return Images["nosprite"];
        }

        /// <summary>
        ///  Retrieves the Font with the given key from the Resource List. Returns null if not found.
        /// </summary>
        public Font GetFont(string key)
        {
            key = key.ToLowerInvariant();
            if (Fonts.ContainsKey(key)) return Fonts[key];
            else return null;
        }

        #endregion
    }

    public struct SpriteInfo
    {
        public string name;
        public Vector2D Offsets;
        public Vector2D Size;
    }
}
