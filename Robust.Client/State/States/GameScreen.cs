using Robust.Client.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using System.Collections.Generic;
using System.Linq;
using Robust.Client.GameObjects.EntitySystems;
using Robust.Client.Input;
using Robust.Client.Interfaces.GameObjects;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Client.Interfaces.Graphics.ClientEye;
using Robust.Client.Interfaces.Input;
using Robust.Client.Interfaces.Placement;
using Robust.Client.Interfaces.UserInterface;
using Robust.Client.Player;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Interfaces.Timing;

namespace Robust.Client.State.States
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
        readonly IPlacementManager placementManager;
        [Dependency]
        readonly IEyeManager eyeManager;
        [Dependency]
        private readonly IEntitySystemManager entitySystemManager;
        [Dependency]
        private readonly IGameTiming timing;

        private EscapeMenu escapeMenu;
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
        }

        public override void Shutdown()
        {
            escapeMenu.Dispose();

            playerManager.LocalPlayer.DetachEntity();

            userInterfaceManager.StateRoot.DisposeAllChildren();

            inputManager.SetInputCommand(EngineKeyFunctions.EscapeMenu, null);

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

            var mousePosWorld = eyeManager.ScreenToWorld(new ScreenCoordinates(inputManager.MouseScreenPosition));
            var entityToClick = GetEntityUnderPosition(mousePosWorld);
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
            if (playerManager.LocalPlayer == null)
                return;

            var mousePosWorld = eyeManager.ScreenToWorld(new ScreenCoordinates(eventargs.Position));
            var entityToClick = GetEntityUnderPosition(mousePosWorld);

            //Dispatches clicks to relevant clickable components, another single exit point for UI
            if (entityToClick == null)
                return;

            var clickable = entityToClick.GetComponent<IClientClickableComponent>();
            clickable.DispatchClick(playerManager.LocalPlayer.ControlledEntity, eventargs.ClickType);
        }

        public IEntity GetEntityUnderPosition(GridCoordinates coordinates)
        {
            var entitiesUnderPosition = GetEntitiesUnderPosition(coordinates);
            return entitiesUnderPosition.Count > 0 ? entitiesUnderPosition[0] : null;
        }

        /// <summary>
        ///     Gets all the entities currently under the mouse cursor.
        /// </summary>
        /// <returns>A list of the entities, sorted such that the first entry is the top entity.</returns>
        public IList<IEntity> GetEntitiesUnderMouse()
        {
            var mousePosWorld = eyeManager.ScreenToWorld(new ScreenCoordinates(inputManager.MouseScreenPosition));
            return GetEntitiesUnderPosition(mousePosWorld);
        }

        public IList<IEntity> GetEntitiesUnderPosition(GridCoordinates coordinates)
        {
            return GetEntitiesUnderPosition(_entityManager, coordinates);
        }

        private static IList<IEntity> GetEntitiesUnderPosition(IClientEntityManager entityMan,
            GridCoordinates coordinates)
        {
            // Find all the entities intersecting our click
            var entities = entityMan.GetEntitiesIntersecting(coordinates.MapID, coordinates.Position);

            // Check the entities against whether or not we can click them
            var foundEntities = new List<(IEntity clicked, int drawDepth)>();
            foreach (var entity in entities)
            {
                if (entity.TryGetComponent<IClientClickableComponent>(out var component)
                    && entity.Transform.IsMapTransform
                    && component.CheckClick(coordinates, out var drawDepthClicked))
                {
                    foundEntities.Add((entity, drawDepthClicked));
                }
            }

            if (foundEntities.Count == 0)
                return new List<IEntity>();

            foundEntities.Sort(new ClickableEntityComparer());
            // 0 is the top element.
            foundEntities.Reverse();
            return foundEntities.Select(a => a.clicked).ToList();
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
                var transx = x.clicked.Transform;
                var transy = y.clicked.Transform;
                return transx.GridPosition.Y.CompareTo(transy.GridPosition.Y);
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
            var message = new FullInputCmdMessage(timing.CurTick, funcId, args.State, mousePosWorld, args.PointerLocation, entityToClick?.Uid ?? EntityUid.Invalid);

            // client side command handlers will always be sent the local player session.
            var session = playerManager.LocalPlayer.Session;
            inputSys.HandleInputCommand(session, func, message);
        }
    }
}
