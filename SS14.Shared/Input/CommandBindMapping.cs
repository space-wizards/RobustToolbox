using System.Collections.Generic;

namespace SS14.Shared.Input
{
    /// <summary>
    ///     Contains a mapping of <see cref="BoundKeyFunction"/> to <see cref="InputCommand"/>.
    /// </summary>
    public class CommandBindMapping
    {
        public IPlayerCommandStates CommandStates { get; private set; }

        private readonly Dictionary<BoundKeyFunction, InputCmdHandler> _commandBinds = new Dictionary<BoundKeyFunction, InputCmdHandler>();

        public CommandBindMapping()
        {
            CommandStates = new PlayerCommandStates();
        }

        public void BindFunction(BoundKeyFunction function, InputCmdHandler command)
        {
            _commandBinds.Add(function, command);
        }
        
        public bool TryGetHandler(BoundKeyFunction function, out InputCmdHandler handler)
        {
            return _commandBinds.TryGetValue(function, out handler);
        }

        public void UnbindFunction(BoundKeyFunction function)
        {
            _commandBinds.Remove(function);
        }
    }
}
