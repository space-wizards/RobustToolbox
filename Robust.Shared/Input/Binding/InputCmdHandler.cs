using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Players;

namespace Robust.Shared.Input.Binding
{
    public delegate void StateInputCmdDelegate(ICommonSession? session);

    public abstract class InputCmdHandler
    {
        public virtual bool FireOutsidePrediction => false;

        public virtual void Enabled(ICommonSession? session)
        {
        }

        public virtual void Disabled(ICommonSession? session)
        {
        }

        public abstract bool HandleCmdMessage(ICommonSession? session, InputCmdMessage message);

        /// <summary>
        ///     Makes a quick input command from enabled and disabled delegates.
        /// </summary>
        /// <param name="enabled">The delegate to be ran when this command is enabled.</param>
        /// <param name="disabled">The delegate to be ran when this command is disabled.</param>
        /// <returns>The new input command.</returns>
        public static InputCmdHandler FromDelegate(StateInputCmdDelegate? enabled = null,
            StateInputCmdDelegate? disabled = null, bool handle=true, bool outsidePrediction=false)
        {
            return new StateInputCmdHandler
            {
                EnabledDelegate = enabled,
                DisabledDelegate = disabled,
                Handle = handle,
                OutsidePrediction = outsidePrediction
            };
        }

        private class StateInputCmdHandler : InputCmdHandler
        {
            public StateInputCmdDelegate? EnabledDelegate;
            public StateInputCmdDelegate? DisabledDelegate;
            public bool Handle { get; set; }
            public bool OutsidePrediction;
            public override bool FireOutsidePrediction => OutsidePrediction;

            public override void Enabled(ICommonSession? session)
            {
                EnabledDelegate?.Invoke(session);
            }

            public override void Disabled(ICommonSession? session)
            {
                DisabledDelegate?.Invoke(session);
            }

            public override bool HandleCmdMessage(ICommonSession? session, InputCmdMessage message)
            {
                if (!(message is FullInputCmdMessage msg))
                    return false;

                switch (msg.State)
                {
                    case BoundKeyState.Up:
                        Disabled(session);
                        return Handle;
                    case BoundKeyState.Down:
                        Enabled(session);
                        return Handle;
                }

                //Client Sanitization: unknown key state, just ignore
                return false;
            }
        }
    }

    public delegate bool PointerInputCmdDelegate(ICommonSession? session, EntityCoordinates coords, EntityUid uid);

    public delegate bool PointerInputCmdDelegate2(in PointerInputCmdHandler.PointerInputCmdArgs args);

    public class PointerInputCmdHandler : InputCmdHandler
    {
        private PointerInputCmdDelegate2 _callback;
        private bool _ignoreUp;

        public override bool FireOutsidePrediction { get; }

        /// <summary>
        /// Handler which will handle the command using the indicated callback
        /// </summary>
        /// <param name="callback">callback to handle the command</param>
        /// <param name="ignoreUp">whether keyup actions will be ignored by this handler (like lifting a key or releasing
        /// mouse button)</param>
        public PointerInputCmdHandler(PointerInputCmdDelegate callback, bool ignoreUp = true, bool outsidePrediction = false)
            : this((in PointerInputCmdArgs args) =>
            callback(args.Session, args.Coordinates, args.EntityUid), ignoreUp, outsidePrediction) { }

        /// <summary>
        /// Handler which will handle the command using the indicated callback
        /// </summary>
        /// <param name="callback">callback to handle the command</param>
        /// <param name="ignoreUp">whether keyup actions will be ignored by this handler (like lifting a key or releasing
        /// mouse button)</param>
        public PointerInputCmdHandler(PointerInputCmdDelegate2 callback, bool ignoreUp = true, bool outsidePrediction = false)
        {
            _callback = callback;
            _ignoreUp = ignoreUp;
            FireOutsidePrediction = outsidePrediction;
        }

        public override bool HandleCmdMessage(ICommonSession? session, InputCmdMessage message)
        {
            if (!(message is FullInputCmdMessage msg) || (_ignoreUp && msg.State != BoundKeyState.Down))
                return false;

            var handled = _callback?.Invoke(new PointerInputCmdArgs(session, msg.Coordinates,
                msg.ScreenCoordinates, msg.Uid, msg.State, msg));
            return handled.HasValue && handled.Value;
        }

        public readonly struct PointerInputCmdArgs
        {
            public readonly ICommonSession? Session;
            public readonly EntityCoordinates Coordinates;
            public readonly ScreenCoordinates ScreenCoordinates;
            public readonly EntityUid EntityUid;
            public readonly BoundKeyState State;
            public readonly FullInputCmdMessage OriginalMessage;

            public PointerInputCmdArgs(ICommonSession? session, EntityCoordinates coordinates,
                ScreenCoordinates screenCoordinates, EntityUid entityUid, BoundKeyState state,
                FullInputCmdMessage originalMessage)
            {
                Session = session;
                Coordinates = coordinates;
                ScreenCoordinates = screenCoordinates;
                EntityUid = entityUid;
                State = state;
                OriginalMessage = originalMessage;
            }
        }
    }

    public class PointerStateInputCmdHandler : InputCmdHandler
    {
        private PointerInputCmdDelegate _enabled;
        private PointerInputCmdDelegate _disabled;
        public override bool FireOutsidePrediction { get; }

        public PointerStateInputCmdHandler(PointerInputCmdDelegate enabled, PointerInputCmdDelegate disabled, bool outsidePrediction = false)
        {
            _enabled = enabled;
            _disabled = disabled;
            FireOutsidePrediction = outsidePrediction;
        }

        /// <inheritdoc />
        public override bool HandleCmdMessage(ICommonSession? session, InputCmdMessage message)
        {
            if (!(message is FullInputCmdMessage msg))
                return false;

            switch (msg.State)
            {
                case BoundKeyState.Up:
                    return _disabled?.Invoke(session, msg.Coordinates, msg.Uid) == true;
                case BoundKeyState.Down:
                    return _enabled?.Invoke(session, msg.Coordinates, msg.Uid) == true;
            }

            //Client Sanitization: unknown key state, just ignore
            return false;
        }
    }

    /// <summary>
    /// Consumes both up and down states without calling any handler delegates. Primarily used on the client to
    /// prevent an input message from being sent to the server.
    /// </summary>
    public class NullInputCmdHandler : InputCmdHandler
    {
        /// <inheritdoc />
        public override bool HandleCmdMessage(ICommonSession? session, InputCmdMessage message)
        {
            return true;
        }
    }
}
