using SS14.Client.GameObjects.Components;
using SS14.Client.Interfaces.Input;
using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.Systems;
using SS14.Shared.Input;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using SS14.Shared.Players;

namespace SS14.Client.GameObjects.EntitySystems
{
    /// <summary>
    ///     Client-side processing of all input commands through the simulation.
    /// </summary>
    class InputSystem : EntitySystem
    {
        private readonly IPlayerCommandStates _cmdStates = new PlayerCommandStates();
        private readonly CommandBindMapping _bindMap = new CommandBindMapping();

        /// <summary>
        ///     Current states for all of the keyFunctions.
        /// </summary>
        public IPlayerCommandStates CmdStates => _cmdStates;

        /// <summary>
        ///     Holds the keyFunction -> handler bindings for the simulation.
        /// </summary>
        public ICommandBindMapping BindMap => _bindMap;

        /// <summary>
        ///     Inserts an Input Command into the simulation.
        /// </summary>
        /// <param name="session">Player session that raised the command. On client, this is always the LocalPlayer session.</param>
        /// <param name="function">Function that is being changed.</param>
        /// <param name="message">Arguments for this event.</param>
        public void HandleInputCommand(ICommonSession session, BoundKeyFunction function, FullInputCmdMessage message)
        {
            // set state, state change is updated regardless if it is locally bound
            _cmdStates.SetState(function, message.State);

            // handle local binds before sending off
            if (_bindMap.TryGetHandler(function, out var handler))
            {
                // local handlers can block sending over the network.
                if (handler.HandleCmdMessage(session, message))
                    return;
            }

            RaiseNetworkEvent(message);
        }

        /// <inheritdoc />
        public override void SubscribeEvents()
        {
            base.SubscribeEvents();

            SubscribeEvent<PlayerAttachSysMessage>(OnAttachedEntityChanged);
        }

        private static void OnAttachedEntityChanged(object sender, EntitySystemMessage message)
        {
            if(!(message is PlayerAttachSysMessage msg))
                return;

            var inputMan = IoCManager.Resolve<IInputManager>();

            if (msg.AttachedEntity != null) // attach
            {
                if(!msg.AttachedEntity.TryGetComponent(out InputComponent inputComp))
                {
                    Logger.DebugS("input.context", $"AttachedEnt has no InputComponent: entId={msg.AttachedEntity.Uid}, entProto={msg.AttachedEntity.Prototype}");
                    return;
                }

                if(inputMan.Contexts.Exists(inputComp.ContextName))
                {
                    inputMan.Contexts.SetActiveContext(inputComp.ContextName);
                }
                else
                {
                    Logger.ErrorS("input.context", $"Unknown context: entId={msg.AttachedEntity.Uid}, entProto={msg.AttachedEntity.Prototype}, context={inputComp.ContextName}");
                }
            }
            else // detach
            {
                inputMan.Contexts.SetActiveContext(InputContextContainer.DefaultContextName);
            }
        }
    }

    /// <summary>
    ///     Entity system message that is raised when the player changes attached entities.
    /// </summary>
    public class PlayerAttachSysMessage : EntitySystemMessage
    {
        /// <summary>
        ///     New entity the player is attached to.
        /// </summary>
        public IEntity AttachedEntity { get; }

        /// <summary>
        ///     Creates a new instance of <see cref="PlayerAttachSysMessage"/>.
        /// </summary>
        /// <param name="attachedEntity">New entity the player is attached to.</param>
        public PlayerAttachSysMessage(IEntity attachedEntity)
        {
            AttachedEntity = attachedEntity;
        }
    }
}
