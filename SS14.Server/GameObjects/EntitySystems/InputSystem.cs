using System;
using System.Collections.Generic;
using SS14.Server.Interfaces.Player;
using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.System;
using SS14.Shared.Input;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.IoC;

namespace SS14.Server.GameObjects.EntitySystems
{
    public class InputSystem : EntitySystem
    {
        public InputSystem()
        {
            EntityQuery = new ComponentEntityQuery
            {
                OneSet = new List<Type>
                {
                    typeof(KeyBindingInputComponent),
                },
            };
        }

        /// <inheritdoc />
        public override void RegisterMessageTypes()
        {
            base.RegisterMessageTypes();

            RegisterMessageType<BoundKeyChangedMessage>();
        }

        /// <inheritdoc />
        public override void HandleNetMessage(INetChannel channel, EntitySystemMessage message)
        {
            base.HandleNetMessage(channel, message);

            var playerMan = IoCManager.Resolve<IPlayerManager>();
            var session = playerMan.GetSessionByChannel(channel);
            var entity = session.AttachedEntity;

            if (entity == null)
                return;

            switch (message)
            {
                case BoundKeyChangedMessage msg:
                    entity.SendMessage(null, new BoundKeyChangedMsg(msg.Function, msg.State));
                    msg.Owner = entity.Uid;
                    RaiseEvent(msg);
                    break;
            }
        }
    }
}
