using System.Collections.Generic;
using System.Linq;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.ViewVariables;

namespace SS14.Server.ViewVariables.Traits
{
    internal class ViewVariablesTraitEntity : ViewVariablesTrait
    {
        private readonly IEntity _entity;

        public ViewVariablesTraitEntity(ViewVariablesSession session) : base(session)
        {
            _entity = (IEntity) Session.Object;
        }

        public override ViewVariablesBlob DataRequest(ViewVariablesRequest viewVariablesRequest)
        {
            if (viewVariablesRequest is ViewVariablesRequestEntityComponents)
            {
                var list = new List<ViewVariablesBlobEntityComponents.Entry>();
                // See engine#636 for why the Distinct() call.
                foreach (var component in _entity.GetAllComponents().Distinct())
                {
                    var type = component.GetType();
                    list.Add(new ViewVariablesBlobEntityComponents.Entry
                        {Stringified = type.ToString(), Qualified = type.AssemblyQualifiedName});
                }

                return new ViewVariablesBlobEntityComponents
                {
                    ComponentTypes = list
                };
            }

            return base.DataRequest(viewVariablesRequest);
        }
    }
}
