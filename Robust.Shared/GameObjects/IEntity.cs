using System;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.GameObjects
{
    [CopyByRef, Serializable]
    public sealed class IEntity
    {
        #region Members

        [ViewVariables]
        public EntityUid Uid { get; }

        #endregion Members

        #region Initialization

        public IEntity(EntityUid uid)
        {
            Uid = uid;
        }

        #endregion Initialization

        #region Components

        #endregion Components
    }
}
