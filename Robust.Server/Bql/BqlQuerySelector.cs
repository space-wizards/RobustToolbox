using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Robust.Server.Bql
{
    [Flags]
    [PublicAPI]
    public enum QuerySelectorArgument
    {
        Integer    = 0b00000001,
        Float      = 0b00000010,
        String     = 0b00000100,
        Percentage = 0b00001000,
        Component  = 0b00010000,
        //SubQuery   = 0b00100000,
        EntityId   = 0b01000000,
    }

    [PublicAPI]
    public abstract class BqlQuerySelector
    {
        /// <summary>
        /// The token name for the given QuerySelector, for example `when`.
        /// </summary>
        public virtual string Token => throw new NotImplementedException();

        /// <summary>
        /// Arguments for the given QuerySelector, presented as "what arguments are permitted in what spot".
        /// </summary>
        public virtual QuerySelectorArgument[] Arguments => throw new NotImplementedException();

        public virtual IEnumerable<IEntity> DoSelection(IEnumerable<IEntity> input, IReadOnlyList<object> arguments, bool isInverted)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Performs selection as the first selector in the query. Allows for optimizing when you can be more efficient
        /// than just querying every entity.
        /// </summary>
        /// <param name="arguments"></param>
        /// <param name="isInverted"></param>
        /// <returns></returns>
        public virtual IEnumerable<IEntity> DoInitialSelection(IReadOnlyList<object> arguments, bool isInverted)
        {
            var entityManager = IoCManager.Resolve<IEntityManager>();
            return DoSelection(entityManager.GetEntities(), arguments, isInverted);
        }

        [UsedImplicitly]
        protected BqlQuerySelector() {}
    }
}
