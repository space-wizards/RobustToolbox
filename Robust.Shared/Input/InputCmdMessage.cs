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
    public abstract class InputCmdMessage : EntityEventArgs, IComparable<InputCmdMessage>
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
    public sealed class StateInputCmdMessage : InputCmdMessage
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
    [Virtual]
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
    public sealed class PointerInputCmdMessage : EventInputCmdMessage
    {
        /// <summary>
        ///     Local Coordinates of the pointer when the command was created.
        /// </summary>
        public NetCoordinates Coordinates { get; }

        /// <summary>
        ///     Entity that was under the pointer when the command was created (if any).
        /// </summary>
        public NetEntity Uid { get; }

        /// <summary>
        ///     Creates an instance of <see cref="PointerInputCmdMessage"/>.
        /// </summary>
        /// <param name="tick">Client tick this was created.</param>
        /// <param name="inputFunctionId">Function this command is changing.</param>
        /// <param name="coordinates">Local Coordinates of the pointer when the command was created.</param>
        public PointerInputCmdMessage(GameTick tick, ushort subTick, KeyFunctionId inputFunctionId, NetCoordinates coordinates)
            : this(tick, subTick, inputFunctionId, coordinates, NetEntity.Invalid) { }

        /// <summary>
        ///     Creates an instance of <see cref="PointerInputCmdMessage"/> with an optional Entity reference.
        /// </summary>
        /// <param name="tick">Client tick this was created.</param>
        /// <param name="inputFunctionId">Function this command is changing.</param>
        /// <param name="coordinates">Local Coordinates of the pointer when the command was created.</param>
        /// <param name="uid">Entity that was under the pointer when the command was created.</param>
        public PointerInputCmdMessage(GameTick tick, ushort subTick, KeyFunctionId inputFunctionId, NetCoordinates coordinates, NetEntity uid)
            : base(tick, subTick, inputFunctionId)
        {
            Coordinates = coordinates;
            Uid = uid;
        }
    }

    /// <summary>
    /// Handles inputs clientside. This is used so the client can still interact with client-only entities without relying on
    /// <see cref="NetEntity"/>.
    /// </summary>
    public sealed class ClientFullInputCmdMessage : InputCmdMessage, IFullInputCmdMessage
    {
        /// <summary>
        ///     New state of the Input Function.
        /// </summary>
        public BoundKeyState State { get; init; }

        /// <summary>
        ///     Local Coordinates of the pointer when the command was created.
        /// </summary>
        public EntityCoordinates Coordinates { get; init; }

        /// <summary>
        ///     Screen Coordinates of the pointer when the command was created.
        /// </summary>
        public ScreenCoordinates ScreenCoordinates { get; init; }

        /// <summary>
        ///     Entity that was under the pointer when the command was created (if any).
        /// </summary>
        public EntityUid Uid { get; init; }

        public ClientFullInputCmdMessage(GameTick tick, ushort subTick, KeyFunctionId inputFunctionId) : base(tick, subTick, inputFunctionId)
        {
        }

        public ClientFullInputCmdMessage(
            GameTick tick,
            ushort subTick,
            KeyFunctionId inputFunctionId,
            EntityCoordinates coordinates,
            ScreenCoordinates screenCoordinates,
            BoundKeyState state,
            EntityUid uid) : base(tick,
            subTick,
            inputFunctionId)
        {
            Coordinates = coordinates;
            ScreenCoordinates = screenCoordinates;
            State = state;
            Uid = uid;
        }
    }

    public interface IFullInputCmdMessage
    {
        GameTick Tick { get; }
        BoundKeyState State { get; }
        KeyFunctionId InputFunctionId { get; }
        ushort SubTick { get; }
        uint InputSequence { get; set; }
    }

    /// <summary>
    ///     An input command that has both state and pointer info.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class FullInputCmdMessage : InputCmdMessage, IFullInputCmdMessage
    {
        /// <summary>
        ///     New state of the Input Function.
        /// </summary>
        public BoundKeyState State { get; }

        /// <summary>
        ///     Local Coordinates of the pointer when the command was created.
        /// </summary>
        public NetCoordinates Coordinates { get; }

        /// <summary>
        ///     Screen Coordinates of the pointer when the command was created.
        /// </summary>
        public ScreenCoordinates ScreenCoordinates { get; }

        /// <summary>
        ///     Entity that was under the pointer when the command was created (if any).
        /// </summary>
        public NetEntity Uid { get; init; }

        /// <summary>
        ///     Creates an instance of <see cref="FullInputCmdMessage"/>.
        /// </summary>
        /// <param name="tick">Client tick this was created.</param>
        /// <param name="inputSequence"></param>
        /// <param name="inputFunctionId">Function this command is changing.</param>
        /// <param name="state">New state of the Input Function.</param>
        /// <param name="coordinates">Local Coordinates of the pointer when the command was created.</param>
        /// <param name="screenCoordinates"></param>
        public FullInputCmdMessage(GameTick tick, ushort subTick, int inputSequence, KeyFunctionId inputFunctionId, BoundKeyState state, NetCoordinates coordinates, ScreenCoordinates screenCoordinates)
            : this(tick, subTick, inputFunctionId, state, coordinates, screenCoordinates, NetEntity.Invalid) { }

        /// <summary>
        ///     Creates an instance of <see cref="FullInputCmdMessage"/> with an optional Entity reference.
        /// </summary>
        /// <param name="tick">Client tick this was created.</param>
        /// <param name="inputFunctionId">Function this command is changing.</param>
        /// <param name="state">New state of the Input Function.</param>
        /// <param name="coordinates">Local Coordinates of the pointer when the command was created.</param>
        /// <param name="screenCoordinates"></param>
        /// <param name="uid">Entity that was under the pointer when the command was created.</param>
        public FullInputCmdMessage(GameTick tick, ushort subTick, KeyFunctionId inputFunctionId, BoundKeyState state, NetCoordinates coordinates, ScreenCoordinates screenCoordinates, NetEntity uid)
            : base(tick, subTick, inputFunctionId)
        {
            State = state;
            Coordinates = coordinates;
            ScreenCoordinates = screenCoordinates;
            Uid = uid;
        }
    }
}
