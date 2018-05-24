using System;
using System.Collections.Generic;
using System.Linq;
using SS14.Client.Console;
using SS14.Client.GameObjects.EntitySystems;
using SS14.Client.Input;
using SS14.Client.Interfaces.GameObjects;
using SS14.Client.Interfaces.GameObjects.Components;
using SS14.Client.Interfaces.Graphics.ClientEye;
using SS14.Client.Interfaces.Input;
using SS14.Client.Interfaces.Placement;
using SS14.Client.Interfaces.Player;
using SS14.Client.Interfaces.UserInterface;
using SS14.Client.UserInterface;
using SS14.Client.UserInterface.Controls;
using SS14.Client.UserInterface.CustomControls;
using SS14.Shared;
using SS14.Shared.Configuration;
using SS14.Shared.GameObjects;
using SS14.Shared.Input;
using SS14.Shared.Interfaces.Configuration;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using SS14.Shared.Map;
using SS14.Shared.Network;

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

        private EscapeMenu escapeMenu;

        private Chatbox _gameChat;

        public override void Startup()
        {
            IoCManager.InjectDependencies(this);

            escapeMenu = new EscapeMenu
            {
                Visible = false
            };
            escapeMenu.AddToScreen();

            var escapeMenuCommand = InputCommand.FromDelegate(() =>
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
            inputManager.SetInputCommand(EngineKeyFunctions.FocusChat, InputCommand.FromDelegate(() =>
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
        }

        public override void Update(ProcessFrameEventArgs e)
        {
            _componentManager.Update(e.Elapsed);
            _entityManager.Update(e.Elapsed);
            playerManager.Update(e.Elapsed);
        }

        public override void FrameUpdate(RenderFrameEventArgs e)
        {
            placementManager.FrameUpdate(e);
            _entityManager.FrameUpdate(e.Elapsed);
        }

        public override void MouseDown(MouseButtonEventArgs eventargs)
        {
            if (playerManager.LocalPlayer == null || placementManager.MouseDown(eventargs))
                return;

            var map = playerManager.LocalPlayer.ControlledEntity.GetComponent<ITransformComponent>().MapID;
            var mousePosWorld = eyeManager.ScreenToWorld(new ScreenCoordinates(eventargs.Position, map));

            // Find all the entities intersecting our click
            var entities =
                _entityManager.GetEntitiesIntersecting(mousePosWorld.MapID, mousePosWorld.Position);

            // Check the entities against whether or not we can click them
            var clickedEntities = new List<(IEntity clicked, int drawDepth)>();
            foreach (IEntity entity in entities)
            {
                if (entity.TryGetComponent<IClientClickableComponent>(out var component)
                && entity.GetComponent<ITransformComponent>().IsMapTransform
                && component.CheckClick(mousePosWorld, out int drawdepthofclicked))
                {
                    clickedEntities.Add((entity, drawdepthofclicked));
                }
            }

            IEntity entityToClick;

            if (clickedEntities.Any())
            {
                entityToClick = (from cd in clickedEntities
                                 orderby cd.drawDepth ascending,
                                     cd.clicked.GetComponent<ITransformComponent>().LocalPosition
                                     .Y ascending
                                 select cd.clicked).Last();
            }
            else
            {
                entityToClick = null;
            }

            //First possible exit point for click, acceptable due to being clientside
            if (entityToClick != null && placementManager.Eraser && placementManager.IsActive)
            {
                placementManager.HandleDeletion(entityToClick);
                return;
            }

            ClickType clicktype = eventargs.ClickType;
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

            //Assemble information to send to server about click
            if (clicktype != ClickType.None)
            {
                var UID = EntityUid.Invalid;
                if (entityToClick != null)
                    UID = entityToClick.Uid;

                ClickEventMessage message = new ClickEventMessage(UID, clicktype, mousePosWorld);
                //IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<InputSystem>().RaiseClick(message);
                // TODO: CLICKING
            }
        }

        public override void MouseUp(MouseButtonEventArgs e)
        {
            placementManager.MouseUp(e);
        }
    }
}
