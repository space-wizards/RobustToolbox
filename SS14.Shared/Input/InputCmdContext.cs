using System.Collections;
using System.Collections.Generic;

namespace SS14.Shared.Input
{
    public interface IInputCmdContext : IEnumerable<BoundKeyFunction>
    {
        void AddFunction(BoundKeyFunction function);
        bool FunctionExists(BoundKeyFunction function);
        bool FunctionExistsHierarchy(BoundKeyFunction function);
        void RemoveFunction(BoundKeyFunction function);
    }

    internal class InputCmdContext : IInputCmdContext
    {
        private List<BoundKeyFunction> _commands = new List<BoundKeyFunction>();
        private IInputCmdContext _parent;

        public InputCmdContext(IInputCmdContext parent)
        {
            _parent = parent;
        }

        internal InputCmdContext() { }

        public void AddFunction(BoundKeyFunction function)
        {
            _commands.Add(function);
        }

        public bool FunctionExists(BoundKeyFunction function)
        {
            return _commands.Contains(function);
        }

        public bool FunctionExistsHierarchy(BoundKeyFunction function)
        {
            if(_commands.Contains(function))
                return true;

            if(_parent != null)
                return _parent.FunctionExistsHierarchy(function);

            return false;
        }

        public void RemoveFunction(BoundKeyFunction function)
        {
            _commands.Remove(function);
        }

        public IEnumerator<BoundKeyFunction> GetEnumerator()
        {
            return _commands.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
