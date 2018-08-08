using System;
using SS14.Client.Console;
using SS14.Client.Graphics.Shaders;
using SS14.Client.Input;
using SS14.Client.Interfaces.GameObjects;
using SS14.Client.Interfaces.GameObjects.Components;
using SS14.Client.Interfaces.Graphics.ClientEye;
using SS14.Client.Interfaces.Input;
using SS14.Client.Interfaces.Placement;
using SS14.Client.Interfaces.Player;
using SS14.Client.Interfaces.UserInterface;
using SS14.Client.UserInterface.CustomControls;
using SS14.Shared.GameObjects;
using SS14.Shared.Input;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.IoC;
using SS14.Shared.Map;
using System.Collections.Generic;
using System.Linq;
using SS14.Client.GameObjects.EntitySystems;
using SS14.Shared.Interfaces.Timing;
using SS14.Shared.Log;

namespace SS14.Client.State.States
{
    // OH GOD.
    // Ok actually it's fine.
    public sealed partial class GameScreen : State
    {
        [Dependency]
        readonly IClientEntityManager _entityManager;
        [Dependency]
        readonly IComponentManager _componentManager;
        [Dependency]
        readonly IInputManager inputManager;
        [Dependency]
        readonly IPlayerManager playerManager;
        [Dependency]
        readonly IUserInterfaceManager userInterfaceManager;
        [Dependency]
        readonly IMapManager mapManager;
        [Dependency]
        readonly IClientChatConsole console;
        [Dependency]
        readonly IPlacementManager placementManager;
        [Dependency]
        readonly IEyeManager eyeManager;
        [Dependency]
        private readonly IEntitySystemManager entitySystemManager;
        [Dependency]
        private readonly IGameTiming timing;

        private EscapeMenu escapeMenu;

        private Chatbox _gameChat;

        private IEntity lastHoveredEntity;

        public override void Startup()
        {
            IoCManager.InjectDependencies(this);
            
            inputManager.KeyBindStateChanged += OnKeyBindStateChanged;

            escapeMenu = new EscapeMenu
            {
                Visible = false
            };
            escapeMenu.AddToScreen();

            var escapeMenuCommand = InputCmdHandler.FromDelegate(session =>
            {
                if (escapeMenu.Visible)
                {
                    if (escapeMenu.IsAtFront())
                    {
                        escapeMenu.Visible = false;
                    }
                    else
                    {
                        escapeMenu.MoveToFront();
                    }
                }
                else
                {
                    escapeMenu.OpenCentered();
                }
            });
            inputManager.SetInputCommand(EngineKeyFunctions.EscapeMenu, escapeMenuCommand);
            inputManager.SetInputCommand(EngineKeyFunctions.FocusChat, InputCmdHandler.FromDelegate(session =>
            {
                _gameChat.Input.GrabFocus();
            }));

            _gameChat = new Chatbox();
            userInterfaceManager.StateRoot.AddChild(_gameChat);
            _gameChat.TextSubmitted += console.ParseChatMessage;
            console.AddString += _gameChat.AddLine;
            _gameChat.DefaultChatFormat = "say \"{0}\"";
        }

        public override void Shutdown()
        {
            escapeMenu.Dispose();
            _gameChat.TextSubmitted -= console.ParseChatMessage;
            console.AddString -= _gameChat.AddLine;

            playerManager.LocalPlayer.DetachEntity();

            _entityManager.Shutdown();
            userInterfaceManager.StateRoot.DisposeAllChildren();

            var maps = mapManager.GetAllMaps().ToArray();
            foreach (var map in maps)
            {
                if (map.Index != MapId.Nullspace)
                {
                    mapManager.DeleteMap(map.Index);
                }
            }

            inputManager.SetInputCommand(EngineKeyFunctions.EscapeMenu, null);
            inputManager.SetInputCommand(EngineKeyFunctions.FocusChat, null);

            inputManager.KeyBindStateChanged -= OnKeyBindStateChanged;
        }

        public override void Update(ProcessFrameEventArgs e)
        {
            _componentManager.CullRemovedComponents();
            _entityManager.Update(e.Elapsed);
            playerManager.Update(e.Elapsed);
        }

        public override void FrameUpdate(RenderFrameEventArgs e)
        {
            placementManager.FrameUpdate(e);
            _entityManager.FrameUpdate(e.Elapsed);

            var map = playerManager.LocalPlayer.ControlledEntity.GetComponent<ITransformComponent>().MapID;
            var mousePosWorld = eyeManager.ScreenToWorld(new ScreenCoordinates(inputManager.MouseScreenPosition));
            IEntity entityToClick = GetEntityUnderPosition(mousePosWorld);
            if (entityToClick == lastHoveredEntity)
            {
                return;
            }

            if (lastHoveredEntity != null && !lastHoveredEntity.Deleted)
            {
                lastHoveredEntity.GetComponent<IClientClickableComponent>().OnMouseLeave();
            }

            lastHoveredEntity = entityToClick;

            if (lastHoveredEntity != null)
            {
                lastHoveredEntity.GetComponent<IClientClickableComponent>().OnMouseEnter();
            }
        }

        public override void MouseDown(MouseButtonEventArgs eventargs)
        {
            if (playerManager.LocalPlayer == null || placementManager.MouseDown(eventargs))
                return;
            
            var mousePosWorld = eyeManager.ScreenToWorld(new ScreenCoordinates(eventargs.Position));
            var entityToClick = GetEntityUnderPosition(mousePosWorld);

            //First possible exit point for click, acceptable due to being clientside
            if (entityToClick != null && placementManager.Eraser && placementManager.IsActive)
            {
                placementManager.HandleDeletion(entityToClick);
                return;
            }

            var clicktype = eventargs.ClickType;
            //Dispatches clicks to relevant clickable components, another single exit point for UI
            if (entityToClick != null)
            {
                var clickable = entityToClick.GetComponent<IClientClickableComponent>();
                switch (eventargs.ClickType)
                {
                    case ClickType.Left:
                        clickable.DispatchClick(playerManager.LocalPlayer.ControlledEntity, ClickType.Left);
                        break;
                    case ClickType.Right:
                        clickable.DispatchClick(playerManager.LocalPlayer.ControlledEntity, ClickType.Right);
                        break;
                        /*
                    //Acceptable click exit due to being a UI behavior
                    case Mouse.Button.Middle:
                        OpenEntityEditWindow(entToClick);
                        return;
                        */
                }
            }
        }

        public IEntity GetEntityUnderPosition(GridLocalCoordinates coordinates)
        {
            // Find all the entities intersecting our click
            var entities =
                _entityManager.GetEntitiesIntersecting(coordinates.MapID, coordinates.Position);

            // Check the entities against whether or not we can click them
            var foundEntities = new List<(IEntity clicked, int drawDepth)>();
            foreach (IEntity entity in entities)
            {
                if (entity.TryGetComponent<IClientClickableComponent>(out var component)
                && entity.GetComponent<ITransformComponent>().IsMapTransform
                && component.CheckClick(coordinates, out int drawdepthofclicked))
                {
                    foundEntities.Add((entity, drawdepthofclicked));
                }
            }

            if (foundEntities.Count != 0)
            {
                foundEntities.Sort(new ClickableEntityComparer());
                return foundEntities[foundEntities.Count - 1].clicked;
            }
            return null;
        }

        public override void MouseUp(MouseButtonEventArgs e)
        {
            placementManager.MouseUp(e);
        }

        internal class ClickableEntityComparer : IComparer<(IEntity clicked, int depth)>
        {
            public int Compare((IEntity clicked, int depth) x, (IEntity clicked, int depth) y)
            {
                var val = x.depth.CompareTo(y.depth);
                if (val != 0)
                {
                    return val;
                }
                var transx = x.clicked.GetComponent<ITransformComponent>();
                var transy = y.clicked.GetComponent<ITransformComponent>();
                return transx.LocalPosition.Y.CompareTo(transy.LocalPosition.Y);
            }
        }

        /// <summary>
        ///     Converts a state change event from outside the simulation to inside the simulation.
        /// </summary>
        /// <param name="args">Event data values for a bound key state change.</param>
        private void OnKeyBindStateChanged(BoundKeyEventArgs args)
        {
            var inputSys = entitySystemManager.GetEntitySystem<InputSystem>();
            
            var func = args.Function;
            var funcId = inputManager.NetworkBindMap.KeyFunctionID(func);

            var mousePosWorld = eyeManager.ScreenToWorld(args.PointerLocation);
            var entityToClick = GetEntityUnderPosition(mousePosWorld);
            var message = new FullInputCmdMessage(timing.CurTick, funcId, args.State, mousePosWorld, entityToClick?.Uid ?? EntityUid.Invalid);

            // client side command handlers will always be sent the local player session.
            var session = playerManager.LocalPlayer.Session;
            inputSys.HandleInputCommand(session, func, message);
        }
    }
}
