using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SS14.Client.Graphics;
using SS14.Client.Graphics.ClientEye;
using SS14.Client.Interfaces.GameObjects;
using SS14.Client.Interfaces.GameObjects.Components;
using SS14.Client.Interfaces.ResourceManagement;
using SS14.Client.ResourceManagement;
using SS14.Client.Utility;
using SS14.Shared;
using SS14.Shared.Enums;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using SS14.Shared.Map;
using SS14.Shared.Maths;
using SS14.Shared.Utility;
using YamlDotNet.RepresentationModel;

// Warning: Shitcode ahead!
namespace SS14.Client.GameObjects
{
    public class SpriteComponent : Component, ISpriteRenderableComponent, ISpriteComponent, IClickTargetComponent
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
                    SceneSprite.ZIndex = (int)value;
                }
            }
        }
        private Color color = Color.White;
        public Color Color
        {
            get => color;
            set
            {
                color = value;
                if (SceneSprite != null)
                {
                    SceneSprite.SelfModulate = value.Convert();
                }
            }
        }
        public MapId MapID { get; private set; }

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

        public bool Cardinal { get; private set; } = true;

        public Vector2 Scale { get; private set; } = Vector2.One;

        private Vector2 offset = Vector2.Zero;
        public Vector2 Offset
        {
            get => offset;
            set
            {
                if (value == offset)
                {
                    return;
                }

                SceneSprite.Offset = value.Convert() * EyeManager.PIXELSPERMETER;
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
            transform.OnRotate += OnRotate;
            MapID = transform.MapID;

            SceneSprite = new Godot.Sprite()
            {
                ZIndex = (int)DrawDepth,
                SelfModulate = Color.Convert(),
                Scale = Scale.Convert(),
            };
            SceneSprite.SetName("SpriteComponent");

            transform.SceneNode.AddChild(SceneSprite);

            UpdateCurrentSprite();
        }

        public override void Shutdown()
        {
            transform.OnRotate -= OnRotate;
            transform.OnMove -= OnMove;
            transform = null;

            SceneSprite.QueueFree();
            SceneSprite.Dispose();
            SceneSprite = null;

            base.Shutdown();
        }

        public void OnMove(object sender, MoveEventArgs args)
        {
            MapID = args.NewPosition.MapID;
        }

        private void OnRotate(Angle newAngle)
        {
            UpdateCurrentSprite();
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
            Direction dir;
            // Oh hey look Y is STILL FLIPPED.
            var angle = new Angle(-transform.Rotation);
            if (Cardinal)
            {
                dir = angle.GetCardinalDir();
            }
            else
            {
                dir = angle.GetDir();
            }
            string name = $"{withoutExt}_{dir.ToString().ToLowerInvariant()}{ext}";

            if (dirSprites.ContainsKey(name))
                sprite = dirSprites[name];

            return sprite;
        }

        public void UpdateCurrentSprite()
        {
            CurrentSprite = GetActiveDirectionalSprite();
            if (SceneSprite != null && CurrentSprite != null)
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
                Color = node.AsColor();
            }

            if (mapping.TryGetNode<YamlSequenceNode>("sprites", out var sequence))
            {
                foreach (YamlNode spriteNode in sequence)
                {
                    AddSprite(spriteNode.AsString());
                }
            }

            if (mapping.TryGetNode("sprite", out node))
            {
                AddSprite(node.AsString());
            }

            if (mapping.TryGetNode("cardinal", out node))
            {
                Cardinal = node.AsBool();
            }

            if (mapping.TryGetNode("scale", out node))
            {
                Scale = node.AsVector2();
            }
        }

        /// <inheritdoc />
        public override void HandleComponentState(ComponentState state)
        {
            var newState = (SpriteComponentState)state;
            DrawDepth = newState.DrawDepth;
            Offset = newState.Offset;

            if (newState.SpriteKey != null && sprites.ContainsKey(newState.SpriteKey) &&
                currentBaseSprite != sprites[newState.SpriteKey])
                SetSpriteByKey(newState.SpriteKey);

            visible = newState.Visible;
        }
    }
}
