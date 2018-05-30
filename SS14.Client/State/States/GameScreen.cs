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

        private IEntity lastEntity;

        public override void FrameUpdate(RenderFrameEventArgs e)
        {
            placementManager.FrameUpdate(e);
            _entityManager.FrameUpdate(e.Elapsed);

            var map = playerManager.LocalPlayer.ControlledEntity.GetComponent<ITransformComponent>().MapID;
            IEntity entityToClick = GetEntityUnderPosition(new ScreenCoordinates(inputManager.MouseScreenPosition, map));
            if (entityToClick == lastEntity)
            {
                return;
            }

            lastEntity?.GetComponent<ISpriteComponent>()?.LayerSetShader(0, (Shader)null);
            lastEntity = entityToClick;
            entityToClick?.GetComponent<ISpriteComponent>()?.LayerSetShader(0, "selection_outline");
        }

        public override void MouseDown(MouseButtonEventArgs eventargs)
        {
            if (playerManager.LocalPlayer == null || placementManager.MouseDown(eventargs))
                return;

            var map = playerManager.LocalPlayer.ControlledEntity.GetComponent<ITransformComponent>().MapID;
            var screencoords = new ScreenCoordinates(eventargs.Position, map);
            IEntity entityToClick = GetEntityUnderPosition(screencoords);
            var mousePosWorld = eyeManager.ScreenToWorld(screencoords);

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
                IoCManager.Resolve<IEntityNetworkManager>().SendSystemNetworkMessage(message);
            }
        }

        private IEntity GetEntityUnderPosition(ScreenCoordinates coordinates)
        {
            var mousePosWorld = eyeManager.ScreenToWorld(coordinates);

            // Find all the entities intersecting our click
            var entities =
                _entityManager.GetEntitiesIntersecting(mousePosWorld.MapID, mousePosWorld.Position);

            // Check the entities against whether or not we can click them
            var foundEntities = new List<(IEntity clicked, int drawDepth)>();
            foreach (IEntity entity in entities)
            {
                if (entity.TryGetComponent<IClientClickableComponent>(out var component)
                && entity.GetComponent<ITransformComponent>().IsMapTransform
                && component.CheckClick(mousePosWorld, out int drawdepthofclicked))
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
    }
}
