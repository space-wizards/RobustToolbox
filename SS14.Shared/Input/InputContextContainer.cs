using System;
using System.Collections.Generic;

namespace SS14.Shared.Input
{
    public interface IInputContextContainer
    {
        IInputCmdContext ActiveContext { get; }
        event EventHandler<ContextChangedEventArgs> ContextChanged;
        IInputCmdContext New(string uniqueName, string parentName);
        IInputCmdContext New(string uniqueName, IInputCmdContext parent);
        bool Exists(string uniqueName);
        IInputCmdContext GetContext(string uniqueName);
        bool TryGetContext(string uniqueName, out IInputCmdContext context);
        void Remove(string uniqueName);
        void SetActiveContext(string uniqueName);
    }

    internal class InputContextContainer : IInputContextContainer
    {
        public const string DefaultContextName = "common";

        public event EventHandler<ContextChangedEventArgs> ContextChanged;

        private readonly Dictionary<string, InputCmdContext> _contexts = new Dictionary<string, InputCmdContext>();
        private InputCmdContext _activeContext;

        public IInputCmdContext ActiveContext
        {
            get => _activeContext;
            private set
            {
                var args = new ContextChangedEventArgs(_activeContext, value);
                _activeContext = (InputCmdContext) value;
                ContextChanged?.Invoke(this, args);
            }
        }

        public InputContextContainer()
        {
            _contexts.Add(DefaultContextName, new InputCmdContext());
            SetActiveContext(DefaultContextName);
        }

        public IInputCmdContext New(string uniqueName, string parentName)
        {
            if (string.IsNullOrWhiteSpace(uniqueName))
                throw new ArgumentException("String is null or whitespace.", nameof(uniqueName));

            if (string.IsNullOrWhiteSpace(parentName))
                throw new ArgumentException("String is null or whitespace.", nameof(parentName));

            if (!_contexts.TryGetValue(parentName, out var parentContext))
                throw new ArgumentException("Parent does not exist.", nameof(parentName));

            if (_contexts.ContainsKey(uniqueName))
                throw new ArgumentException($"Context with name {uniqueName} already exists.", nameof(uniqueName));

            var newContext = new InputCmdContext(parentContext);
            _contexts.Add(uniqueName, newContext);
            return newContext;
        }

        public IInputCmdContext New(string uniqueName, IInputCmdContext parent)
        {
            if (string.IsNullOrWhiteSpace(uniqueName))
                throw new ArgumentException("String is null or whitespace.", nameof(uniqueName));

            if (_contexts.ContainsKey(uniqueName))
                throw new ArgumentException($"Context with name {uniqueName} already exists.", nameof(uniqueName));

            if (parent == null)
                throw new ArgumentNullException(nameof(parent));

            var newContext = new InputCmdContext(parent);
            _contexts.Add(uniqueName, newContext);
            return newContext;
        }

        public bool Exists(string uniqueName)
        {
            return _contexts.ContainsKey(uniqueName);
        }

        public IInputCmdContext GetContext(string uniqueName)
        {
            return _contexts[uniqueName];
        }

        public bool TryGetContext(string uniqueName, out IInputCmdContext context)
        {
            if (_contexts.TryGetValue(uniqueName, out var ctext))
            {
                context = ctext;
                return true;
            }

            context = default;
            return false;
        }

        public void Remove(string uniqueName)
        {
            _contexts.Remove(uniqueName);
        }

        public void SetActiveContext(string uniqueName)
        {
            ActiveContext = _contexts[uniqueName];
        }
    }

    public class ContextChangedEventArgs : EventArgs
    {
        public IInputCmdContext NewContext { get; }
        public IInputCmdContext OldContext { get; }

        public ContextChangedEventArgs(IInputCmdContext oldContext, IInputCmdContext newContext)
        {
            OldContext = oldContext;
            NewContext = newContext;
        }
    }
}
