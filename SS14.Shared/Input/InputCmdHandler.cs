using System;

namespace SS14.Shared.Input
{
    public abstract class InputCmdHandler
    {
        public virtual void Enabled() { }
        public virtual void Disabled() { }

        /// <summary>
        ///     Makes a quick input command from enabled and disabled delegates.
        /// </summary>
        /// <param name="enabled">The delegate to be ran when this command is enabled.</param>
        /// <param name="disabled">The delegate to be ran when this command is disabled.</param>
        /// <returns>The new input command.</returns>
        public static InputCmdHandler FromDelegate(Action enabled = null, Action disabled = null)
        {
            return new StateInputCmdHandler
            {
                EnabledDelegate = enabled,
                DisabledDelegate = disabled,
            };
        }
    }

    public class StateInputCmdHandler : InputCmdHandler
    {
        public Action EnabledDelegate;
        public Action DisabledDelegate;

        public override void Enabled()
        {
            EnabledDelegate?.Invoke();
        }

        public override void Disabled()
        {
            DisabledDelegate?.Invoke();
        }
    }

    public class PointerInputCmdHandler : InputCmdHandler
    {
        //TODO: MAKE ME
    }
}
