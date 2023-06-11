using System;
using Robust.Client.GameStates;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Players;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Client.GameObjects
{
    /// <summary>
    ///     Client-side processing of all input commands through the simulation.
    /// </summary>
    public sealed class InputSystem : SharedInputSystem, IPostInjectInit
    {
        [Dependency] private readonly IInputManager _inputManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IClientGameStateManager _stateManager = default!;
        [Dependency] private readonly IConsoleHost _conHost = default!;
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly ILogManager _logManager = default!;

        private ISawmill _sawmillInputContext = default!;

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
                if (!_stateManager.IsPredictionEnabled && !handler.FireOutsidePrediction)
                    continue;

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

            _conHost.RegisterCommand("incmd",
                "Inserts an input command into the simulation",
                "incmd <KeyFunction> <d|u KeyState> <wxPos> <wyPos>",
                GenerateInputCommand);
        }

        public override void Shutdown()
        {
            base.Shutdown();

            _conHost.UnregisterCommand("incmd");
        }

        private void GenerateInputCommand(IConsoleShell shell, string argstr, string[] args)
        {
            var localPlayer = _playerManager.LocalPlayer;
            if(localPlayer is null)
                return;

            var pent = localPlayer.ControlledEntity;
            if(pent is null)
                return;

            BoundKeyFunction keyFunction = new BoundKeyFunction(args[0]);
            BoundKeyState state = args[1] == "u" ? BoundKeyState.Up: BoundKeyState.Down;

            var pxform = Transform(pent.Value);
            var wPos = pxform.WorldPosition + new Vector2(float.Parse(args[2]), float.Parse(args[3]));
            var coords = EntityCoordinates.FromMap(EntityManager, pent.Value, new MapCoordinates(wPos, pxform.MapID));

            var funcId = _inputManager.NetworkBindMap.KeyFunctionID(keyFunction);

            var message = new FullInputCmdMessage(_timing.CurTick, _timing.TickFraction, funcId, state,
                coords, new ScreenCoordinates(0, 0, default), EntityUid.Invalid);

            HandleInputCommand(localPlayer.Session, keyFunction, message);
        }

        private void OnAttachedEntityChanged(PlayerAttachSysMessage message)
        {
            if (message.AttachedEntity != default) // attach
            {
                SetEntityContextActive(_inputManager, message.AttachedEntity);
            }
            else // detach
            {
                _inputManager.Contexts.SetActiveContext(InputContextContainer.DefaultContextName);
            }
        }

        private void SetEntityContextActive(IInputManager inputMan, EntityUid entity)
        {
            if(entity == default || !EntityManager.EntityExists(entity))
                throw new ArgumentNullException(nameof(entity));

            if (!EntityManager.TryGetComponent(entity, out InputComponent? inputComp))
            {
                _sawmillInputContext.Debug($"AttachedEnt has no InputComponent: entId={entity}, entProto={EntityManager.GetComponent<MetaDataComponent>(entity).EntityPrototype}. Setting default \"{InputContextContainer.DefaultContextName}\" context...");
                inputMan.Contexts.SetActiveContext(InputContextContainer.DefaultContextName);
                return;
            }

            if (inputMan.Contexts.Exists(inputComp.ContextName))
            {
                inputMan.Contexts.SetActiveContext(inputComp.ContextName);
            }
            else
            {
                _sawmillInputContext.Error($"Unknown context: entId={entity}, entProto={EntityManager.GetComponent<MetaDataComponent>(entity).EntityPrototype}, context={inputComp.ContextName}. . Setting default \"{InputContextContainer.DefaultContextName}\" context...");
                inputMan.Contexts.SetActiveContext(InputContextContainer.DefaultContextName);
            }
        }

        /// <summary>
        ///     Sets the active context to the defined context on the attached entity.
        /// </summary>
        public void SetEntityContextActive()
        {
            var controlled = _playerManager.LocalPlayer?.ControlledEntity ?? EntityUid.Invalid;
            if (controlled == EntityUid.Invalid)
            {
                return;
            }

            SetEntityContextActive(_inputManager, controlled);
        }

        void IPostInjectInit.PostInject()
        {
            _sawmillInputContext = _logManager.GetSawmill("input.context");
        }
    }

    /// <summary>
    ///     Entity system message that is raised when the player changes attached entities.
    /// </summary>
    public sealed class PlayerAttachSysMessage : EntityEventArgs
    {
        /// <summary>
        ///     New entity the player is attached to.
        /// </summary>
        public EntityUid AttachedEntity { get; }

        /// <summary>
        ///     Creates a new instance of <see cref="PlayerAttachSysMessage"/>.
        /// </summary>
        /// <param name="attachedEntity">New entity the player is attached to.</param>
        public PlayerAttachSysMessage(EntityUid attachedEntity)
        {
            AttachedEntity = attachedEntity;
        }
    }

    public sealed class PlayerAttachedEvent : EntityEventArgs
    {
        public PlayerAttachedEvent(EntityUid entity)
        {
            Entity = entity;
        }

        public EntityUid Entity { get; }
    }

    public sealed class PlayerDetachedEvent : EntityEventArgs
    {
        public PlayerDetachedEvent(EntityUid entity)
        {
            Entity = entity;
        }

        public EntityUid Entity { get; }
    }
}
