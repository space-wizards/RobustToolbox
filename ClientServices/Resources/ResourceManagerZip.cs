using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ClientInterfaces.Configuration;
using ClientInterfaces.Resource;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using GorgonLibrary.Sprites;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using SS13_Shared;
using Font = GorgonLibrary.Graphics.Font;
using Image = GorgonLibrary.Graphics.Image;

namespace ClientServices.Resources
{
    public class ResourceManager : IResourceManager
    {
        private const int zipBufferSize = 4096;
        private readonly IConfigurationManager _configurationManager;
        private readonly Dictionary<string, Font> _fonts = new Dictionary<string, Font>();

        private readonly Dictionary<string, Image> _images = new Dictionary<string, Image>();
        private readonly Dictionary<string, FXShader> _shaders = new Dictionary<string, FXShader>();
        private readonly Dictionary<string, SpriteInfo> _spriteInfos = new Dictionary<string, SpriteInfo>();
        private readonly Dictionary<string, Sprite> _sprites = new Dictionary<string, Sprite>();
        private readonly Dictionary<string, AnimationCollection> _animationCollections = new Dictionary<string, AnimationCollection>(); 
        private readonly List<string> supportedImageExtensions = new List<string> {".png"};

        public ResourceManager(IConfigurationManager configurationManager)
        {
            _configurationManager = configurationManager;
            LoadResourceZip();
        }

        #region Resource Loading & Disposal

        /// <summary>
        ///  <para>Loads all Resources from given Zip into the respective Resource Lists and Caches</para>
        /// </summary>
        public void LoadResourceZip()
        {
            string zipPath = _configurationManager.GetResourcePath();
            string password = _configurationManager.GetResourcePassword();
            if (!File.Exists(zipPath)) throw new FileNotFoundException("Specified Zip does not exist: " + zipPath);

            FileStream zipFileStream = File.OpenRead(zipPath);
            var zipFile = new ZipFile(zipFileStream);

            if (!string.IsNullOrWhiteSpace(password)) zipFile.Password = password;

            IOrderedEnumerable<ZipEntry> filesInZip = from ZipEntry e in zipFile
                                                      where e.IsFile
                                                      orderby
                                                          supportedImageExtensions.Contains(
                                                              Path.GetExtension(e.Name).ToLowerInvariant()) descending
                                                      //Loading images first so the TAI files that come after can be loaded correctly.
                                                      select e;

            foreach (ZipEntry entry in filesInZip)
            {
                if (supportedImageExtensions.Contains(Path.GetExtension(entry.Name).ToLowerInvariant()))
                {
                    Image loadedImg = LoadImageFrom(zipFile, entry);
                    if (loadedImg == null) continue;
                    else _images.Add(loadedImg.Name, loadedImg);
                }
                else
                {
                    switch (Path.GetExtension(entry.Name).ToLowerInvariant())
                    {
                        case ".fx":
                            FXShader loadedShader = LoadShaderFrom(zipFile, entry);
                            if (loadedShader == null) continue;
                            else _shaders.Add(loadedShader.Name, loadedShader);
                            break;

                        case ".tai":
                            IEnumerable<Sprite> loadedSprites = LoadSpritesFrom(zipFile, entry);
                            foreach (
                                Sprite current in loadedSprites.Where(current => !_sprites.ContainsKey(current.Name)))
                                _sprites.Add(current.Name, current);
                            break;

                        case ".ttf":
                            Font loadedFont = LoadFontFrom(zipFile, entry);
                            if (loadedFont == null) continue;
                            else _fonts.Add(loadedFont.Name, loadedFont);
                            break;

                        case ".xml":
                            if (entry.Name.Contains("Animations"))
                            {
                                AnimationCollection animationCollection = LoadAnimationCollectionFrom(zipFile, entry);
                                if (animationCollection == null) continue;
                                else _animationCollections.Add(animationCollection.Name, animationCollection);
                            }
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
        ///  <para>Clears all Resource lists</para>
        /// </summary>
        public void ClearLists()
        {
            _images.Clear();
            _shaders.Clear();
            _fonts.Clear();
            _spriteInfos.Clear();
            _sprites.Clear();
        }

        /// <summary>
        ///  <para>Loads Image from given Zip-File and Entry.</para>
        /// </summary>
        private Image LoadImageFrom(ZipFile zipFile, ZipEntry imageEntry)
        {
            string ResourceName = Path.GetFileNameWithoutExtension(imageEntry.Name).ToLowerInvariant();

            if (ImageCache.Images.Contains(ResourceName))
                return null;

            var byteBuffer = new byte[zipBufferSize];

            Stream zipStream = zipFile.GetInputStream(imageEntry);
            //Will throw exception is missing or wrong password. Handle this.

            var memStream = new MemoryStream();

            StreamUtils.Copy(zipStream, memStream, byteBuffer);
            memStream.Position = 0;

            Image loadedImg = Image.FromStream(ResourceName, memStream, (int) memStream.Length);

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

            var byteBuffer = new byte[zipBufferSize];

            Stream zipStream = zipFile.GetInputStream(shaderEntry);
            //Will throw exception is missing or wrong password. Handle this.

            var memStream = new MemoryStream();

            StreamUtils.Copy(zipStream, memStream, byteBuffer);
            memStream.Position = 0;

            FXShader loadedShader = FXShader.FromStream(ResourceName, memStream, ShaderCompileOptions.Debug,
                                                        (int) memStream.Length, false);

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

            var byteBuffer = new byte[zipBufferSize];

            Stream zipStream = zipFile.GetInputStream(fontEntry);
            //Will throw exception is missing or wrong password. Handle this.

            var memStream = new MemoryStream();

            StreamUtils.Copy(zipStream, memStream, byteBuffer);
            memStream.Position = 0;

            Font loadedFont = Font.FromStream(ResourceName, memStream, (int) memStream.Length, 9, false);

            loadedFont.AntiAlias = true;
            loadedFont.Refresh();

            memStream.Close();
            zipStream.Close();
            memStream.Dispose();
            zipStream.Dispose();

            return loadedFont;
        }

        /// <summary>
        /// Loads animation collection from given zipfile and entry.
        /// </summary>
        /// <param name="zipFile"></param>
        /// <param name="entry"></param>
        /// <returns></returns>
        private AnimationCollection LoadAnimationCollectionFrom(ZipFile zipFile, ZipEntry entry)
        {
            string ResourceName = Path.GetFileNameWithoutExtension(entry.Name).ToLowerInvariant();


            var byteBuffer = new byte[zipBufferSize];

            Stream zipStream = zipFile.GetInputStream(entry);
            //Will throw exception is missing or wrong password. Handle this.

            System.Xml.Serialization.XmlSerializer serializer =
                new System.Xml.Serialization.XmlSerializer(typeof(AnimationCollection));

            var animationCollection = (AnimationCollection)serializer.Deserialize(zipStream);
            zipStream.Close();
            zipStream.Dispose();

            return animationCollection;
        }

        /// <summary>
        ///  <para>Loads TAI from given Zip-File and Entry and creates & loads Sprites from it.</para>
        /// </summary>
        private IEnumerable<Sprite> LoadSpritesFrom(ZipFile zipFile, ZipEntry taiEntry)
        {
            string ResourceName = Path.GetFileNameWithoutExtension(taiEntry.Name).ToLowerInvariant();

            var loadedSprites = new List<Sprite>();

            var byteBuffer = new byte[zipBufferSize];

            Stream zipStream = zipFile.GetInputStream(taiEntry);
            //Will throw exception is missing or wrong password. Handle this.

            var memStream = new MemoryStream();

            StreamUtils.Copy(zipStream, memStream, byteBuffer);
            memStream.Position = 0;

            var taiReader = new StreamReader(memStream, true);
            string loadedTAI = taiReader.ReadToEnd();

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

                Image atlasTex = ImageCache.Images[splitResourceName[0]];
                //Grab the image for the sprite from the cache.

                var info = new SpriteInfo();
                info.Name = originalName;

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

                info.Offsets = new Vector2D((float) Math.Round(offsetX*atlasTex.Width, 1),
                                            (float) Math.Round(offsetY*atlasTex.Height, 1));
                info.Size = new Vector2D((float) Math.Round(sizeX*atlasTex.Width, 1),
                                         (float) Math.Round(sizeY*atlasTex.Height, 1));

                if (!_spriteInfos.ContainsKey(originalName)) _spriteInfos.Add(originalName, info);

                loadedSprites.Add(new Sprite(originalName, atlasTex, info.Offsets, info.Size));
            }

            return loadedSprites;
        }

        #endregion

        #region Resource Retrieval

        /// <summary>
        ///  <para>Retrieves the Image with the given key from the Resource list and returns it as a Sprite.</para>
        ///  <para>If a sprite has been created before using this method, it will return that Sprite. Returns error Sprite if not found.</para>
        /// </summary>
        public Sprite GetSpriteFromImage(string key)
        {
            key = key.ToLowerInvariant();
            if (_images.ContainsKey(key))
            {
                if (_sprites.ContainsKey(key))
                {
                    return _sprites[key];
                }
                else
                {
                    var newSprite = new Sprite(key, _images[key]);
                    _sprites.Add(key, newSprite);
                    return newSprite;
                }
            }
            else return _sprites["nosprite"];
        }

        /// <summary>
        ///  Retrieves the Sprite with the given key from the Resource List. Returns error Sprite if not found.
        /// </summary>
        public Sprite GetSprite(string key)
        {
            key = key.ToLowerInvariant();
            if (_sprites.ContainsKey(key))
            {
                _sprites[key].Color = Color.White;
                return _sprites[key];
            }
            else return GetSpriteFromImage(key);
        }

        public List<Sprite> GetSprites()
        {
            return _sprites.Values.ToList();
        } 

        public List<string> GetSpriteKeys()
        {
            return _sprites.Keys.ToList();
        } 

        /// <summary>
        /// Checks if a sprite with the given key is in the Resource List.
        /// </summary>
        /// <param name="key">key to check</param>
        /// <returns></returns>
        public bool SpriteExists(string key)
        {
            key = key.ToLowerInvariant();
            return _sprites.ContainsKey(key);
        }

        /// <summary>
        /// Checks if an Image with the given key is in the Resource List.
        /// </summary>
        /// <param name="key">key to check</param>
        /// <returns></returns>
        public bool ImageExists(string key)
        {
            key = key.ToLowerInvariant();
            return _images.ContainsKey(key);
        }

        /// <summary>
        ///  Retrieves the SpriteInfo with the given key from the Resource List. Returns null if not found.
        /// </summary>
        public SpriteInfo? GetSpriteInfo(string key)
        {
            key = key.ToLowerInvariant();
            if (_spriteInfos.ContainsKey(key)) return _spriteInfos[key];
            else return null;
        }

        /// <summary>
        ///  Retrieves the Shader with the given key from the Resource List. Returns null if not found.
        /// </summary>
        public FXShader GetShader(string key)
        {
            key = key.ToLowerInvariant();
            if (_shaders.ContainsKey(key)) return _shaders[key];
            else return null;
        }

        /// <summary>
        ///  Retrieves the Image with the given key from the Resource List. Returns error Image if not found.
        /// </summary>
        public Image GetImage(string key)
        {
            key = key.ToLowerInvariant();
            if (_images.ContainsKey(key)) return _images[key];
            else return _images["nosprite"];
        }

        /// <summary>
        ///  Retrieves the Font with the given key from the Resource List. Returns null if not found.
        /// </summary>
        public Font GetFont(string key)
        {
            key = key.ToLowerInvariant();
            if (_fonts.ContainsKey(key)) return _fonts[key];
            else return null;
        }

        #endregion
    }
}