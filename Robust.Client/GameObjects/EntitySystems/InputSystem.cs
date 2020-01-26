using System;
using Robust.Client.GameObjects.Components;
using Robust.Client.Interfaces.Input;
using Robust.Client.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Input;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Players;
using Robust.Shared.Utility;

namespace Robust.Client.GameObjects.EntitySystems
{
    /// <summary>
    ///     Client-side processing of all input commands through the simulation.
    /// </summary>
    public class InputSystem : EntitySystem
    {
#pragma warning disable 649
        [Dependency] private readonly IInputManager _inputManager;
        [Dependency] private readonly IPlayerManager _playerManager;
#pragma warning restore 649

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
            #if DEBUG

            var funcId = _inputManager.NetworkBindMap.KeyFunctionID(function);
            DebugTools.Assert(funcId == message.InputFunctionId, "Function ID in message does not match function.");

            #endif

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

        private void OnAttachedEntityChanged(object sender, EntitySystemMessage message)
        {
            if(!(message is PlayerAttachSysMessage msg))
                return;

            if (msg.AttachedEntity != null) // attach
            {
                SetEntityContextActive(_inputManager, msg.AttachedEntity);
            }
            else // detach
            {
                _inputManager.Contexts.SetActiveContext(InputContextContainer.DefaultContextName);
            }
        }

        private static void SetEntityContextActive(IInputManager inputMan, IEntity entity)
        {
            if(entity == null || !entity.IsValid())
                throw new ArgumentNullException(nameof(entity));

            if (!entity.TryGetComponent(out InputComponent inputComp))
            {
                Logger.DebugS("input.context", $"AttachedEnt has no InputComponent: entId={entity.Uid}, entProto={entity.Prototype}");
                return;
            }

            if (inputMan.Contexts.Exists(inputComp.ContextName))
            {
                inputMan.Contexts.SetActiveContext(inputComp.ContextName);
            }
            else
            {
                Logger.ErrorS("input.context", $"Unknown context: entId={entity.Uid}, entProto={entity.Prototype}, context={inputComp.ContextName}");
            }
        }

        /// <summary>
        ///     Sets the active context to the defined context on the attached entity.
        /// </summary>
        public void SetEntityContextActive()
        {
            SetEntityContextActive(_inputManager, _playerManager.LocalPlayer.ControlledEntity);
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
