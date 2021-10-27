using System;
using System.Collections.Generic;

namespace Robust.Shared.GameObjects
{
    public abstract partial class EntitySystem
    {
        private Dictionary<Action, (float required, float accumulated)> _updateRegistrations = new();

        /// <inheritdoc />
        public IEnumerable<Action> GetDueUpdates(float frameTime)
        {
            foreach (var (method, (required, accumulated)) in _updateRegistrations)
            {
                var delta = accumulated + frameTime;
                while (delta > required)
                {
                    delta -= required;
                    yield return method;
                }

                _updateRegistrations[method] = (required, delta);
            }
        }

        protected void RegisterUpdate(float requiredFrametime, Action method)
        {
            if (_updateRegistrations.ContainsKey(method))
                throw new ArgumentException($"Method {method} has already been registered.");

            _updateRegistrations[method] = (requiredFrametime, 0f);
        }

        protected void UnregisterUpdate(Action method)
        {
            if (!_updateRegistrations.Remove(method))
                throw new ArgumentException($"Failed unregistering method {method}.");
        }
    }
}
