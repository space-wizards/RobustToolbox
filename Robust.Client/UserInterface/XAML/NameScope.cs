using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace Robust.Client.UserInterface.XAML
{
/// <summary>
    /// Implements a name scope.
    /// </summary>
    public class NameScope
    {
        public bool IsCompleted { get; private set; }

        private readonly Dictionary<string, Control> _inner = new Dictionary<string, Control>();

        public void Register(string name, Control element)
        {
            if (IsCompleted)
                throw new InvalidOperationException("NameScope is completed, no further registrations are allowed");
            Contract.Requires<ArgumentNullException>(name != null);
            Contract.Requires<ArgumentNullException>(element != null);

            if (_inner.TryGetValue(name, out Control? existing))
            {
                if (existing != element)
                {
                    throw new ArgumentException($"Control with the name '{name}' already registered.");
                }
            }
            else
            {
                _inner.Add(name, element);
            }
        }

        public Control Find(string name)
        {
            Contract.Requires<ArgumentNullException>(name != null);

            _inner.TryGetValue(name, out var result);
            return result;
        }

        public void Complete()
        {
            IsCompleted = true;
        }
    }
}
