using System;
using System.Collections.Generic;
using Robust.Server.ViewVariables.Traits;
using Robust.Shared.IoC;
using Robust.Shared.Network.Messages;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Robust.Server.ViewVariables
{
    /// <summary>
    ///     Traits define what behavior an object can have that VV cares about.
    ///     So like, is it enumerable, does it have VV accessible members. That kinda deal.
    ///     These are the "modular" way of extending VV.
    ///     Server traits are bound to one <see cref="ViewVariablesesSession"/>, AKA one object.
    /// </summary>
    internal abstract class ViewVariablesTrait
    {
        internal readonly IViewVariablesSession Session;

        protected ViewVariablesTrait(IViewVariablesSession session)
        {
            Session = session;
        }

        /// <summary>
        ///     Invoked when the client requests a data blob from the session this trait is bound to,
        ///     Using <see cref="MsgViewVariablesReqData"/>.
        /// </summary>
        /// <param name="viewVariablesRequest">
        ///     The request meta object, equivalent to the <see cref="MsgViewVariablesReqData.RequestMeta"/> object.
        /// </param>
        /// <returns>
        ///     <see langword="null"/>If this trait doesn't care about this request, a meaningful blob otherwise.
        ///     No, not the game mode, the other kind of blob.
        /// </returns>
        public virtual ViewVariablesBlob? DataRequest(ViewVariablesRequest viewVariablesRequest)
        {
            return null;
        }

        /// <summary>
        ///     Tries to get an object relative to the object handled by this trait.
        ///     This is for doing the whole "this guy wants to open a VV window on a sub object" deal.
        /// </summary>
        /// <param name="property">
        ///     The first element of a property index list as described by <see cref="ViewVariablesSessionRelativeSelector.PropertyIndex"/>
        ///     <para>
        ///         "Why is this the first only?" Well my idea was that it should get the first level of the index,
        ///        and then if it were like a tuple the session would handle the rest of the indices.
        ///         Whether that will work out in practice when the next person comes along to implement that is probably uncertain,
        ///         so if you wanna change that future person go ahead.
        ///     </para>
        ///     <!--
        ///         My god that's the first time I've used <para> in a doc comment.
        ///         Hey man at least my stuff is deeply commented so you have an explanation WHY things are half-assed and where the half assing is.
        ///     -->
        /// </param>
        /// <param name="value">The to-be value of the object if this trait managed to retrieve it.</param>
        /// <returns>True if we retrieved a value, false otherwise.</returns>
        public virtual bool TryGetRelativeObject(object property, out object? value)
        {
            value = default;
            return false;
        }

        /// <summary>
        ///     TRY to modify a property on this trait.
        ///     For example <see cref="ViewVariablesTraitMembers"/> tries to modify the object's members here.
        /// </summary>
        /// <param name="property">
        ///     A list of objects that your trait might understand to figure out what to modify.
        ///     See <see cref="ViewVariablesSessionRelativeSelector.PropertyIndex"/> for an explanation.
        /// </param>
        /// <param name="value">
        ///     The new value of the object.
        /// </param>
        /// <returns>True if this trait can and did modify the property, false otherwise.</returns>
        public virtual bool TryModifyProperty(object[] property, object value)
        {
            return false;
        }

        /// <summary>
        ///     Swaps values like references over to reference tokens to prevent issues.
        /// </summary>
        protected object? MakeValueNetSafe(object? value)
        {
            if (value == null)
            {
                return null;
            }

            var valType = value.GetType();
            if (!valType.IsValueType)
            {
                // TODO: More flexibility in which types can be sent here.

                // We don't blindly send any prototypes, we ONLY send prototypes for valid, registered variants.
                if (typeof(IPrototype).IsAssignableFrom(valType)
                    && IoCManager.Resolve<IPrototypeManager>().TryGetVariantFrom(valType, out var variant))
                {
                    return new ViewVariablesBlobMembers.PrototypeReferenceToken()
                    {
                        Stringified = PrettyPrint.PrintUserFacing(value),
                        ID = ((IPrototype) value).ID, Variant = variant
                    };
                }

                if (valType != typeof(string))
                {
                    return new ViewVariablesBlobMembers.ReferenceToken
                    {
                        Stringified = PrettyPrint.PrintUserFacing(value)
                    };
                }
            }
            else if (!Session.RobustSerializer.CanSerialize(valType))
            {
                // Handle KeyValuePair<,>
                if (valType.IsGenericType && valType.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
                {
                    dynamic kv = value;

                    var key = kv.Key;
                    var val = kv.Value;

                    return new ViewVariablesBlobMembers.ServerKeyValuePairToken
                    {
                        Key = MakeValueNetSafe(key),
                        Value = MakeValueNetSafe(val)
                    };
                }

                // Can't send this value type over the wire.
                return new ViewVariablesBlobMembers.ServerValueTypeToken
                {
                    Stringified = value.ToString()
                };
            }

            return value;
        }

        /// <summary>
        ///     Swaps null values for net safe equivalents depending on type.
        ///     Will return null for types where null is considered net safe.
        /// </summary>
        protected object? MakeNullValueNetSafe(Type type)
        {
            if (typeof(IPrototype).IsAssignableFrom(type))
            {
                var protoMan = IoCManager.Resolve<IPrototypeManager>();

                if (protoMan.TryGetVariantFrom(type, out var variant))
                    return new ViewVariablesBlobMembers.PrototypeReferenceToken()
                    {
                        ID = null, Variant = variant, Stringified = PrettyPrint.PrintUserFacing(null),
                    };
            }

            return null;
        }

    }
}
