using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.IoC;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.GameObjects
{
    [CopyByRef, Serializable]
    public sealed class IEntity
    {
        #region Members

        [ViewVariables]
        public EntityUid Uid { get; }


        [ViewVariables(VVAccess.ReadWrite)]
        public string Name
        {
            get => IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(Uid).EntityName;
            set => IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(Uid).EntityName = value;
        }

        [ViewVariables]
        public TransformComponent Transform => IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(Uid);

        #endregion Members

        #region Initialization

        public IEntity(EntityUid uid)
        {
            Uid = uid;
        }

        #endregion Initialization

        #region Component Messaging

        [Obsolete("Component Messages are deprecated, use Entity Events instead.")]
        public void SendNetworkMessage(IComponent owner, ComponentMessage message, INetChannel? channel = null)
        {
            IoCManager.Resolve<IEntityManager>().EntityNetManager?.SendComponentNetworkMessage(channel, this, owner, message);
        }

        #endregion Component Messaging

        #region Components

        public bool HasComponent<T>()
        {
            return IoCManager.Resolve<IEntityManager>().HasComponent<T>(Uid);
        }

        public bool HasComponent(Type type)
        {
            return IoCManager.Resolve<IEntityManager>().HasComponent(Uid, type);
        }

        public T GetComponent<T>()
        {
            return IoCManager.Resolve<IEntityManager>().GetComponent<T>(Uid);
        }

        public IComponent GetComponent(Type type)
        {
            return IoCManager.Resolve<IEntityManager>().GetComponent(Uid, type);
        }

        public bool TryGetComponent<T>([NotNullWhen(true)] out T? component) where T : class
        {
            return IoCManager.Resolve<IEntityManager>().TryGetComponent(Uid, out component);
        }

        public T? GetComponentOrNull<T>() where T : class, IComponent
        {
            return IoCManager.Resolve<IEntityManager>().GetComponentOrNull<T>(Uid);
        }

        public bool TryGetComponent(Type type, [NotNullWhen(true)] out IComponent? component)
        {
            return IoCManager.Resolve<IEntityManager>().TryGetComponent(Uid, type, out component);
        }

        #endregion Components
    }
}
