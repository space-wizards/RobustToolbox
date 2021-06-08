using System;
using Robust.Client.GameStates;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Players;
using Robust.Shared.Utility;

namespace Robust.Client.GameObjects
{
    /// <summary>
    ///     Client-side processing of all input commands through the simulation.
    /// </summary>
    public class InputSystem : SharedInputSystem
    {
        [Dependency] private readonly IInputManager _inputManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IClientGameStateManager _stateManager = default!;

        private readonly IPlayerCommandStates _cmdStates = new PlayerCommandStates();

        /// <summary>
        ///     Current states for all of the keyFunctions.
        /// </summary>
        public IPlayerCommandStates CmdStates => _cmdStates;

        /// <summary>
        /// If the input system is currently predicting input.
        /// </summary>
        public bool Predicted { get; private set; }

        /// <summary>
        ///     Inserts an Input Command into the simulation.
        /// </summary>
        /// <param name="session">Player session that raised the command. On client, this is always the LocalPlayer session.</param>
        /// <param name="function">Function that is being changed.</param>
        /// <param name="message">Arguments for this event.</param>
        /// <param name="replay">if true, current cmd state will not be checked or updated - use this for "replaying" an
        /// old input that was saved or buffered until further processing could be done</param>
        public bool HandleInputCommand(ICommonSession? session, BoundKeyFunction function, FullInputCmdMessage message, bool replay = false)
        {
            #if DEBUG

            var funcId = _inputManager.NetworkBindMap.KeyFunctionID(function);
            DebugTools.Assert(funcId == message.InputFunctionId, "Function ID in message does not match function.");

            #endif

            if (!replay)
            {
                // set state, state change is updated regardless if it is locally bound
                if (_cmdStates.GetState(function) == message.State)
                {
                    return false;
                }
                _cmdStates.SetState(function, message.State);
            }

            // handle local binds before sending off
            foreach (var handler in BindRegistry.GetHandlers(function))
            {
                // local handlers can block sending over the network.
                if (handler.HandleCmdMessage(session, message))
                {
                    return true;
                }
            }

            // send it off to the server
            DispatchInputCommand(message);
            return false;
        }

        /// <summary>
        /// Handle a predicted input command.
        /// </summary>
        /// <param name="inputCmd">Input command to handle as predicted.</param>
        public void PredictInputCommand(FullInputCmdMessage inputCmd)
        {
            DebugTools.AssertNotNull(_playerManager.LocalPlayer);

            var keyFunc = _inputManager.NetworkBindMap.KeyFunctionName(inputCmd.InputFunctionId);

            Predicted = true;
            var session = _playerManager.LocalPlayer!.Session;
            foreach (var handler in BindRegistry.GetHandlers(keyFunc))
            {
                if (handler.HandleCmdMessage(session, inputCmd)) break;
            }
            Predicted = false;

        }

        private void DispatchInputCommand(FullInputCmdMessage message)
        {
            _stateManager.InputCommandDispatched(message);
            EntityManager.EntityNetManager?.SendSystemNetworkMessage(message, message.InputSequence);
        }

        public override void Initialize()
        {
            SubscribeLocalEvent<PlayerAttachSysMessage>(OnAttachedEntityChanged);
        }

        private void OnAttachedEntityChanged(PlayerAttachSysMessage message)
        {
            if (message.AttachedEntity != null) // attach
            {
                SetEntityContextActive(_inputManager, message.AttachedEntity);
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

            if (!entity.TryGetComponent(out InputComponent? inputComp))
            {
                Logger.DebugS("input.context", $"AttachedEnt has no InputComponent: entId={entity.Uid}, entProto={entity.Prototype}. Setting default \"{InputContextContainer.DefaultContextName}\" context...");
                inputMan.Contexts.SetActiveContext(InputContextContainer.DefaultContextName);
                return;
            }

            if (inputMan.Contexts.Exists(inputComp.ContextName))
            {
                inputMan.Contexts.SetActiveContext(inputComp.ContextName);
            }
            else
            {
                Logger.ErrorS("input.context", $"Unknown context: entId={entity.Uid}, entProto={entity.Prototype}, context={inputComp.ContextName}. . Setting default \"{InputContextContainer.DefaultContextName}\" context...");
                inputMan.Contexts.SetActiveContext(InputContextContainer.DefaultContextName);
            }
        }

        /// <summary>
        ///     Sets the active context to the defined context on the attached entity.
        /// </summary>
        public void SetEntityContextActive()
        {
            if (_playerManager.LocalPlayer?.ControlledEntity == null)
            {
                return;
            }

            SetEntityContextActive(_inputManager, _playerManager.LocalPlayer.ControlledEntity);
        }
    }

    /// <summary>
    ///     Entity system message that is raised when the player changes attached entities.
    /// </summary>
    public class PlayerAttachSysMessage : EntityEventArgs
    {
        /// <summary>
        ///     New entity the player is attached to.
        /// </summary>
        public IEntity? AttachedEntity { get; }

        /// <summary>
        ///     Creates a new instance of <see cref="PlayerAttachSysMessage"/>.
        /// </summary>
        /// <param name="attachedEntity">New entity the player is attached to.</param>
        public PlayerAttachSysMessage(IEntity? attachedEntity)
        {
            AttachedEntity = attachedEntity;
        }
    }

    public class PlayerAttachedEvent : EntityEventArgs
    {
        public PlayerAttachedEvent(IEntity entity)
        {
            Entity = entity;
        }

        public IEntity Entity { get; }
    }

    public class PlayerDetachedEvent : EntityEventArgs
    {
        public PlayerDetachedEvent(IEntity old)
        {
            Old = old;
        }

        public IEntity Old { get; }
    }
}
