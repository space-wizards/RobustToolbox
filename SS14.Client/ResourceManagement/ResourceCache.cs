using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using SFML.Graphics;
using SFML.System;
using SS14.Client.Graphics.Collection;
using SS14.Client.Graphics.Shader;
using SS14.Client.Graphics.Sprite;
using SS14.Client.Interfaces.Resource;
using SS14.Shared.IoC;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.Configuration;
using SS14.Shared.Log;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SS14.Client.Graphics.TexHelpers;
using SS14.Client.ResourceManagement;
using SS14.Shared.Configuration;
using SS14.Shared.ContentPack;
using SS14.Shared.Interfaces;

namespace SS14.Client.Resources
{
    public class ResourceCache : IResourceCache, IDisposable
    {
        [Dependency]
        private readonly IConfigurationManager _config;

        [Dependency]
        private readonly IResourceManager _resources;

        private readonly Dictionary<Type, Dictionary<string, BaseResource>> _cachedObjects = new Dictionary<Type, Dictionary<string, BaseResource>>();
        
        #region OldCode

        private const int zipBufferSize = 4096;
        private MemoryStream VertexShader, FragmentShader;
        private readonly Dictionary<string, ParticleSettings> _particles = new Dictionary<string, ParticleSettings>();
        private readonly Dictionary<string, Texture> _textures = new Dictionary<string, Texture>();
        private readonly Dictionary<string, GLSLShader> _shaders = new Dictionary<string, GLSLShader>();
        private readonly Dictionary<string, TechniqueList> _techniqueList = new Dictionary<string, TechniqueList>();
        private readonly Dictionary<string, SpriteInfo> _spriteInfos = new Dictionary<string, SpriteInfo>();
        private readonly Dictionary<string, Sprite> _sprites = new Dictionary<string, Sprite>();
        private readonly Dictionary<string, AnimationCollection> _animationCollections = new Dictionary<string, AnimationCollection>();
        private readonly Dictionary<string, AnimatedSprite> _animatedSprites = new Dictionary<string, AnimatedSprite>();
        private readonly List<string> _supportedImageExtensions = new List<string> { ".png" };

        private readonly Dictionary<Texture, string> _textureToKey = new Dictionary<Texture, string>();
        public Dictionary<Texture, string> TextureToKey => _textureToKey;

        #region Resource Loading & Disposal

        /// <summary>
        ///  <para>Loads the embedded base files.</para>
        /// </summary>
        public void LoadBaseResources()
        {
            _resources.Initialize();

            _resources.MountContentDirectory("");

            _resources.MountContentPack(@"../../Resources/EngineContentPack.zip");

            _resources.MountDefaultContentPack();
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
            var cfgMgr = _config;

            cfgMgr.RegisterCVar("res.pack", Path.Combine("..","..","Resources","ResourcePack.zip"), CVarFlags.ARCHIVE);
            cfgMgr.RegisterCVar("res.password", String.Empty, CVarFlags.SERVER | CVarFlags.REPLICATED);

            string zipPath = path ?? _config.GetCVar<string>("res.pack");
            string password = pw ?? _config.GetCVar<string>("res.password");

            if (AppDomain.CurrentDomain.GetAssemblyByName("SS14.UnitTesting") != null)
            {
                string debugPath = "..";
                zipPath = Path.Combine(debugPath, zipPath);
            }

            zipPath = PathHelpers.ExecutableRelativeFile(zipPath);

            if (!File.Exists(zipPath))
                throw new FileNotFoundException("Specified Zip does not exist: " + zipPath);

            FileStream zipFileStream = File.OpenRead(zipPath);
            var zipFile = new ZipFile(zipFileStream);

            if (!string.IsNullOrWhiteSpace(password)) zipFile.Password = password;

#region Sort Resource pack
            var directories = zipFile.Cast<ZipEntry>()
                .Where(a => a.IsDirectory)
                .OrderByDescending(a => a.Name.ToLowerInvariant() == "textures");

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
#endregion Sort Resource pack

            Logger.Log("Loading resources...");

#region Load Resources
            foreach (KeyValuePair<string, List<ZipEntry>> current in sorted)
            {
                switch (current.Key)
                {
                    case ("textures/"):

                        int itemCount = current.Value.Count();
                        Task<Texture>[] taskArray = new Task<Texture>[itemCount];
                        for (int i = 0; i < itemCount; i++)
                        {
                            ZipEntry texture = current.Value[i];

                            if (_supportedImageExtensions.Contains(Path.GetExtension(texture.Name).ToLowerInvariant()))
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

                    case ("tai/"): // Tai? HANK HANK
                        Logger.Log("Loading tai...");
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
#if _DELME
                    case ("fonts/"):
                        Logger.Log("Loading fonts...");
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
#endif
                    case ("particlesystems/"):
                        Logger.Log("Loading particlesystems...");
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
                            Logger.Log("Loading shaders...");
                            GLSLShader LoadedShader;
                            TechniqueList List;

                            foreach (ZipEntry shader in current.Value)
                            {
                                int FirstIndex = shader.Name.IndexOf('/');
                                int LastIndex = shader.Name.LastIndexOf('/');

                                if (FirstIndex != LastIndex)  // if the shader pixel/fragment files are in folder/technique group, construct shader and add it to a technique list.
                                {
                                    string FolderName = shader.Name.Substring(FirstIndex + 1, LastIndex - FirstIndex - 1);

                                    if (!_techniqueList.Keys.Contains(FolderName))
                                    {
                                        List = new TechniqueList();
                                        List.Name = FolderName;
                                        _techniqueList.Add(FolderName, List);
                                    }

                                    LoadedShader = LoadShaderFrom(zipFile, shader);
                                    if (LoadedShader == null) continue;
                                    else _techniqueList[FolderName].Add(LoadedShader);
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

                    case ("animations/"):
                        Logger.Log("Loading animations...");
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
#endregion Load Resources

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
            _techniqueList.Clear();
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
                return TextureCache.Textures[ResourceName].Texture;

            var byteBuffer = new byte[zipBufferSize];

            try
            {
                Stream zipStream = zipFile.GetInputStream(imageEntry);
                var memStream = new MemoryStream();

                StreamUtils.Copy(zipStream, memStream, byteBuffer);
                memStream.Position = 0;

                Image img = new Image(memStream);
                bool[,] opacityMap = new bool[img.Size.X, img.Size.Y];
                for (int y = 0; y < img.Size.Y; y++)
                {
                    for (int x = 0; x < img.Size.X; x++)
                    {
                        Color pColor = img.GetPixel(Convert.ToUInt32(x), Convert.ToUInt32(y));
                        if (pColor.A > Limits.ClickthroughLimit)
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
                TextureInfo tmp = new TextureInfo(loadedImg, img, opacityMap);
                TextureCache.Add(ResourceName, tmp);
                _textureToKey.Add(TextureCache.Textures[ResourceName].Texture, ResourceName);

                memStream.Close();
                zipStream.Close();
                memStream.Dispose();
                zipStream.Dispose();
                return loadedImg;
            }
            catch (Exception I)
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
            Stream zipStream = zipFile.GetInputStream(entry);
            //Will throw exception is missing or wrong password. TODO: Handle this.

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
            Stream zipStream = zipFile.GetInputStream(entry);
            //Will throw exception is missing or wrong password. TODO: Handle this.

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
            var loadedSprites = new List<KeyValuePair<string, Sprite>>();

            var byteBuffer = new byte[zipBufferSize];

            Stream zipStream = zipFile.GetInputStream(taiEntry);
            //Will throw exception is missing or wrong password. TODO: Handle this.

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

                Texture atlasTex = TextureCache.Textures[imageName].Texture;
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

                info.Offsets = new Vector2f((float)Math.Round(offsetX * atlasTex.Size.X, 1),
                    (float)Math.Round(offsetY * atlasTex.Size.Y, 1));
                info.Size = new Vector2f((float)Math.Round(sizeX * atlasTex.Size.X, 1),
                    (float)Math.Round(sizeY * atlasTex.Size.Y, 1));

                if (!_spriteInfos.ContainsKey(originalName)) _spriteInfos.Add(originalName, info);

                loadedSprites.Add(new KeyValuePair<string, Sprite>(originalName,
                    new Sprite(atlasTex, new IntRect((int)info.Offsets.X, (int)info.Offsets.Y, (int)info.Size.X, (int)info.Size.Y))));
            }

            return loadedSprites;
        }
#endregion Resource Loading & Disposal

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
            return DefaultSprite();
        }

        /// <summary>
        ///  Retrieves the Sprite with the given key from the Resource List. Returns error Sprite if not found.
        /// </summary>
        [Obsolete]
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

        public object GetAnimatedSprite(string key)
        {
            key = key.ToLowerInvariant();
            if (_animationCollections.ContainsKey(key))
            {
                return new AnimatedSprite(key, _animationCollections[key], this);
            }
            return DefaultSprite();
        }

        public Sprite DefaultSprite()
        {
            return GetResource<SpriteResource>("Textures/noSprite.png").Sprite;
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
            if (_techniqueList.ContainsKey(key)) return _techniqueList[key];
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


        #endregion Resource Retrieval

        #endregion OldCode

        /// <summary>
        /// fetches the resource from the cache.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="path"></param>
        /// <returns></returns>
        public T GetResource<T>(string path)
            where T : BaseResource, new()
        {
            if (TryGetResource(path, out T resource))
            {
                return resource;
            }

            throw new FileNotFoundException($"The file {path} of type {typeof(T)} cannot be found.");
        }

        /// <summary>
        /// Tries to fetch the resource from the cache. On a cache miss the resource will try to be fetched from the VFS.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="path"></param>
        /// <param name="resource"></param>
        /// <returns></returns>
        public bool TryGetResource<T>(string path, out T resource) 
            where T : BaseResource, new()
        {
            // make sure path map exists for this type...
            if (!_cachedObjects.TryGetValue(typeof(T), out Dictionary<string, BaseResource> pathMap))
            {
                pathMap = new Dictionary<string, BaseResource>();
                _cachedObjects.Add(typeof(T), pathMap);
            }

            // found it!
            if (pathMap.TryGetValue(path, out BaseResource obj))
            {
                resource = (T) obj;
                return true;
            }

            // cache miss, lets try to find it in the VFS
            if (_resources.TryContentFileRead(path, out MemoryStream stream))
            {
                resource = new T();
                resource.Load(this, path, stream);
                CacheResource(path, resource);
                return true;
            }

            // ok, can't find it, lets try the fallback
            var tempRes = new T();
            
            // stop this, we failed
            if (tempRes.Fallback != null && tempRes.Fallback != path)
            {
                TryGetResource(tempRes.Fallback, out T fallbackRes);
                resource = fallbackRes;
                return true;
            }

            resource = default(T);
            return false;
        }
        
        /// <summary>
        /// Is the resource cached? Will not cache the resource if false.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public bool ResourceCached<T>(string path)
        {
            if(_cachedObjects.TryGetValue(typeof(T), out Dictionary<string, BaseResource> typeMap))
                return typeMap.ContainsKey(path);
            return false;
        }

        /// <summary>
        /// Pre-caches a resource, without returning it.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="path"></param>
        /// <returns></returns>
        public bool PreCacheResource<T>(string path)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Adds a resource to the cache.
        /// </summary>
        /// <param name="resource"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public void CacheResource<T>(string path, T resource)
            where T : BaseResource, new()
        {
            if (!_cachedObjects.TryGetValue(typeof(T), out Dictionary<string, BaseResource> typeMap))
            {
                typeMap = new Dictionary<string, BaseResource>();
                _cachedObjects.Add(typeof(T), typeMap);
            }

            if (typeMap.TryGetValue(path, out BaseResource res))
            {
                typeMap.Remove(path);
                res.Dispose();
            }

            typeMap.Add(path, resource);
        }

        /// <summary>
        /// Disposes all cached resources.
        /// </summary>
        public void Dispose()
        {
            foreach (var kvTypeMap in _cachedObjects)
            {
                foreach (var kvResource in kvTypeMap.Value)
                {
                    kvResource.Value.Dispose();
                }
            }
        }
    }
}
