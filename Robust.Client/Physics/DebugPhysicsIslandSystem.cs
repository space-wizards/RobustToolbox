using System;
using System.Collections.Generic;
using Robust.Client.Graphics;
using Robust.Shared.Console;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Timing;

namespace Robust.Client.Physics
{
    internal sealed class PhysicsIslandCommand : IConsoleCommand
    {
        public string Command => "showislands";
        public string Description => "Shows the current physics bodies involved in each physics island.";
        public string Help => "showislands";
        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 0)
            {
                shell.WriteLine("This command doesn't take args!");
                return;
            }

            EntitySystem.Get<DebugPhysicsIslandSystem>().Mode ^= DebugPhysicsIslandMode.Solve;
        }
    }

    internal sealed class DebugPhysicsIslandSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        public DebugPhysicsIslandMode Mode { get; set; } = DebugPhysicsIslandMode.None;

        /*
         * Island solve debug:
         * This will draw above every body involved in a particular island solve.
         */

        public readonly Queue<(TimeSpan Time, List<IPhysBody> Bodies)> IslandSolve = new();
        public const float SolveDuration = 0.1f;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<IslandSolveMessage>(HandleIslandSolveMessage);
            IoCManager.Resolve<IOverlayManager>().AddOverlay(new PhysicsIslandOverlay());
        }

        public override void Shutdown()
        {
            base.Shutdown();
            UnsubscribeLocalEvent<IslandSolveMessage>();
            IoCManager.Resolve<IOverlayManager>().RemoveOverlay(typeof(PhysicsIslandOverlay));
        }

        public override void FrameUpdate(float frameTime)
        {
            base.FrameUpdate(frameTime);
            while (IslandSolve.TryPeek(out var solve))
            {
                if (solve.Time.TotalSeconds + SolveDuration > _gameTiming.CurTime.TotalSeconds)
                {
                    IslandSolve.Dequeue();
                }
                else
                {
                    break;
                }
            }
        }

        private void HandleIslandSolveMessage(IslandSolveMessage message)
        {
            if ((Mode & DebugPhysicsIslandMode.Solve) == 0x0) return;
            IslandSolve.Enqueue((_gameTiming.CurTime, message.Bodies));
        }
    }

    [Flags]
    internal enum DebugPhysicsIslandMode : ushort
    {
        None = 0,
        Solve = 1 << 0,
        Contacts = 1 << 1,
    }

    internal sealed class PhysicsIslandOverlay : Overlay
    {
        [Dependency] private readonly IEyeManager _eyeManager = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        private DebugPhysicsIslandSystem _islandSystem = default!;

        public override OverlaySpace Space => OverlaySpace.WorldSpace;

        public PhysicsIslandOverlay()
        {
            _islandSystem = EntitySystem.Get<DebugPhysicsIslandSystem>();
            _eyeManager = IoCManager.Resolve<IEyeManager>();
            _gameTiming = IoCManager.Resolve<IGameTiming>();
        }

        protected internal override void Draw(in OverlayDrawArgs args)
        {
            var worldHandle = (DrawingHandleWorld) args.DrawingHandle;

            DrawIslandSolve(worldHandle);
        }

        private void DrawIslandSolve(DrawingHandleWorld handle)
        {
            if ((_islandSystem.Mode & DebugPhysicsIslandMode.Solve) == 0x0) return;

            var viewport = _eyeManager.GetWorldViewport();

            foreach (var solve in _islandSystem.IslandSolve)
            {
                var ratio = (float) Math.Max(
                    (solve.Time.TotalSeconds + DebugPhysicsIslandSystem.SolveDuration -
                     _gameTiming.CurTime.TotalSeconds) / DebugPhysicsIslandSystem.SolveDuration, 0.0f);

                if (ratio <= 0.0f) continue;

                foreach (var body in solve.Bodies)
                {
                    var worldAABB = body.GetWorldAABB();
                    if (!viewport.Intersects(worldAABB)) continue;

                    handle.DrawRect(worldAABB, Color.Green.WithAlpha(ratio * 0.5f));
                }
            }
        }
    }
}
