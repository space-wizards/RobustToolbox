using System;

namespace SS14.Shared.IoC
{
    /// <summary>
    /// Defines priority and whether or not IoC can resolve something to this class.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public class IoCTargetAttribute : Attribute
    {
        private bool disabled = false;
        private int priority = 0;

        public bool Disabled { get { return disabled; } set { disabled = value; } }
        public int Priority { get { return priority; } set { priority = value; } }
    }
}
