using OpenTK;
using SS14.Client.Graphics.Collection;
using SS14.Client.Graphics.States;
using SS14.Client.Graphics.Utility;
using SS14.Client.Interfaces.Resource;
using SS14.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using SS14.Shared.Maths;
using OpenTK.Graphics;

namespace SS14.Client.Graphics.Sprites
{
    public class AnimatedSprite
    {
        #region Public Properties

        public string Name { get; set; }
        public Dictionary<string, AnimationState> AnimationStates { get; private set; }
        private AnimationState _currentAnimationState;

        public string CurrentAnimationStateKey
        {
            get
            {
                if (_currentAnimationState != null)
                    return _currentAnimationState.Name;
                return null;
            }
        }

        public string BaseName { get; set; }

        public Direction Direction
        {
            get { return _direction; }
            set
            {
                if (_direction != value)
                {
                    _direction = value;
                    UpdateDirection();
                }
            }
        }

        public Box2 AverageAABB
        {
            get
            {
                if (_currentSprite != null)
                    return _averageAABBs[_currentAnimationState.Name][Direction];
                return new Box2();
            }
        }

        #region Sprite passthrough methods

        /// <summary>
        ///     Sub-rectangle of the texture to use as the sprite. This is NOT the local bounds of the sprite.
        /// </summary>
        public Box2i TextureRect => _currentSprite?.TextureRect.Convert() ?? new Box2i();

        /// <summary>
        ///     Local bounding box of the sprite, with the origin at the top left.
        /// </summary>
        public Box2 LocalAABB => _currentSprite?.GetLocalBounds().Convert() ?? new Box2();

        public bool HorizontalFlip { get; set; }

        #endregion Sprite passthrough methods

        #endregion Public Properties

        #region Variables

        /// <summary>
        /// Dictionary of animation names to directional sprite set
        /// </summary>
        private readonly Dictionary<string, Dictionary<Direction, SFML.Graphics.Sprite[]>> _sprites = new Dictionary<string, Dictionary<Direction, SFML.Graphics.Sprite[]>>();

        private readonly Dictionary<string, Dictionary<Direction, Box2>> _averageAABBs = new Dictionary<string, Dictionary<Direction, Box2>>();
        private SFML.Graphics.Sprite _currentSprite;

        private Direction _direction = Direction.South;

        #endregion Variables

        #region Methods

        //Todo encapsulate this further down as components -- AnimatedSpriteState, AnimatedSpriteStateDirection
        public void LoadSprites(AnimationCollection collection, IResourceCache resourceCache)
        {
            float x = 0, y = 0, h = 0, w = 0;
            int t = 0;
            foreach (var info in collection.Animations)
            {
                _sprites.Add(info.Name, new Dictionary<Direction, SFML.Graphics.Sprite[]>());

                //Because we have a shitload of frames, we're going to store the average size as the AABB for each direction and each animation
                _averageAABBs.Add(info.Name, new Dictionary<Direction, Box2>());

                var sprites = _sprites[info.Name];
                var averageAABBs = _averageAABBs[info.Name];
                AnimationStates.Add(info.Name, new AnimationState(info));
                foreach (var dir in Enum.GetValues(typeof(Direction)).Cast<Direction>())
                {
                    sprites.Add(dir, new SFML.Graphics.Sprite[info.Frames]);
                    var thisDirSprites = sprites[dir];
                    for (var i = 0; i < info.Frames; i++)
                    {
                        var spritename = collection.Name.ToLowerInvariant() + "_" + info.Name.ToLowerInvariant() + "_"
                                         + DirectionToUriComponent(dir) + "_" + i;
                        thisDirSprites[i] = resourceCache.GetSprite(spritename);
                        var bounds = thisDirSprites[i].GetLocalBounds();
                        x += bounds.Left;
                        y += bounds.Top;
                        w += bounds.Width;
                        h += bounds.Height;
                        t++;
                    }
                    averageAABBs.Add(dir, Box2.FromDimensions(x / t, y / t, w / t, h / t));
                    t = 0;
                    x = 0;
                    y = 0;
                    w = 0;
                    h = 0;
                }
            }
        }

        public SFML.Graphics.Sprite GetCurrentSprite()
        {
            return _currentSprite;
        }

        public void Draw(Color4 Color)
        {
            _currentSprite.Scale = new SFML.System.Vector2f(HorizontalFlip ? -1 : 1, 1);
            _currentSprite.Color = Color.Convert();
            _currentSprite.Draw();
            _currentSprite.Color = Color4.White.Convert();
        }

        public void SetPosition(float x, float y)
        {
            _currentSprite.Position = new SFML.System.Vector2f(x, y);
        }

        private void UpdateDirection()
        {
            SetCurrentSprite();
        }

        public void SetAnimationState(string state)
        {
            if (_currentAnimationState != null)
            {
                //Dont change the state to the current state or we'll be stuck always on the first frame :(
                if (_currentAnimationState.Name == state)
                    return;
                _currentAnimationState.Reset();
            }
            if (state != null)
            {
                _currentAnimationState = AnimationStates[state];
                _currentAnimationState.Enabled = true;
            }
        }

        private void SetCurrentSprite()
        {
            _currentSprite = _sprites[_currentAnimationState.Name][Direction][_currentAnimationState.CurrentFrame];
        }

        public void SetLoop(bool loop)
        {
            _currentAnimationState.Loop = loop;
        }

        public void SetTime(float time)
        {
            _currentAnimationState.SetTime(time);
            SetCurrentSprite();
        }

        public void CopyStateFrom(AnimatedSprite sprite)
        {
            SetAnimationState(sprite.CurrentAnimationStateKey);
            Direction = sprite.Direction;
            var otherAnimationState = sprite._currentAnimationState;
            SetLoop(otherAnimationState.Loop);
            SetTime(otherAnimationState.CurrentTime);
        }

        public void Update(float time)
        {
            _currentAnimationState.AddTime(time);
            SetCurrentSprite();
        }

        public static string DirectionToUriComponent(Direction dir)
        {
            switch (dir)
            {
                case Direction.East:
                    return "e";
                case Direction.West:
                    return "w";
                case Direction.North:
                    return "n";
                case Direction.South:
                    return "s";
                case Direction.NorthEast:
                    return "ne";
                case Direction.SouthEast:
                    return "se";
                case Direction.NorthWest:
                    return "nw";
                case Direction.SouthWest:
                    return "sw";
                default:
                    return "";
            }
        }

        #endregion Methods

        #region Constructor

        public AnimatedSprite(string name, AnimationCollection collection, IResourceCache resourceCache)
        {
            AnimationStates = new Dictionary<string, AnimationState>();
            Name = name;
            LoadSprites(collection, resourceCache);
            if (AnimationStates.ContainsKey("idle"))
                SetAnimationState("idle");
            SetLoop(true);
            SetCurrentSprite();

            //IoCManager.Resolve<IResourceCache>().
        }

        #endregion Constructor
    }
}
