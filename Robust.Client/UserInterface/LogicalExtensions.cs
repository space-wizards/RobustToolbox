using System.Collections.Generic;

namespace Robust.Client.UserInterface
{
    public static class LogicalExtensions
    {
        public static IEnumerable<Control> GetSelfAndLogicalAncestors(this Control control)
        {
            Control? c = control;
            while (c != null)
            {
                yield return c;
                c = c.Parent;
            }
        }
    }
}
