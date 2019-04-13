using System;
using SS14.Shared.GameObjects;
using SS14.Shared.Map;
using SS14.Shared.Serialization;
using SS14.Shared.Timing;

namespace SS14.Shared.Input
{
    /// <summary>
    ///     Abstract class that all Input Commands derive from.
    /// </summary>
    [Serializable, NetSerializable]
    public abstract class InputCmdMessage : EntitySystemMessage
    {
        /// <summary>
        ///     Client tick this was created.
        /// </summary>
        public GameTick Tick { get; }

        /// <summary>
        ///     The function this command is changing.
        /// </summary>
        public KeyFunctionId InputFunctionId { get; }

        /// <summary>
        ///     Creates an instance of <see cref="InputCmdMessage"/>.
        /// </summary>
        /// <param name="tick">Client tick this was created.</param>
        /// <param name="inputFunctionId">Function this command is changing.</param>
        public InputCmdMessage(GameTick tick, KeyFunctionId inputFunctionId)
        {
            Tick = tick;
            InputFunctionId = inputFunctionId;
        }
    }

    /// <summary>
    ///     An Input Command for a function that has a state.
    /// </summary>
    [Serializable, NetSerializable]
    public class StateInputCmdMessage : InputCmdMessage
    {
        /// <summary>
        ///     New state of the Input Function.
        /// </summary>
        public BoundKeyState State { get; }

        /// <summary>
        ///     Creates an instance of <see cref="StateInputCmdMessage"/>.
        /// </summary>
        /// <param name="tick">Client tick this was created.</param>
        /// <param name="inputFunctionId">Function this command is changing.</param>
        /// <param name="state">New state of the Input Function.</param>
        public StateInputCmdMessage(GameTick tick, KeyFunctionId inputFunctionId, BoundKeyState state)
            : base(tick, inputFunctionId)
        {
            State = state;
        }
    }

    /// <summary>
    ///     A OneShot Input Command that does not have a state.
    /// </summary>
    [Serializable, NetSerializable]
    public class EventInputCmdMessage : InputCmdMessage
    {
        /// <summary>
        ///     Creates an instance of <see cref="EventInputCmdMessage"/>.
        /// </summary>
        /// <param name="tick">Client tick this was created.</param>
        /// <param name="inputFunctionId">Function this command is changing.</param>
        public EventInputCmdMessage(GameTick tick, KeyFunctionId inputFunctionId)
            : base(tick, inputFunctionId) { }
    }

    /// <summary>
    ///     A OneShot Input Command that also contains pointer info.
    /// </summary>
    [Serializable, NetSerializable]
    public class PointerInputCmdMessage : EventInputCmdMessage
    {
        /// <summary>
        ///     Local Coordinates of the pointer when the command was created.
        /// </summary>
        public GridCoordinates Coordinates { get; }

        /// <summary>
        ///     Entity that was under the pointer when the command was created (if any).
        /// </summary>
        public EntityUid Uid { get; }

        /// <summary>
        ///     Creates an instance of <see cref="PointerInputCmdMessage"/>.
        /// </summary>
        /// <param name="tick">Client tick this was created.</param>
        /// <param name="inputFunctionId">Function this command is changing.</param>
        /// <param name="coordinates">Local Coordinates of the pointer when the command was created.</param>
        public PointerInputCmdMessage(GameTick tick, KeyFunctionId inputFunctionId, GridCoordinates coordinates)
            : this(tick, inputFunctionId, coordinates, EntityUid.Invalid) { }

        /// <summary>
        ///     Creates an instance of <see cref="PointerInputCmdMessage"/> with an optional Entity reference.
        /// </summary>
        /// <param name="tick">Client tick this was created.</param>
        /// <param name="inputFunctionId">Function this command is changing.</param>
        /// <param name="coordinates">Local Coordinates of the pointer when the command was created.</param>
        /// <param name="uid">Entity that was under the pointer when the command was created.</param>
        public PointerInputCmdMessage(GameTick tick, KeyFunctionId inputFunctionId, GridCoordinates coordinates, EntityUid uid)
            : base(tick, inputFunctionId)
        {
            Coordinates = coordinates;
            Uid = uid;
        }
    }

    /// <summary>
    ///     An input command that has both state and pointer info.
    /// </summary>
    [Serializable, NetSerializable]
    public class FullInputCmdMessage : InputCmdMessage
    {
        /// <summary>
        ///     New state of the Input Function.
        /// </summary>
        public BoundKeyState State { get; }

        /// <summary>
        ///     Local Coordinates of the pointer when the command was created.
        /// </summary>
        public GridCoordinates Coordinates { get; }

        /// <summary>
        ///     Screen Coordinates of the pointer when the command was created.
        /// </summary>
        public ScreenCoordinates ScreenCoordinates { get; }

        /// <summary>
        ///     Entity that was under the pointer when the command was created (if any).
        /// </summary>
        public EntityUid Uid { get; }

        /// <summary>
        ///     Creates an instance of <see cref="FullInputCmdMessage"/>.
        /// </summary>
        /// <param name="tick">Client tick this was created.</param>
        /// <param name="inputFunctionId">Function this command is changing.</param>
        /// <param name="state">New state of the Input Function.</param>
        /// <param name="coordinates">Local Coordinates of the pointer when the command was created.</param>
        /// <param name="screenCoordinates"></param>
        public FullInputCmdMessage(GameTick tick, KeyFunctionId inputFunctionId, BoundKeyState state, GridCoordinates coordinates, ScreenCoordinates screenCoordinates)
            : this(tick, inputFunctionId, state, coordinates, screenCoordinates, EntityUid.Invalid) { }

        /// <summary>
        ///     Creates an instance of <see cref="FullInputCmdMessage"/> with an optional Entity reference.
        /// </summary>
        /// <param name="tick">Client tick this was created.</param>
        /// <param name="inputFunctionId">Function this command is changing.</param>
        /// <param name="state">New state of the Input Function.</param>
        /// <param name="coordinates">Local Coordinates of the pointer when the command was created.</param>
        /// <param name="screenCoordinates"></param>
        /// <param name="uid">Entity that was under the pointer when the command was created.</param>
        public FullInputCmdMessage(GameTick tick, KeyFunctionId inputFunctionId, BoundKeyState state, GridCoordinates coordinates, ScreenCoordinates screenCoordinates, EntityUid uid)
            : base(tick, inputFunctionId)
        {
            State = state;
            Coordinates = coordinates;
            ScreenCoordinates = screenCoordinates;
            Uid = uid;
        }
    }
}
