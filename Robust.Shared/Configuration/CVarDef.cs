using System;
using JetBrains.Annotations;

namespace Robust.Shared.Configuration
{
    /// <summary>
    ///     Abstract base class for <see cref="CVarDef{T}"/>. You shouldn't inherit this yourself and may be looking for
    ///     <see cref="M:Robust.Shared.Configuration.CVarDef.Create``1(System.String,``0,Robust.Shared.Configuration.CVar,System.String)"/>
    /// </summary>
    public abstract class CVarDef
    {
        /// <summary>
        ///     The default value of this CVar when no override is specified by configuration or the user.
        /// </summary>
        public object DefaultValue { get; }
        /// <summary>
        ///     Flags for this CVar.
        /// </summary>
        public CVar Flags { get; }
        /// <summary>
        ///     The name of this CVar. This needs to contain only printable characters.
        ///     Periods '.' are reserved. Everything before the last period is a nested table identifier,
        ///     everything after is the CVar name in the TOML document.
        /// </summary>
        public string Name { get; }
        /// <summary>
        ///     The description of this CVar.
        /// </summary>
        public string? Desc { get; }

        private protected CVarDef(string name, object defaultValue, CVar flags, string? desc)
        {
            Name = name;
            DefaultValue = defaultValue;
            Flags = flags;
            Desc = desc;
        }

        /// <summary>
        ///     Creates a new CVar definition, for use in <see cref="CVarDefsAttribute"/>-annotated classes.
        /// </summary>
        /// <param name="name">See <see cref="Name"/>.</param>
        /// <param name="defaultValue">See <see cref="DefaultValue"/>.</param>
        /// <param name="flag">See <see cref="Flags"/>.</param>
        /// <param name="desc">See <see cref="Desc"/>.</param>
        /// <typeparam name="T">The type of the CVar, which can be any of: bool, int, long, float, string, any enum, and ushort.</typeparam>
        public static CVarDef<T> Create<T>(
            string name,
            T defaultValue,
            CVar flag = CVar.NONE,
            string? desc = null) where T : notnull
        {
            return new(name, defaultValue, flag, desc);
        }
    }

    /// <summary>
    ///     Contains information defining a CVar for <see cref="IConfigurationManager"/>
    /// </summary>
    /// <typeparam name="T">The type of the CVar, which can be any of: bool, int, long, float, string, any enum, and ushort.</typeparam>
    /// <seealso cref="CVarDefsAttribute"/>
    /// <seealso cref="M:Robust.Shared.Configuration.IConfigurationManager.RegisterCVar``1(System.String,``0,Robust.Shared.Configuration.CVar,System.Action{``0})"/>
    public sealed class CVarDef<T> : CVarDef where T : notnull
    {
        public new T DefaultValue { get; }

        internal CVarDef(string name, T defaultValue, CVar flags, string? desc)
            : base(name, defaultValue, flags, desc)
        {
            DefaultValue = defaultValue;
        }
    }

    /// <summary>
    ///     Marks a static class as containing CVar definitions.
    /// </summary>
    /// <remarks>
    ///     There is no limit on the number of CVarDefs classes you can have, and all CVars will ultimately share the
    ///     same namespace regardless of which class they're in.<br/>
    ///     <br/>
    ///     CVar definitions can be in any assembly, but should never be marked <see cref="CVar.REPLICATED"/> or
    ///     <see cref="CVar.NOTIFY"/> if not in a shared assembly.
    /// </remarks>
    /// <example>
    ///     <code>
    ///         public static class MyCVars
    ///         {
    ///             public static readonly CVarDef&lt;bool&gt; MyEnabled =
    ///                 CVarDef.Create("mycvars.enabled", true, CVar.SERVER, "Enables the thing.");
    ///         }
    ///     </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Class)]
    [MeansImplicitUse]
    public sealed class CVarDefsAttribute : Attribute
    {

    }
}
