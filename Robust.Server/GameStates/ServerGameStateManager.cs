using System;
using System.Diagnostics.Tracing;
using System.Linq;
using JetBrains.Annotations;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Network;
using Robust.Shared.Network.Messages;
using Robust.Shared.Timing;
using Robust.Shared.Player;

namespace Robust.Server.GameStates
{
    /// <inheritdoc cref="IServerGameStateManager"/>
    [UsedImplicitly]
    public sealed class ServerGameStateManager : IServerGameStateManager, IPostInjectInit
    {
        private PvsSystem _pvs = default!;

        [Dependency] private readonly EntityManager _entityManager = default!;
        [Dependency] private readonly IServerNetManager _networkManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;

        private ISawmill _logger = default!;

        public ushort TransformNetId { get; set; }

        public Action<ICommonSession, GameTick>? ClientAck { get; set; }
        public Action<ICommonSession, GameTick, NetEntity?>? ClientRequestFull { get; set; }

        public void PostInject()
        {
            _logger = Logger.GetSawmill("gamestate");
        }

        /// <inheritdoc />
        public void Initialize()
        {
            _networkManager.RegisterNetMessage<MsgState>();
            _networkManager.RegisterNetMessage<MsgStateLeavePvs>();
            _networkManager.RegisterNetMessage<MsgStateAck>(HandleStateAck);
            _networkManager.RegisterNetMessage<MsgStateRequestFull>(HandleFullStateRequest);
            _pvs = _entityManager.System<PvsSystem>();
        }

        private void HandleFullStateRequest(MsgStateRequestFull msg)
        {
            if (!_playerManager.TryGetSessionById(msg.MsgChannel.UserId, out var session))
                return;

            NetEntity? ent = msg.MissingEntity.IsValid() ? msg.MissingEntity : null;
            ClientRequestFull?.Invoke(session, msg.Tick, ent);
        }

        private void HandleStateAck(MsgStateAck msg)
        {
            if (_playerManager.TryGetSessionById(msg.MsgChannel.UserId, out var session))
                ClientAck?.Invoke(session, msg.Sequence);
        }

        /// <inheritdoc />
        public void SendGameStateUpdate()
        {
            var players = _playerManager.Sessions.Where(o => o.Status == SessionStatus.InGame).ToArray();
            _pvs.SendGameStates(players);
        }

        [EventSource(Name = "Robust.Pvs")]
        public sealed class PvsEventSource : System.Diagnostics.Tracing.EventSource
        {
            public static PvsEventSource Log { get; } = new();

            [Event(1)]
            public void WorkStart(uint tick, int playerIdx, Guid playerGuid) => WriteEvent(1, tick, playerIdx, playerGuid);

            [Event(2)]
            public void WorkStop(uint tick, int playerIdx, Guid playerGuid) => WriteEvent(2, tick, playerIdx, playerGuid);

            [NonEvent]
            private unsafe void WriteEvent(int eventId, uint arg1, int arg2, Guid arg3)
            {
                if (IsEnabled())
                {
                    var descrs = stackalloc EventData[3];

                    descrs[0].DataPointer = (IntPtr)(&arg1);
                    descrs[0].Size = 4;
                    descrs[1].DataPointer = (IntPtr)(&arg2);
                    descrs[1].Size = 4;
                    descrs[2].DataPointer = (IntPtr)(&arg3);
                    descrs[2].Size = 16;

                    WriteEventCore(eventId, 3, descrs);
                }
            }
        }
    }
}
