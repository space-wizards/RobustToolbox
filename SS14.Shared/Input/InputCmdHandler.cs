using System;
using SS14.Shared.Players;

namespace SS14.Shared.Input
{
    public delegate void InputStateCmdDelegate(ICommonSession channel);

    public abstract class InputCmdHandler
    {
        public virtual void Enabled(ICommonSession session) { }
        public virtual void Disabled(ICommonSession session) { }

        public abstract void HandleCmdMessage(ICommonSession session, InputCmdMessage message);

        /// <summary>
        ///     Makes a quick input command from enabled and disabled delegates.
        /// </summary>
        /// <param name="enabled">The delegate to be ran when this command is enabled.</param>
        /// <param name="disabled">The delegate to be ran when this command is disabled.</param>
        /// <returns>The new input command.</returns>
        public static InputCmdHandler FromDelegate(InputStateCmdDelegate enabled = null, InputStateCmdDelegate disabled = null)
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
        public InputStateCmdDelegate EnabledDelegate;
        public InputStateCmdDelegate DisabledDelegate;

        public override void Enabled(ICommonSession session)
        {
            EnabledDelegate?.Invoke(session);
        }

        public override void Disabled(ICommonSession session)
        {
            DisabledDelegate?.Invoke(session);
        }

        public override void HandleCmdMessage(ICommonSession session, InputCmdMessage message)
        {
            if (!(message is InputCmdStateMessage msg))
                return;

            switch (msg.State)
            {
                case BoundKeyState.Up:
                    Disabled(session);
                    break;
                case BoundKeyState.Down:
                    Enabled(session);
                    break;
            }

            //Client Sanitization: unknown key state, just ignore
        }
    }

    public class PointerInputCmdHandler : InputCmdHandler
    {
        //TODO: MAKE ME
        public override void HandleCmdMessage(ICommonSession session, InputCmdMessage message)
        {
            throw new NotImplementedException();
        }
    }
}
