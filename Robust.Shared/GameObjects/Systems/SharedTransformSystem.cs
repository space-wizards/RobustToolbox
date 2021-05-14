using System.Collections.Generic;

namespace Robust.Shared.GameObjects
{
    internal class SharedTransformSystem : EntitySystem
    {
        private readonly Queue<MoveEvent> _gridMoves = new();
        private readonly Queue<MoveEvent> _otherMoves = new();

        public void DeferMoveEvent(MoveEvent moveEvent)
        {
            if (moveEvent.Sender.HasComponent<IMapGridComponent>())
                _gridMoves.Enqueue(moveEvent);
            else
                _otherMoves.Enqueue(moveEvent);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            // Process grid moves first.
            Process(_gridMoves);
            Process(_otherMoves);

            void Process(Queue<MoveEvent> queue)
            {
                while (queue.TryDequeue(out var ev))
                {
                    if (ev.Sender.Deleted)
                        continue;

                    RaiseLocalEvent(ev);
                }
            }
        }
    }
}
