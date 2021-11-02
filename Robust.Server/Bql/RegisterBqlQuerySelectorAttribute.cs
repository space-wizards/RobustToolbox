using System;
using JetBrains.Annotations;

namespace Robust.Server.Bql
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    [BaseTypeRequired(typeof(BqlQuerySelector))]
    [MeansImplicitUse]
    [PublicAPI]
    public class RegisterBqlQuerySelectorAttribute : Attribute
    {

    }
}
