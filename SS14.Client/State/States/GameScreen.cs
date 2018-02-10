using System;
using System.Collections.Generic;
using System.Linq;
using SS14.Client.Console;
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
        }

        public override void KeyDown(KeyEventArgs e)
        {
            if (e.Key == Keyboard.Key.Escape)
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

                e.Handle();
                return;
            }

            if (e.Key == Keyboard.Key.T && !_gameChat.Input.HasFocus() && !(userInterfaceManager.Focused is LineEdit))
            {
                _gameChat.Input.GrabFocus();
            }

            inputManager.KeyDown(e);
        }

        public override void KeyUp(KeyEventArgs e)
        {
            inputManager.KeyUp(e);
        }

        public override void MouseDown(MouseButtonEventArgs e)
        {
            if (playerManager.LocalPlayer == null || placementManager.MouseDown(e))
                return;

            var map = playerManager.LocalPlayer.ControlledEntity.GetComponent<ITransformComponent>().MapID;
            var mousePosWorld = eyeManager.ScreenToWorld(new ScreenCoordinates(e.Position, map));

            // Find all the entities intersecting our click
            var entities =
                _entityManager.GetEntitiesIntersecting(mousePosWorld.MapID, mousePosWorld.Position);

            // Check the entities against whether or not we can click them
            var clickedEntities = new List<(IEntity clicked, int drawDepth)>();
            foreach (IEntity entity in entities)
            {
                if (entity.TryGetComponent<IClientClickableComponent>(out var component)
                 && component.CheckClick(mousePosWorld, out int drawdepthofclicked))
                {
                    clickedEntities.Add((entity, drawdepthofclicked));
                }
            }

            if (!clickedEntities.Any())
            {
                return;
            }

            //Sort them by which we should click
            IEntity entToClick = (from cd in clickedEntities
                                  orderby cd.drawDepth ascending,
                                      cd.clicked.GetComponent<ITransformComponent>().LocalPosition
                                      .Y ascending
                                  select cd.clicked).Last();

            if (placementManager.Eraser && placementManager.IsActive)
            {
                placementManager.HandleDeletion(entToClick);
                return;
            }

            // Check whether click is outside our 1.5 meter range
            float checkDistance = 1.5f;
            if (!playerManager.LocalPlayer.ControlledEntity.GetComponent<ITransformComponent>().LocalPosition.InRange(entToClick.GetComponent<ITransformComponent>().LocalPosition, checkDistance))
                return;

            var clickable = entToClick.GetComponent<IClientClickableComponent>();
            switch (e.Button)
            {
                case Mouse.Button.Left:
                    clickable.DispatchClick(playerManager.LocalPlayer.ControlledEntity, ClickType.Left);
                    break;
                case Mouse.Button.Right:
                    clickable.DispatchClick(playerManager.LocalPlayer.ControlledEntity, ClickType.Right);
                    break;
                //case Mouse.Button.Middle:
                //    OpenEntityEditWindow(entToClick);
                //    return;
            }
        }

        public override void MouseUp(MouseButtonEventArgs e)
        {
            placementManager.MouseUp(e);
        }
    }
}
