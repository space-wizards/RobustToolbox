using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Robust.Server.ViewVariables.Traits
{
    internal sealed class ViewVariablesTraitEntity : ViewVariablesTrait
    {
        private readonly EntityUid _entity;

        public ViewVariablesTraitEntity(IViewVariablesSession session) : base(session)
        {
            var netEntity = (NetEntity) Session.Object;
            _entity = IoCManager.Resolve<IEntityManager>().GetEntity(netEntity);
        }

        public override ViewVariablesBlob? DataRequest(ViewVariablesRequest viewVariablesRequest)
        {
            var entMan = IoCManager.Resolve<IEntityManager>();
            var compFactory = IoCManager.Resolve<IComponentFactory>();

            if (viewVariablesRequest is ViewVariablesRequestMembers)
            {
                var blob = new ViewVariablesBlobMembers();

                // TODO VV: Fill blob with info about this entity.

                return blob;
            }

            if (viewVariablesRequest is ViewVariablesRequestEntityComponents)
            {
                var list = new List<ViewVariablesBlobEntityComponents.Entry>();
                // See engine#636 for why the Distinct() call.
                foreach (var component in entMan.GetComponents(_entity))
                {
                    var type = component.GetType();
                    list.Add(new ViewVariablesBlobEntityComponents.Entry
                        {Stringified = PrettyPrint.PrintUserFacingTypeShort(type, 2), FullName = type.FullName, ComponentName = compFactory.GetComponentName(type)});
                }

                return new ViewVariablesBlobEntityComponents
                {
                    ComponentTypes = list
                };
            }

            if (viewVariablesRequest is ViewVariablesRequestAllValidComponents)
            {
                var list = new List<string>();

                var componentFactory = IoCManager.Resolve<IComponentFactory>();

                foreach (var type in componentFactory.AllRegisteredTypes)
                {
                    if (entMan.HasComponent(_entity, type))
                        continue;

                    list.Add(componentFactory.GetRegistration(type).Name);
                }

                return new ViewVariablesBlobAllValidComponents(){ComponentTypes = list};
            }

            return base.DataRequest(viewVariablesRequest);
        }
    }
}
