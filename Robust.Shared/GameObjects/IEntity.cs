using System;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.GameObjects
{
    [CopyByRef, Serializable]
    public sealed class IEntity : EntityUid
    {
        public IEntity(int uid) : base(uid)
        {
        }
    }
}
