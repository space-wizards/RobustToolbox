using System;

namespace SS14.Shared.Input
{
    public abstract class InputCommand
    {
        public virtual void Enabled() { }
        public virtual void Disabled() { }

        /// <summary>
        ///     Makes a quick input command from enabled and disabled delegates.
        /// </summary>
        /// <param name="enabled">The delegate to be ran when this command is enabled.</param>
        /// <param name="disabled">The delegate to be ran when this command is disabled.</param>
        /// <returns>The new input command.</returns>
        public static InputCommand FromDelegate(Action enabled = null, Action disabled = null)
        {
            return new DelegateInputCommand
            {
                EnabledDelegate = enabled,
                DisabledDelegate = disabled,
            };
        }

        class DelegateInputCommand : InputCommand
        {
            public Action EnabledDelegate;
            public Action DisabledDelegate;

            public override void Enabled()
            {
                if (EnabledDelegate != null)
                {
                    EnabledDelegate();
                }
            }

            public override void Disabled()
            {
                if (DisabledDelegate != null)
                {
                    DisabledDelegate();
                }
            }
        }
    }
}
