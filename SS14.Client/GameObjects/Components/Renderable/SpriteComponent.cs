using Lidgren.Network;
using OpenTK;
using SS14.Client.Graphics;
using SS14.Client.Interfaces.GameObjects;
using SS14.Client.Interfaces.GameObjects.Components;
using SS14.Client.Interfaces.ResourceManagement;
using SS14.Client.ResourceManagement;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.IoC;
using SS14.Shared.Map;
using SS14.Shared.Maths;
using SS14.Shared.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YamlDotNet.RepresentationModel;
using Vector2 = SS14.Shared.Maths.Vector2;
using Vector2i = SS14.Shared.Maths.Vector2i;

// Warning: Shitcode ahead!
namespace SS14.Client.GameObjects
{
    public class SpriteComponent : Component, ISpriteRenderableComponent, ISpriteComponent
    {
        public override string Name => "Sprite";
        public override uint? NetID => NetIDs.SPRITE;

        public TextureSource CurrentSprite { get; private set; }
        protected TextureSource currentBaseSprite { get; private set; }
        protected string currentBaseSpriteKey;
        protected Dictionary<string, TextureSource> dirSprites = new Dictionary<string, TextureSource>();
        protected bool HorizontalFlip { get; set; }
        protected IRenderableComponent master;
        protected List<IRenderableComponent> slaves = new List<IRenderableComponent>();
        protected Dictionary<string, TextureSource> sprites = new Dictionary<string, TextureSource>();
        protected bool visible = true;
        private DrawDepth drawDepth;
        public DrawDepth DrawDepth
        {
            get => drawDepth;
            set
            {
                drawDepth = value;
                if (SceneSprite != null)
                {
                    SceneSprite.Z = (int)value;
                }
            }
        }
        public Color Color { get; set; } = Color.White;
        public int MapID { get; private set; }

        public override Type StateType => typeof(SpriteComponentState);

        private IClientTransformComponent transform;
        private Godot.Sprite SceneSprite;

        public float Bottom
        {
            get
            {
                return transform.WorldPosition.Y +
                       (CurrentSprite.Height / 2);
            }
        }

        #region ISpriteComponent Members

        public Box2 AverageAABB => LocalAABB;

        public Box2 LocalAABB
        {
            get
            {
                return Box2.FromDimensions(0, 0, CurrentSprite.Width, CurrentSprite.Height);
            }
        }

        public TextureSource GetSprite(string spriteKey)
        {
            if (sprites.ContainsKey(spriteKey))
                return sprites[spriteKey];
            else
                return null;
        }

        public List<TextureSource> GetAllSprites()
        {
            return sprites.Values.ToList();
        }

        public void SetSpriteByKey(string spriteKey)
        {
            if (sprites.ContainsKey(spriteKey))
            {
                currentBaseSprite = sprites[spriteKey];
                currentBaseSpriteKey = spriteKey;
                if (Owner != null)
                    Owner.SendMessage(this, ComponentMessageType.SpriteChanged);
            }
            else
                throw new Exception("Whoops. That sprite isn't in the dictionary.");

            UpdateCurrentSprite();
        }

        public void AddSprite(string spriteKey)
        {
            if (sprites.ContainsKey(spriteKey))
            {
                throw new InvalidOperationException("That sprite is already added.");
            }

            var manager = IoCManager.Resolve<IResourceCache>();
            if (manager.TryGetResource<TextureResource>($@"./Textures/{spriteKey}", out var sprite))
            {
                AddSprite(spriteKey, sprite.Texture);
            }

            //If there's only one sprite, and the current sprite is explicitly not set, then lets go ahead and set our sprite.
            if (sprites.Count == 1)
            {
                SetSpriteByKey(sprites.Keys.First());
            }

            BuildDirectionalSprites();
        }

        public void AddSprite(string key, TextureSource spritetoadd)
        {
            if (spritetoadd != null && !String.IsNullOrEmpty(key))
            {
                sprites.Add(key, spritetoadd);
            }
            BuildDirectionalSprites();
        }

        public bool HasSprite(string key)
        {
            return sprites.ContainsKey(key);
        }

        #endregion ISpriteComponent Members

        private void BuildDirectionalSprites()
        {
            dirSprites.Clear();
            var resMgr = IoCManager.Resolve<IResourceCache>();

            foreach (var curr in sprites)
            {
                foreach (string dir in Enum.GetNames(typeof(Direction)))
                {
                    var ext = Path.GetExtension(curr.Key);
                    var withoutExt = Path.ChangeExtension(curr.Key, null);
                    string name = $"{withoutExt}_{dir.ToLowerInvariant()}{ext}";
                    if (resMgr.TryGetResource<TextureResource>(@"./Textures/" + name, out var res))
                        dirSprites.Add(name, res.Texture);
                }
            }

            UpdateCurrentSprite();
        }

        public override void OnAdd(IEntity owner)
        {
            base.OnAdd(owner);
            //Send a spritechanged message so everything knows whassup.
            Owner.SendMessage(this, ComponentMessageType.SpriteChanged);
        }

        public override void Initialize()
        {
            base.Initialize();
            transform = Owner.GetComponent<IClientTransformComponent>();
            transform.OnMove += OnMove;
            MapID = transform.MapID;

            SceneSprite = new Godot.Sprite();
            SceneSprite.SetName("SpriteComponent");
            SceneSprite.Z = (int)DrawDepth;
            transform.SceneNode.AddChild(SceneSprite);

            UpdateCurrentSprite();
        }

        public override void Shutdown()
        {
            transform.OnMove -= OnMove;
            transform = null;
            base.Shutdown();
        }

        public void OnMove(object sender, MoveEventArgs args)
        {
            MapID = args.NewPosition.MapID;
        }

        public void ClearSprites()
        {
            sprites.Clear();
        }

        protected virtual TextureSource GetBaseSprite()
        {
            return currentBaseSprite;
        }

        protected void SetDrawDepth(DrawDepth p)
        {
            DrawDepth = p;
        }

        private TextureSource GetActiveDirectionalSprite()
        {
            if (currentBaseSprite == null || transform == null) return null;

            var sprite = currentBaseSprite;

            var ext = Path.GetExtension(currentBaseSpriteKey);
            var withoutExt = Path.ChangeExtension(currentBaseSpriteKey, null);
            string name = $"{withoutExt}_{transform.Rotation.GetDir().ToString().ToLowerInvariant()}{ext}";

            if (dirSprites.ContainsKey(name))
                sprite = dirSprites[name];

            return sprite;
        }

        public void UpdateCurrentSprite()
        {
            CurrentSprite = GetActiveDirectionalSprite();
            if (SceneSprite != null)
            {
                SceneSprite.Texture = CurrentSprite.Texture;
            }
        }

        public bool SpriteExists(string key)
        {
            if (sprites.ContainsKey(key))
                return true;
            return false;
        }

        public override void LoadParameters(YamlMappingNode mapping)
        {
            YamlNode node;
            if (mapping.TryGetNode("drawdepth", out node))
            {
                SetDrawDepth(node.AsEnum<DrawDepth>());
            }

            if (mapping.TryGetNode("color", out node))
            {
                try
                {
                    Color = System.Drawing.Color.FromName(node.ToString());
                }
                catch
                {
                    Color = node.AsHexColor();
                }
            }

            if (mapping.TryGetNode<YamlSequenceNode>("sprites", out var sequence))
            {
                foreach (YamlNode spriteNode in sequence)
                {
                    AddSprite(spriteNode.AsString());
                }
            }
        }

        /// <inheritdoc />
        public override void HandleComponentState(ComponentState state)
        {
            var newState = (SpriteComponentState)state;
            DrawDepth = newState.DrawDepth;

            if (newState.SpriteKey != null && sprites.ContainsKey(newState.SpriteKey) &&
                currentBaseSprite != sprites[newState.SpriteKey])
                SetSpriteByKey(newState.SpriteKey);

            visible = newState.Visible;
        }
    }
}
