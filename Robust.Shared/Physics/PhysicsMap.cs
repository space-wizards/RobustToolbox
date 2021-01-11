using System.Collections.Generic;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Utility;

namespace Robust.Shared.Physics
{
    public class PhysicsMap
    {
        // AKA world.
        public HashSet<PhysicsComponent> Bodies = new();

        public HashSet<PhysicsComponent> AwakeBodies = new();

        // Queued map changes
        private HashSet<PhysicsComponent> _queuedBodyAdd = new();
        private HashSet<PhysicsComponent> _queuedBodyRemove = new();

        public void AddBody(PhysicsComponent body)
        {
            DebugTools.Assert(!_queuedBodyAdd.Contains(body));
            _queuedBodyAdd.Add(body);
        }

        public void RemoveBody(PhysicsComponent body)
        {
            DebugTools.Assert(!_queuedBodyRemove.Contains(body));
            _queuedBodyRemove.Add(body);
        }

        #region Queue
        private void ProcessChanges()
        {
            ProcessAddQueue();
            ProcessRemoveQueue();
        }

        private void ProcessAddQueue()
        {
            foreach (var body in _queuedBodyAdd)
            {
                Bodies.Add(body);

                if (body.Awake)
                {
                    AwakeBodies.Add(body);
                }
            }

            _queuedBodyAdd.Clear();
        }

        private void ProcessRemoveQueue()
        {
            foreach (var body in _queuedBodyRemove)
            {
                Bodies.Remove(body);

                if (body.Awake)
                {
                    AwakeBodies.Remove(body);
                }
            }

            _queuedBodyRemove.Clear();
        }
        #endregion

        public void Step(float frameTime)
        {
            ProcessChanges();
        }
    }
}
