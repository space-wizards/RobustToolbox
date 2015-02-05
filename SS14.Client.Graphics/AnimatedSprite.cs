using GorgonLibrary.Graphics;
using SS14.Client.Interfaces.Resource;
using SS14.Shared;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace SS14.Client.Graphics
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
                if(_direction != value)
                {
                    _direction = value;
                    UpdateDirection();
                }
            }
        }

        public RectangleF AverageAABB
        {
            get
            {
                if (_currentSprite != null)
                    return _averageAABBs[_currentAnimationState.Name][Direction];
                return RectangleF.Empty;
            }
        }

        #region Sprite passthrough methods

        public RectangleF AABB
        {
            get
            {
                if (_currentSprite != null)
                    return _currentSprite.AABB;
                return RectangleF.Empty;
            }
        }

        public bool HorizontalFlip
        {
            get
            {
                if (_currentSprite != null) return _currentSprite.HorizontalFlip;
                return false;
            }
            set
            {
                if (_currentSprite != null)
                    _currentSprite.HorizontalFlip = value;
            }
        }

        #endregion

        #endregion

        #region Variables

        /// <summary>
        /// Dictionary of animation names to directional sprite set
        /// </summary>
        private Dictionary<string, Dictionary<Direction, Sprite[]>> _sprites = new Dictionary<string, Dictionary<Direction, Sprite[]>>();

        private Dictionary<string, Dictionary<Direction, RectangleF>> _averageAABBs = new Dictionary<string, Dictionary<Direction, RectangleF>>();
        private Sprite _currentSprite;

        private Direction _direction = Direction.South;

        #endregion

        #region Methods

        //Todo encapsulate this further down as components -- AnimatedSpriteState, AnimatedSpriteStateDirection
        public void LoadSprites(AnimationCollection collection, IResourceManager resourceManager)
        {
            float x = 0, y = 0, h = 0, w = 0;
            int t = 0;
            foreach (var info in collection.Animations)
            {
                _sprites.Add(info.Name, new Dictionary<Direction, Sprite[]>());

                //Because we have a shitload of frames, we're going to store the average size as the AABB for each direction and each animation
                _averageAABBs.Add(info.Name, new Dictionary<Direction, RectangleF>());

                var sprites = _sprites[info.Name];
                var averageAABBs = _averageAABBs[info.Name];
                AnimationStates.Add(info.Name, new AnimationState(info));
                foreach (var dir in Enum.GetValues(typeof(Direction)).Cast<Direction>())
                {
                    sprites.Add(dir, new Sprite[info.Frames]);
                    var thisDirSprites = sprites[dir];
                    for (var i = 0; i < info.Frames; i++)
                    {
                        var spritename = collection.Name.ToLowerInvariant() + "_" + info.Name.ToLowerInvariant() + "_"
                                         + DirectionToUriComponent(dir) + "_" + i;
                        thisDirSprites[i] = resourceManager.GetSprite(spritename);
                        x += thisDirSprites[i].AABB.X;
                        y += thisDirSprites[i].AABB.Y;
                        w += thisDirSprites[i].AABB.Width;
                        h += thisDirSprites[i].AABB.Height;
                        t++;
                    }
                    averageAABBs.Add(dir, new RectangleF(x / t, y / t, w / t, h / t));
                    t = 0;
                    x = 0;
                    y = 0;
                    w = 0;
                    h = 0;
                }
            }
        }

        public Sprite GetCurrentSprite()
        {
            return _currentSprite;
        }

        public void Draw()
        {
            _currentSprite.Draw();
        }

        public void SetPosition(float x, float y)
        {
            _currentSprite.SetPosition(x, y);
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

        #endregion

        #region Constructor

        public AnimatedSprite(string name, AnimationCollection collection, IResourceManager resourceManager)
        {
            AnimationStates = new Dictionary<string, AnimationState>();
            Name = name;
            LoadSprites(collection, resourceManager);
            if (AnimationStates.ContainsKey("idle"))
                SetAnimationState("idle");
            SetLoop(true);
            SetCurrentSprite();

            //IoCManager.Resolve<IResourceManager>().
        }

        #endregion
    }
}
