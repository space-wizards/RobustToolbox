using System;

namespace Robust.Shared.ViewVariables
{
    /// <summary>
    /// Attribute to change how a numeric property is displayed to the user.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    internal class ViewVariablesNumericAttribute : Attribute
    {
        public readonly NumericDisplay DisplayMethod;

        public ViewVariablesNumericAttribute(NumericDisplay displayMethod)
        {
            DisplayMethod = displayMethod;
        }
    }

    public enum NumericDisplay
    {
        None,
        Generic,
        Hex,
        Flags
    }
}
