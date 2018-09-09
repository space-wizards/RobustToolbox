using System.Linq;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Network;
using SS14.Shared.ViewVariables;

namespace SS14.Server.ViewVariables
{
    public sealed class ViewVariablesSessionEntity : ViewVariablesSessionObject
    {
        private IEntity Entity { get; }

        public ViewVariablesSessionEntity(NetSessionId playerSession, IEntity entity, uint sessionId) : base(
            playerSession, entity, sessionId)
        {
            Entity = entity;
        }

        public override ViewVariablesBlob DataRequest()
        {
            var oldBlob = base.DataRequest();
            var newBlob = new ViewVariablesBlobEntity
            {
                ObjectType = oldBlob.ObjectType,
                ObjectTypePretty = oldBlob.ObjectTypePretty,
                Stringified = oldBlob.Stringified,
                Properties = oldBlob.Properties
            };

            // See engine#636 for why the Distinct() call.
            foreach (var component in Entity.GetAllComponents().Distinct())
            {
                var type = component.GetType();
                newBlob.ComponentTypes.Add((type.AssemblyQualifiedName, type.ToString()));
            }

            return newBlob;
        }
    }
}
