using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using ClientInterfaces.Resource;
using GorgonLibrary.Graphics;
using SS13_Shared;

namespace SS13.Graphics
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
            set { _direction = value;
                UpdateDirection();
            }
        }

        #region Sprite passthrough methods
        public RectangleF AABB
        {
            get { 
                if (_currentSprite != null) 
                    return _currentSprite.AABB;
                return RectangleF.Empty;
            }
        }

        public bool HorizontalFlip
        {
            get { if (_currentSprite != null) return _currentSprite.HorizontalFlip;
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
        #region private properties
        #endregion

        #region Variables
        /// <summary>
        /// Dictionary of animation names to directional sprite set
        /// </summary>
        private Dictionary<string, Dictionary<Direction, Sprite[]>> _sprites = new Dictionary<string, Dictionary<Direction, Sprite[]>>();

        private Sprite _currentSprite;

        private Direction _direction = Direction.South;
#endregion

#region Methods
        public void LoadSprites(AnimationCollection collection, IResourceManager resourceManager)
        {
            foreach(var info in collection.Animations)
            {
                _sprites.Add(info.Name, new Dictionary<Direction, Sprite[]>());
                var sprites = _sprites[info.Name];
                AnimationStates.Add(info.Name, new AnimationState(info));
                foreach( var dir in Enum.GetValues(typeof(Direction)).Cast<Direction>())
                {
                    sprites.Add(dir, new Sprite[info.Frames]);
                    var thisDirSprites = sprites[dir];
                    for (var i = 0; i < info.Frames; i++)
                    {
                        var spritename = collection.Name.ToLowerInvariant() + "_" + info.Name.ToLowerInvariant() + "_"
                                         + DirectionToUriComponent(dir) + "_" + i;
                        thisDirSprites[i] = resourceManager.GetSprite(spritename);
                    }   
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
            if(_currentAnimationState != null)
            {
                //Dont change the state to the current state or we'll be stuck always on the first frame :(
                if (_currentAnimationState.Name == state)
                    return;
                _currentAnimationState.Reset();
            }
            _currentAnimationState = AnimationStates[state];
            _currentAnimationState.Enabled = true;
        }

        private void SetCurrentSprite()
        {
            _currentSprite = _sprites[_currentAnimationState.Name][Direction][_currentAnimationState.CurrentFrame];
        }

        public void SetLoop(bool loop)
        {
            _currentAnimationState.Loop = loop;
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

#region constructor/destructor
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
