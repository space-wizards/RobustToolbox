using Robust.Client.Graphics;
using Robust.Client.Utility;
using Robust.Shared.IoC;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Client.UserInterface.Controls
{
    /// <summary>
    ///     A more complex control wrapping <see cref="TextureRect"/> that can do RSI directions and animations.
    /// </summary>
    public sealed class AnimatedTextureRect : Control
    {
        private IRsiStateLike? _state;
        private int _curFrame;
        private float _curFrameTime;

        /// <summary>
        ///     Internal TextureRect used to do actual drawing of the texture.
        ///     You can use this property to change shaders or styling or such.
        /// </summary>
        public TextureRect DisplayRect { get; }

        public RSI.State.Direction RsiDirection { get; } = RSI.State.Direction.South;

        public AnimatedTextureRect()
        {
            IoCManager.InjectDependencies(this);

            DisplayRect = new TextureRect();
            AddChild(DisplayRect);
        }

        public void SetFromSpriteSpecifier(SpriteSpecifier specifier)
        {
            _curFrame = 0;
            _state = specifier.RsiStateLike();
            _curFrameTime = _state.GetDelay(0);
            DisplayRect.Texture = _state.GetFrame(RsiDirection, 0);
        }

        protected override void FrameUpdate(FrameEventArgs args)
        {
            if (!VisibleInTree || _state == null || !_state.IsAnimated)
                return;

            var oldFrame = _curFrame;

            _curFrameTime -= args.DeltaSeconds;
            while (_curFrameTime < _state.GetDelay(_curFrame))
            {
                _curFrame = (_curFrame + 1) % _state.AnimationFrameCount;
                _curFrameTime += _state.GetDelay(_curFrame);
            }

            if (_curFrame != oldFrame)
            {
                DisplayRect.Texture = _state.GetFrame(RsiDirection, _curFrame);
            }
        }
    }
}
