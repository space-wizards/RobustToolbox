using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Robust.Shared.Input
{
    /// <summary>
    ///     Abstract class that all Input Commands derive from.
    /// </summary>
    [Serializable, NetSerializable]
    public abstract class InputCmdMessage : EntitySystemMessage, IComparable<InputCmdMessage>
    {
        /// <summary>
        ///     Client tick this was created.
        /// </summary>
        public GameTick Tick { get; }

        /// <summary>
        ///     How far into the tick this event was fired.
        /// </summary>
        /// <seealso cref="IGameTiming.TickFraction"/>
        public ushort SubTick { get; }

        /// <summary>
        ///     The function this command is changing.
        /// </summary>
        public KeyFunctionId InputFunctionId { get; }

        /// <summary>
        /// Sequence number of this input command.
        /// </summary>
        public uint InputSequence { get; set; }

        /// <summary>
        ///     Creates an instance of <see cref="InputCmdMessage"/>.
        /// </summary>
        /// <param name="tick">Client tick this was created.</param>
        /// <param name="inputFunctionId">Function this command is changing.</param>
        public InputCmdMessage(GameTick tick, ushort subTick, KeyFunctionId inputFunctionId)
        {
            Tick = tick;
            SubTick = subTick;
            InputFunctionId = inputFunctionId;
        }

        public int CompareTo(InputCmdMessage? other)
        {
            if (other == null)
            {
                return 1;
            }

            if (ReferenceEquals(this, other)) return 0;
            if (ReferenceEquals(null, other)) return 1;
            return InputSequence.CompareTo(other.InputSequence);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"tick={Tick}, subTick={SubTick}, seq={InputSequence} func={InputFunctionId}";
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
        public StateInputCmdMessage(GameTick tick, ushort subTick, KeyFunctionId inputFunctionId, BoundKeyState state)
            : base(tick, subTick, inputFunctionId)
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
        public EventInputCmdMessage(GameTick tick, ushort subTick, KeyFunctionId inputFunctionId)
            : base(tick, subTick, inputFunctionId) { }
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
        public EntityCoordinates Coordinates { get; }

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
        public PointerInputCmdMessage(GameTick tick, ushort subTick, KeyFunctionId inputFunctionId, EntityCoordinates coordinates)
            : this(tick, subTick, inputFunctionId, coordinates, EntityUid.Invalid) { }

        /// <summary>
        ///     Creates an instance of <see cref="PointerInputCmdMessage"/> with an optional Entity reference.
        /// </summary>
        /// <param name="tick">Client tick this was created.</param>
        /// <param name="inputFunctionId">Function this command is changing.</param>
        /// <param name="coordinates">Local Coordinates of the pointer when the command was created.</param>
        /// <param name="uid">Entity that was under the pointer when the command was created.</param>
        public PointerInputCmdMessage(GameTick tick, ushort subTick, KeyFunctionId inputFunctionId, EntityCoordinates coordinates, EntityUid uid)
            : base(tick, subTick, inputFunctionId)
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
        public EntityCoordinates Coordinates { get; }

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
        /// <param name="inputSequence"></param>
        /// <param name="inputFunctionId">Function this command is changing.</param>
        /// <param name="state">New state of the Input Function.</param>
        /// <param name="coordinates">Local Coordinates of the pointer when the command was created.</param>
        /// <param name="screenCoordinates"></param>
        public FullInputCmdMessage(GameTick tick, ushort subTick, int inputSequence, KeyFunctionId inputFunctionId, BoundKeyState state, EntityCoordinates coordinates, ScreenCoordinates screenCoordinates)
            : this(tick, subTick, inputFunctionId, state, coordinates, screenCoordinates, EntityUid.Invalid) { }

        /// <summary>
        ///     Creates an instance of <see cref="FullInputCmdMessage"/> with an optional Entity reference.
        /// </summary>
        /// <param name="tick">Client tick this was created.</param>
        /// <param name="inputFunctionId">Function this command is changing.</param>
        /// <param name="state">New state of the Input Function.</param>
        /// <param name="coordinates">Local Coordinates of the pointer when the command was created.</param>
        /// <param name="screenCoordinates"></param>
        /// <param name="uid">Entity that was under the pointer when the command was created.</param>
        public FullInputCmdMessage(GameTick tick, ushort subTick, KeyFunctionId inputFunctionId, BoundKeyState state, EntityCoordinates coordinates, ScreenCoordinates screenCoordinates, EntityUid uid)
            : base(tick, subTick, inputFunctionId)
        {
            State = state;
            Coordinates = coordinates;
            ScreenCoordinates = screenCoordinates;
            Uid = uid;
        }
    }
}
