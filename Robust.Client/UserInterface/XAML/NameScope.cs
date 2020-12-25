using System;
using System.Collections.Generic;

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
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }

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

        public void Absorb(NameScope? nameScope)
        {
            if (nameScope == null) return;

            foreach (var (name, control) in nameScope._inner)
            {
                try
                {
                    Register(name, control);
                }
                catch (Exception e)
                {
                    throw new ArgumentException($"Exception occured when trying to absorb NameScope (at name {name})", e);
                }
            }
        }

        public Control? Find(string name)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            _inner.TryGetValue(name, out var result);
            return result;
        }

        public void Complete()
        {
            IsCompleted = true;
        }
    }
}
