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

using System.Text.RegularExpressions;

using SS3D.Modules;

namespace SS3D
{
    struct SpriteInfo
    {
        public string name;
        public Vector2D Offsets;
        public Vector2D Size;
    }

    /// <summary>
    ///  This class manages Resource loading, caching and distribution.
    /// </summary>
    class ResMgr
    {
        private FileSystem FileSystem;
        private FileSystem GuiFileSystem;

        private Dictionary<string, Image> Images = new Dictionary<string, Image>();

        private Dictionary<string, FXShader> Shaders = new Dictionary<string, FXShader>();

        private Dictionary<string, SpriteInfo> SpriteInfos = new Dictionary<string, SpriteInfo>();
        private Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>();

        private Dictionary<string, GUISkin> GuiSkins = new Dictionary<string, GUISkin>();

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

        /// <summary>
        ///  Retrieves the GUI Skin with the given key from the Resource list. Returns null if not found.
        /// </summary>
        public GUISkin GetGuiSkin(string key)
        {
            if (GuiSkins.ContainsKey(key)) return GuiSkins[key];
            else return null;
        }

        /// <summary>
        ///  <para>Retrieves the Image with the given key from the Resource list and returns it as a Sprite.</para>
        ///  <para>If a sprite has been created before using this method, it will return that Sprite. Returns error Sprite if not found.</para>
        /// </summary>
        public Sprite GetSpriteFromImage(string key)
        {
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
            else return new Sprite(key + "Missing", Images["noSprite"]);
        }

        /// <summary>
        ///  Retrieves the Sprite with the given key from the Resource list. Returns error Sprite if not found.
        /// </summary>
        public Sprite GetSprite(string key)
        {
            if (Sprites.ContainsKey(key)) return Sprites[key];
            else return new Sprite(key + "Missing", Images["noSprite"]);
        }

        /// <summary>
        ///  Retrieves the SpriteInfo with the given key from the Resource list. Returns null if not found.
        /// </summary>
        public SpriteInfo? GetSpriteInfo(string key)
        {
            if (SpriteInfos.ContainsKey(key)) return SpriteInfos[key];
            else return null;
        }

        /// <summary>
        ///  Retrieves the Shader with the given key from the Resource list. Returns null if not found.
        /// </summary>
        public FXShader GetShader(string key)
        {
            if (Shaders.ContainsKey(key)) return Shaders[key];
            else return null;
        }

        /// <summary>
        ///  Retrieves the Image with the given key from the Resource list. Returns error Image if not found.
        /// </summary>
        public Image GetImage(string key)
        {
            if (Images.ContainsKey(key)) return Images[key];
            else return Images["noSprite"];
        }

        /// <summary>
        ///  Initializes the Resource Manager and caches all resources.
        /// </summary>
        public void Initialize()
        {
            FileSystemProvider.Load(@".\GorgonBZip2FileSystem.dll");

            FileSystem = FileSystem.Create("ResourceFS", FileSystemProviderCache.Providers["Gorgon.BZip2FileSystem"]);
            GuiFileSystem = FileSystem.Create("GuiFS", FileSystemProviderCache.Providers["Gorgon.BZip2FileSystem"]);

            if (!File.Exists(ConfigManager.Singleton.Configuration.ResourcePack)) throw new FileNotFoundException("Resource Pack not found : " + ConfigManager.Singleton.Configuration.ResourcePack);

            FileSystem.AssignRoot(ConfigManager.Singleton.Configuration.ResourcePack);

            LoadFiles();
            LoadGUIs();
        }

        private void LoadGUIs()
        {
            if (!Directory.Exists(ConfigManager.Singleton.Configuration.GuiFolder)) throw new FileNotFoundException("GUI Folder missing : " + ConfigManager.Singleton.Configuration.GuiFolder);

            string[] guiFiles = Directory.GetFiles(ConfigManager.Singleton.Configuration.GuiFolder, "*.gui", SearchOption.TopDirectoryOnly);

            foreach (string currentGuiFile in guiFiles)
            {
                string guiName = Path.GetFileNameWithoutExtension(currentGuiFile);
                if (GuiSkins.ContainsKey(guiName)) continue; //Already exists. This shouldnt even be possible. Would require 2+ Files with the same name in the same dir.
                GuiFileSystem.AssignRoot(currentGuiFile);    //Or someone loading this twice...
                GUISkin newSkin = GUISkin.FromFile(GuiFileSystem);
                GuiSkins.Add(guiName, newSkin);
            }

        }

        private void LoadFiles()
        {

            var ImageQuery = from FileSystemFile file in FileSystem where file.Extension.ToLower() == ".png" select file;
            foreach (FileSystemFile file in ImageQuery)
            {
                Image loadedImg;

                if (ImageCache.Images.Contains(file.Filename))
                    loadedImg = ImageCache.Images[file.Filename];
                else
                    loadedImg = Image.FromFileSystem(FileSystem, file.FullPath);

                if (!Images.ContainsKey(file.Filename)) Images.Add(file.Filename, loadedImg);
            }

            #region TAI Loading / Parsing
            //Structure of a TAI file line for reference.
            //<filename>\t\t<atlas filename>, <atlas index>, <atlas type>, <x offset>, <y offset>, <depth offset>, <width>, <height>
            //            0                 ,       1      ,       2     ,      3    ,      4    ,        5      ,    6   ,    7 

            var TAIQuery = from FileSystemFile file in FileSystem where file.Extension.ToLower() == ".tai" select file;
            foreach (FileSystemFile file in TAIQuery)
            {
                string taiContents = Encoding.UTF8.GetString(FileSystem.ReadFile(file.FullPath));
                string[] splitContents = Regex.Split(taiContents, "\r\n"); //Split by newlines.

                foreach (string line in splitContents)
                {
                    if (String.IsNullOrWhiteSpace(line)) continue; //There seems to be an empty line at the end of these files.
                    string[] splitLine = line.Split(','); //Split line by commas.
                    string[] fullPath = Regex.Split(splitLine[0], "\t");
                    //Split first part by tabs - its the original filepath & filename , two tabs and then the name of the atlas texture.
                    //So we end up with fullPath[0] being the original path and fullPath[2] being the name of the atlas texture. fullPath[1] is an empty string between the 2 tabs.

                    string originalName = Path.GetFileNameWithoutExtension(fullPath[0]);
                    //The name of the original picture without extension, before it became part of the atlas. 
                    //This will be the name we can find this under in our Resource lists.

                    string[] splitResourceName = fullPath[2].Split('.');

                    if (!ImageCache.Images.Contains(splitResourceName[0]))
                    { 
                        //Image for this sprite does not exist. Possibly set to defered later.
                        continue;
                    }

                    Image atlasTex = ImageCache.Images[splitResourceName[0]]; //Grab the image for the sprite from the cache.

                    SpriteInfo info = new SpriteInfo();
                    info.name = originalName;

                    float offsetX = 0;
                    float offsetY = 0;
                    float sizeX = 0;
                    float sizeY = 0;

                    if (splitLine.Length > 8) //Separated with , - This causes some problems. Happens on some systems.
                    {
                        offsetX = float.Parse(splitLine[3] + "." + splitLine[4], CultureInfo.InvariantCulture);
                        offsetY = float.Parse(splitLine[5] + "." + splitLine[6], CultureInfo.InvariantCulture);
                        sizeX = float.Parse(splitLine[8] + "." + splitLine[9], CultureInfo.InvariantCulture);
                        sizeY = float.Parse(splitLine[10] + "." + splitLine[11], CultureInfo.InvariantCulture);
                    }
                    else //Seperated with SOMETHING ELSE. This is good.
                    {
                        offsetX = float.Parse(splitLine[3], CultureInfo.InvariantCulture);
                        offsetY = float.Parse(splitLine[4], CultureInfo.InvariantCulture);
                        sizeX = float.Parse(splitLine[6], CultureInfo.InvariantCulture);
                        sizeY = float.Parse(splitLine[7], CultureInfo.InvariantCulture);
                    }

                    info.Offsets = new Vector2D((float)Math.Round(offsetX * (float)atlasTex.Width,1), (float)Math.Round(offsetY * (float)atlasTex.Height,1));
                    info.Size = new Vector2D((float)Math.Round(sizeX * (float)atlasTex.Width,1), (float)Math.Round(sizeY * (float)atlasTex.Height,1));

                    Sprite newSprite = new Sprite(originalName, atlasTex, info.Offsets, info.Size);

                    if (!Sprites.ContainsKey(originalName)) Sprites.Add(originalName, newSprite);
                    if (!SpriteInfos.ContainsKey(originalName)) SpriteInfos.Add(originalName, info);
                }

            }
            #endregion

            var ShaderQuery = from FileSystemFile file in FileSystem where file.Extension.ToLower() == ".fx" select file;
            foreach (FileSystemFile file in ShaderQuery)
            {
                try
                {
                    FXShader loadedShader;

                    if (ShaderCache.Shaders.Contains(file.Filename))
                        continue;
                    else
                        loadedShader = FXShader.FromFileSystem(FileSystem, file.FullPath, ShaderCompileOptions.None);

                    if (!Shaders.ContainsKey(file.Filename)) Shaders.Add(file.Filename, loadedShader);
                }
                catch (Exception EX)
                {
                    MessageBox.Show(EX.Message,EX.TargetSite.ToString());
                }
            }
        }

    }
}
