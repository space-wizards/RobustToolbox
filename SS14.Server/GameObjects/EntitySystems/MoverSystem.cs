using SS14.Server.Interfaces.Player;
using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.Systems;
using SS14.Shared.Input;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.Maths;
using SS14.Shared.Players;

namespace SS14.Server.GameObjects.EntitySystems
{
    class MoverSystem : EntitySystem
    {
        /// <inheritdoc />
        public override void Initialize()
        {
            EntityQuery = new TypeEntityQuery(typeof(PlayerInputMoverComponent));
            
            var moveUpCmdHandler = InputCmdHandler.FromDelegate(
                session => HandleDirChange(session, Direction.North, true),
                session => HandleDirChange(session, Direction.North, false));
            var moveLeftCmdHandler = InputCmdHandler.FromDelegate(
                session => HandleDirChange(session, Direction.West, true),
                session => HandleDirChange(session, Direction.West, false));
            var moveRightCmdHandler = InputCmdHandler.FromDelegate(
                session => HandleDirChange(session, Direction.East, true),
                session => HandleDirChange(session, Direction.East, false));
            var moveDownCmdHandler = InputCmdHandler.FromDelegate(
                session => HandleDirChange(session, Direction.South, true),
                session => HandleDirChange(session, Direction.South, false));
            var runCmdHandler = InputCmdHandler.FromDelegate(
                session => HandleRunChange(session, true),
                session => HandleRunChange(session, false));

            var input = EntitySystemManager.GetEntitySystem<InputSystem>();

            input.BindMap.BindFunction(EngineKeyFunctions.MoveUp, moveUpCmdHandler);
            input.BindMap.BindFunction(EngineKeyFunctions.MoveLeft, moveLeftCmdHandler);
            input.BindMap.BindFunction(EngineKeyFunctions.MoveRight, moveRightCmdHandler);
            input.BindMap.BindFunction(EngineKeyFunctions.MoveDown, moveDownCmdHandler);
            input.BindMap.BindFunction(EngineKeyFunctions.Run, runCmdHandler);
        }

        /// <inheritdoc />
        public override void Shutdown()
        {
            if (EntitySystemManager.TryGetEntitySystem(out InputSystem input))
            {
                input.BindMap.UnbindFunction(EngineKeyFunctions.MoveUp);
                input.BindMap.UnbindFunction(EngineKeyFunctions.MoveLeft);
                input.BindMap.UnbindFunction(EngineKeyFunctions.MoveRight);
                input.BindMap.UnbindFunction(EngineKeyFunctions.MoveDown);
                input.BindMap.UnbindFunction(EngineKeyFunctions.Run);
            }

            base.Shutdown();
        }

        /// <inheritdoc />
        public override void Update(float frameTime)
        {
            foreach (var entity in RelevantEntities)
            {
                var mover = entity.GetComponent<PlayerInputMoverComponent>();
                var physics = entity.GetComponent<PhysicsComponent>();

                UpdateKinematics(entity.Transform, mover, physics);
            }
        }

        private static void UpdateKinematics(ITransformComponent transform, PlayerInputMoverComponent mover, PhysicsComponent physics)
        {
            if (mover.VelocityDir.LengthSquared < 0.001)
            {
                if (physics.LinearVelocity != Vector2.Zero)
                    physics.LinearVelocity = Vector2.Zero;
            }
            else
            {
                physics.LinearVelocity = mover.VelocityDir * (mover.Sprinting ? mover.SprintMoveSpeed : mover.WalkMoveSpeed);
                transform.LocalRotation = mover.VelocityDir.GetDir().ToAngle();
            }
        }

        private static void HandleDirChange(ICommonSession session, Direction dir, bool state)
        {
            if(!TryGetAttachedComponent(session as IPlayerSession, out PlayerInputMoverComponent moverComp))
                return;

            moverComp.SetVelocityDirection(dir, state);
        }

        private static void HandleRunChange(ICommonSession session, bool running)
        {
            if(!TryGetAttachedComponent(session as IPlayerSession, out PlayerInputMoverComponent moverComp))
                return;

            moverComp.Sprinting = running;
        }

        private static bool TryGetAttachedComponent<T>(IPlayerSession session, out T component)
            where T: Component
        {
            component = default(T);
            
            var ent = session.AttachedEntity;

            if (ent == null || !ent.IsValid())
                return false;

            if (!ent.TryGetComponent(out T comp))
                return false;

            component = comp;
            return true;
        }
    }
}
