using System;
using System.Collections.Generic;

namespace Robust.Shared.GameObjects
{
    public abstract partial class EntitySystem
    {
        private Dictionary<Action, (Func<float> required, float accumulated)> _updateRegistrations = new();

        /// <inheritdoc />
        public IEnumerable<Action> GetDueUpdates(float frameTime)
        {
            foreach (var (method, (required, accumulated)) in _updateRegistrations)
            {
                var requiredValue = required();
                var delta = accumulated + frameTime;
                while (delta > requiredValue)
                {
                    delta -= requiredValue;
                    yield return method;
                }

                _updateRegistrations[method] = (required, delta);
            }
        }

        protected void RegisterUpdate(float requiredFrametime, Action method)
        {
            RegisterUpdate(() => requiredFrametime, method);
        }

        protected void RegisterUpdate(Func<float> requiredFrameTimeProvider, Action method)
        {
            if (_updateRegistrations.ContainsKey(method))
                throw new ArgumentException($"Method {method} has already been registered.");

            _updateRegistrations[method] = (requiredFrameTimeProvider, 0f);
        }

        protected void UnregisterUpdate(Action method)
        {
            if (!_updateRegistrations.Remove(method))
                throw new ArgumentException($"Failed unregistering method {method}.");
        }
    }
}
