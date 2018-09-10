using SS14.Shared.ViewVariables;

namespace SS14.Server.ViewVariables
{
    internal abstract class ViewVariablesTrait
    {
        internal readonly ViewVariablesSession Session;

        protected ViewVariablesTrait(ViewVariablesSession session)
        {
            Session = session;
        }

        public virtual ViewVariablesBlob DataRequest(ViewVariablesRequest viewVariablesRequest)
        {
            return null;
        }

        public virtual bool TryGetRelativeObject(object property, out object value)
        {
            value = default(object);
            return false;
        }

        public virtual bool TryModifyProperty(object[] property, object value)
        {
            return false;
        }
    }
}
