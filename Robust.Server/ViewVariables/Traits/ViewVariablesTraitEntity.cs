using System.Collections.Generic;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Robust.Server.ViewVariables.Traits
{
    internal sealed class ViewVariablesTraitEntity : ViewVariablesTrait
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
                foreach (var component in _entity.GetAllComponents())
                {
                    var type = component.GetType();
                    list.Add(new ViewVariablesBlobEntityComponents.Entry
                        {Stringified = TypeAbbreviation.Abbreviate(type.ToString()), FullName = type.FullName});
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
