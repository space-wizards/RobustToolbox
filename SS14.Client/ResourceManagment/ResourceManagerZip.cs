using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using SFML.Graphics;
using SFML.System;
using SS14.Client.Graphics.Collection;
using SS14.Client.Graphics.Shader;
using SS14.Client.Graphics.Sprite;
using SS14.Client.Interfaces.Configuration;
using SS14.Client.Interfaces.Resource;
using SS14.Shared.GameObjects;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TextureCache = SS14.Client.Graphics.texture.TextureCache;

namespace SS14.Client.Resources
{
    [IoCTarget]
    public class ResourceManager : IResourceManager
    {
        private const int zipBufferSize = 4096;
        private MemoryStream VertexShader, FragmentShader;

        private readonly IPlayerConfigurationManager _configurationManager;
        private readonly Dictionary<string, Font> _fonts = new Dictionary<string, Font>();
        private readonly Dictionary<string, ParticleSettings> _particles = new Dictionary<string, ParticleSettings>();
        private readonly Dictionary<string, Texture> _textures = new Dictionary<string, Texture>();
        private readonly Dictionary<string, GLSLShader> _shaders = new Dictionary<string, GLSLShader>();
        private readonly Dictionary<string, TechniqueList> _TechniqueList = new Dictionary<string, TechniqueList>();
        private readonly Dictionary<string, SpriteInfo> _spriteInfos = new Dictionary<string, SpriteInfo>();
        private readonly Dictionary<string, Sprite> _sprites = new Dictionary<string, Sprite>();
        private readonly Dictionary<string, AnimationCollection> _animationCollections = new Dictionary<string, AnimationCollection>();
        private readonly Dictionary<string, AnimatedSprite> _animatedSprites = new Dictionary<string, AnimatedSprite>();
        private readonly List<string> supportedImageExtensions = new List<string> {".png"};

        private readonly Dictionary<Texture, string> _textureToKey = new Dictionary<Texture, string>();
        public Dictionary<Texture, string> TextureToKey => _textureToKey;

        public int done = 0;

        public ResourceManager(IPlayerConfigurationManager configurationManager)
        {
            _configurationManager = configurationManager;
        }

        #region Resource Loading & Disposal

        /// <summary>
        ///  <para>Loads the embedded base files.</para>
        /// </summary>
        public void LoadBaseResources()
        {
            Assembly _assembly = Assembly.GetExecutingAssembly();
            Stream _stream;

            _stream = _assembly.GetManifestResourceStream("SS14.Client._EmbeddedBaseResources.bluehigh.ttf");
            if (_stream != null)
                _fonts.Add("base_font", new Font( _stream));
            _stream = null;

            _stream = _assembly.GetManifestResourceStream("SS14.Client._EmbeddedBaseResources.noSprite.png");
            if (_stream != null)
            {
                Texture nospriteimage = new Texture( _stream);
                _textures.Add("nosprite", nospriteimage);
                _sprites.Add("nosprite", new Sprite(nospriteimage));
            }
            _stream = null;
        }

        /// <summary>
        ///  <para>Loads the local resources as specified by the config</para>
        /// </summary>
        public void LoadLocalResources()
        {
            LoadResourceZip();
            LoadAnimatedSprites();
        }

        /// <summary>
        ///  <para>Loads all Resources from given Zip into the respective Resource Lists and Caches</para>
        /// </summary>
        public void LoadResourceZip(string path = null, string pw = null)
        {
            string zipPath = path ?? _configurationManager.GetResourcePath();
            string password = pw ?? _configurationManager.GetResourcePassword();

            if (Assembly.GetEntryAssembly().GetName().Name == "SS14.UnitTesting")
            {
                string debugPath = "..\\";
                debugPath += zipPath;
                zipPath = debugPath;
            }



            if (!File.Exists(zipPath))
                throw new FileNotFoundException("Specified Zip does not exist: " + zipPath);

            FileStream zipFileStream = File.OpenRead(zipPath);
            var zipFile = new ZipFile(zipFileStream);

            if (!string.IsNullOrWhiteSpace(password)) zipFile.Password = password;

            #region Sort Resource pack
            var directories = from ZipEntry a in zipFile
                              where a.IsDirectory
                              orderby a.Name.ToLowerInvariant() == "textures" descending
                              select a;

            Dictionary<string, List<ZipEntry>> sorted = new Dictionary<string, List<ZipEntry>>();

            foreach (ZipEntry dir in directories)
            {
                if (sorted.ContainsKey(dir.Name.ToLowerInvariant())) continue; //Duplicate folder? shouldnt happen.

                List<ZipEntry> folderContents = (from ZipEntry entry in zipFile
                                                 where entry.Name.ToLowerInvariant().Contains(dir.Name.ToLowerInvariant())
                                                 where entry.IsFile
                                                 select entry).ToList();

                sorted.Add(dir.Name.ToLowerInvariant(), folderContents);
            }

            sorted = sorted.OrderByDescending(x => x.Key == "textures/").ToDictionary(x => x.Key, x => x.Value); //Textures first.
            #endregion

            LogManager.Log("Loading resources...");

            #region Load Resources
            foreach (KeyValuePair<string, List<ZipEntry>> current in sorted)
            {
                switch (current.Key)
                {
                    case("textures/"):

                        int itemCount = current.Value.Count();
                        Task<Texture>[] taskArray = new Task<Texture>[itemCount];
                        for(int i = 0; i < itemCount; i++)
                        {
                            ZipEntry texture = current.Value[i];

                            if(supportedImageExtensions.Contains(Path.GetExtension(texture.Name).ToLowerInvariant()))
                            {
                                taskArray[i] = Task<Texture>.Factory.StartNew(() =>
                                {
                                    return LoadTextureFrom(zipFile, texture);
                                });

                            }
                        }

                        Task.WaitAll(taskArray);
                        for (int i = 0; i < taskArray.Count(); i++)
                        {
                            Texture loadedImg = taskArray[i].Result;
                            if (loadedImg == null) continue;
                            else _textures.Add(Path.GetFileNameWithoutExtension(current.Value[i].Name), loadedImg);
                        }

                        break;

                    case("tai/"): // Tai? HANK HANK
                        LogManager.Log("Loading tai...");
                        foreach (ZipEntry tai in current.Value)
                        {
                            if (Path.GetExtension(tai.Name).ToLowerInvariant() == ".tai")
                            {
                                IEnumerable<KeyValuePair<string, Sprite>> loadedSprites = LoadSpritesFrom(zipFile, tai);
                                foreach (var currentSprite in loadedSprites.Where(currentSprite => !_sprites.ContainsKey(currentSprite.Key)))
                                    _sprites.Add(currentSprite.Key, currentSprite.Value);
                            }
                        }
                        break;

                    case("fonts/"):
                        LogManager.Log("Loading fonts...");
                        foreach (ZipEntry font in current.Value)
                        {
                            if (Path.GetExtension(font.Name).ToLowerInvariant() == ".ttf")
                            {
                                Font loadedFont = LoadFontFrom(zipFile, font);
                                if (loadedFont == null) continue;
                                string ResourceName = Path.GetFileNameWithoutExtension(font.Name).ToLowerInvariant();
                                _fonts.Add(ResourceName, loadedFont);
                            }
                        }
                        break;

                    case("particlesystems/"):
                        LogManager.Log("Loading particlesystems...");
                        foreach (ZipEntry particles in current.Value)
                        {
                            if (Path.GetExtension(particles.Name).ToLowerInvariant() == ".xml")
                            {
                                ParticleSettings particleSettings = LoadParticlesFrom(zipFile, particles);
                                if (particleSettings == null) continue;
                                else _particles.Add(Path.GetFileNameWithoutExtension(particles.Name), particleSettings);
                            }
                        }
                        break;

                    case ("shaders/"):
                        {
                            LogManager.Log("Loading shaders...");
                            GLSLShader LoadedShader;
                            TechniqueList List;

                            foreach (ZipEntry shader in current.Value)
                            {
                                int FirstIndex = shader.Name.IndexOf('/') ;
                                int LastIndex = shader.Name.LastIndexOf('/');

                                if (FirstIndex != LastIndex)  // if the shader pixel/fragment files are in folder/technique group, construct shader and add it to a technique list.
                                {
                                    string FolderName = shader.Name.Substring(FirstIndex + 1 , LastIndex - FirstIndex - 1);

                                    if(!_TechniqueList.Keys.Contains(FolderName))
                                    {
                                        List = new TechniqueList();
                                        List.Name = FolderName;
                                        _TechniqueList.Add(FolderName,List);
                                    }


                                    LoadedShader = LoadShaderFrom(zipFile, shader);
                                    if (LoadedShader == null) continue;
                                    else _TechniqueList[FolderName].Add(LoadedShader);
                                }

                                // if the shader is not in a folder/technique group, add it to the shader dictionary
                                else if (Path.GetExtension(shader.Name).ToLowerInvariant() == ".vert" || Path.GetExtension(shader.Name).ToLowerInvariant() == ".frag")
                                {
                                    LoadedShader = LoadShaderFrom(zipFile, shader);
                                    if (LoadedShader == null) continue;

                                    else _shaders.Add(Path.GetFileNameWithoutExtension(shader.Name).ToLowerInvariant(), LoadedShader);
                                }
                            }
                            break;
                        }

                    case("animations/"):
                        LogManager.Log("Loading animations...");
                        foreach (ZipEntry animation in current.Value)
                        {
                            if (Path.GetExtension(animation.Name).ToLowerInvariant() == ".xml")
                            {
                                AnimationCollection animationCollection = LoadAnimationCollectionFrom(zipFile, animation);
                                if (animationCollection == null) continue;
                                else _animationCollections.Add(animationCollection.Name, animationCollection);
                            }
                        }
                        break;
                }

            }
            #endregion

            sorted = null;
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
            _textures.Clear();
            _shaders.Clear();
            _TechniqueList.Clear();
            _fonts.Clear();
            _spriteInfos.Clear();
            _sprites.Clear();
        }

        /// <summary>
        ///  <para>Loads a Texture from given Zip-File and Entry.</para>
        /// </summary>
        private Texture LoadTextureFrom(ZipFile zipFile, ZipEntry imageEntry)
        {
            string ResourceName = Path.GetFileNameWithoutExtension(imageEntry.Name).ToLowerInvariant();

            if (TextureCache.Textures.ContainsKey(ResourceName))
                return TextureCache.Textures[ResourceName].Item1;

            var byteBuffer = new byte[zipBufferSize];

            try
            {
                Stream zipStream = zipFile.GetInputStream(imageEntry);
                var memStream = new MemoryStream();

                StreamUtils.Copy(zipStream, memStream, byteBuffer);
                memStream.Position = 0;

                Image img = new Image(memStream);
                bool[,] opacityMap = new bool[img.Size.X, img.Size.Y];
                for(int y = 0; y < img.Size.Y; y++)
                {
                    for(int x = 0; x < img.Size.X; x++)
                    {
                        Color pColor = img.GetPixel(Convert.ToUInt32(x), Convert.ToUInt32(y));
                        if(pColor.A > Limits.ClickthroughLimit)
                        {
                            opacityMap[x, y] = true;
                        }
                        else
                        {
                            opacityMap[x, y] = false;
                        }
                    }
                }

                Texture loadedImg = new Texture(memStream);
                TextureCache.Add(ResourceName, loadedImg, opacityMap);
                _textureToKey.Add(loadedImg, ResourceName);

                memStream.Close();
                zipStream.Close();
                memStream.Dispose();
                zipStream.Dispose();
                return loadedImg;

            }
            catch(Exception I)
            {
                System.Console.WriteLine("Failed to load " + imageEntry.Name + ": " + I.ToString());
            }

            return null;

        }

        /// <summary>
        ///  <para>Loads Shader from given Zip-File and Entry.</para>
        /// </summary>
        private GLSLShader LoadShaderFrom(ZipFile zipFile, ZipEntry shaderEntry)
        {
            string ResourceName = Path.GetFileNameWithoutExtension(shaderEntry.Name).ToLowerInvariant();


            var byteBuffer = new byte[zipBufferSize];

            Stream zipStream = zipFile.GetInputStream(shaderEntry);
            GLSLShader loadedShader;

            //Will throw exception if missing or wrong password. Handle this.


            if (shaderEntry.Name.Contains(".frag"))
            {
                FragmentShader = new MemoryStream();
                StreamUtils.Copy(zipStream, FragmentShader, byteBuffer);
                FragmentShader.Position = 0;
            }

            if (shaderEntry.Name.Contains(".vert"))
            {
                VertexShader = new MemoryStream();
                StreamUtils.Copy(zipStream, VertexShader, byteBuffer);
                VertexShader.Position = 0;
            }

            if (VertexShader != null && FragmentShader != null)
            {
                loadedShader = new GLSLShader(VertexShader, FragmentShader);
                loadedShader.ResourceName = ResourceName;
                VertexShader.Dispose();
                FragmentShader.Dispose();
                VertexShader = null;
                FragmentShader = null;

            }
            else
                loadedShader = null;



            zipStream.Close();
            zipStream.Dispose();

            return loadedShader;
        }

        /// <summary>
        ///  <para>Loads Font from given Zip-File and Entry.</para>
        /// </summary>
        private Font LoadFontFrom(ZipFile zipFile, ZipEntry fontEntry)
        {
            var byteBuffer = new byte[zipBufferSize];

            Stream zipStream = zipFile.GetInputStream(fontEntry);
            //Will throw exception is missing or wrong password. Handle this.

            var memStream = new MemoryStream();

            StreamUtils.Copy(zipStream, memStream, byteBuffer);
            memStream.Position = 0;

            Font loadedFont = new Font(memStream);


            // memStream.Close();
            zipStream.Close();
            // memStream.Dispose();
            zipStream.Dispose();

            return loadedFont;
        }

        /// <summary>
        /// Loads particle settings from given zipfile and entry.
        /// </summary>
        /// <param name="zipFile"></param>
        /// <param name="entry"></param>
        /// <returns></returns>
        private ParticleSettings LoadParticlesFrom(ZipFile zipFile, ZipEntry entry)
        {
            string ResourceName = Path.GetFileNameWithoutExtension(entry.Name).ToLowerInvariant();

            var byteBuffer = new byte[zipBufferSize];

            Stream zipStream = zipFile.GetInputStream(entry);
            //Will throw exception is missing or wrong password. Handle this.

            System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(ParticleSettings));

            var particleSettings = (ParticleSettings)serializer.Deserialize(zipStream);
            zipStream.Close();
            zipStream.Dispose();

            return particleSettings;
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

        public void LoadAnimatedSprites()
        {
            foreach (var col in _animationCollections)
            {
                _animatedSprites.Add(col.Key, new AnimatedSprite(col.Key, col.Value, this));
            }
        }

        /// <summary>
        ///  <para>Loads TAI from given Zip-File and Entry and creates & loads Sprites from it.</para>
        /// </summary>
        private IEnumerable<KeyValuePair<string, Sprite>> LoadSpritesFrom(ZipFile zipFile, ZipEntry taiEntry)
        {
            string ResourceName = Path.GetFileNameWithoutExtension(taiEntry.Name).ToLowerInvariant();

            var loadedSprites = new List<KeyValuePair<string,Sprite>>();

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

                string PlatformPathname = SS14.Shared.Utility.PlatformTools.SanePath(fullPath[0]);

                string originalName = Path.GetFileNameWithoutExtension(PlatformPathname).ToLowerInvariant();
                //The name of the original picture without extension, before it became part of the atlas.
                //This will be the name we can find this under in our Resource lists.

                string[] splitResourceName = fullPath[2].Split('.');

                string imageName = splitResourceName[0].ToLowerInvariant();

                if (!TextureCache.Textures.ContainsKey(imageName))
                    continue; //Image for this sprite does not exist. Possibly set to defered later.

                Texture atlasTex = TextureCache.Textures[imageName].Item1;
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

                info.Offsets = new Vector2f((float) Math.Round(offsetX*atlasTex.Size.X, 1),
                    (float) Math.Round(offsetY*atlasTex.Size.Y, 1));
                info.Size = new Vector2f((float) Math.Round(sizeX*atlasTex.Size.X, 1),
                    (float) Math.Round(sizeY*atlasTex.Size.Y, 1));

                if (!_spriteInfos.ContainsKey(originalName)) _spriteInfos.Add(originalName, info);

                loadedSprites.Add(new KeyValuePair<string, Sprite>(originalName,
                    new Sprite(atlasTex, new IntRect((int)info.Offsets.X, (int)info.Offsets.Y, (int)info.Size.X, (int)info.Size.Y))));

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
            if (_textures.ContainsKey(key))
            {
                if (_sprites.ContainsKey(key))
                {
                    return _sprites[key];
                }
                else
                {
                    var newSprite = new Sprite(_textures[key]);
                    _sprites.Add(key, newSprite);
                    return newSprite;
                }
            }
            return GetNoSprite();
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

        public object GetAnimatedSprite(string key)
        {
            key = key.ToLowerInvariant();
            if (_animationCollections.ContainsKey(key))
            {
                return new AnimatedSprite(key, _animationCollections[key], this);
            }
            return GetNoSprite();
        }

        public Sprite GetNoSprite()
        {
            return _sprites["nosprite"];
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
        public bool TextureExists(string key)
        {
            key = key.ToLowerInvariant();
            return _textures.ContainsKey(key);
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
        public GLSLShader GetShader(string key)
        {
            key = key.ToLowerInvariant();
            if (_shaders.ContainsKey(key)) return _shaders[key];
            else return null;
        }

        /// <summary>
        ///  Retrieves the Technique List with the given key from the Resource List. Returns null if not found.
        /// </summary>
        public TechniqueList GetTechnique(string key)
        {
            if (_TechniqueList.ContainsKey(key)) return _TechniqueList[key];
            else return null;

        }

        /// <summary>
        ///  Retrieves the ParticleSettings with the given key from the Resource List. Returns null if not found.
        /// </summary>
        public ParticleSettings GetParticles(string key)
        {
            key = key.ToLowerInvariant();
            if (_particles.ContainsKey(key)) return _particles[key];
            else return null;
        }

        /// <summary>
        ///  Retrieves the Texture with the given key from the Resource List. Returns error Image if not found.
        /// </summary>
        public Texture GetTexture(string key)
        {
            //key = key.ToLowerInvariant(); TODO
            if (_textures.ContainsKey(key)) return _textures[key];
            else return _textures["nosprite"];
        }

        /// <summary>
        ///  Retrieves the Font with the given key from the Resource List. Returns base_font if not found.
        /// </summary>
        public Font GetFont(string key)
        {
            key = key.ToLowerInvariant();
            if (_fonts.ContainsKey(key)) return _fonts[key];
            else return _fonts["base_font"];
        }

        #endregion
    }
}
