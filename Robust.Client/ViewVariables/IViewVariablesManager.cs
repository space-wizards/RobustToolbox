using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;

namespace Robust.Client.ViewVariables
{
    public interface IViewVariablesManager
    {
        /// <summary>
        ///     Open a VV window for a locally existing object.
        /// </summary>
        /// <param name="obj">The object to VV.</param>
        /// <param name="uid">Pass an entityUid to raise directed events when changing members that support this</param>
        void OpenVV(object obj, EntityUid? uid = null);

        /// <summary>
        ///     Open a VV window for a remotely existing object.
        /// </summary>
        /// <param name="selector">The selector to reference the object remotely.</param>
        void OpenVV(ViewVariablesObjectSelector selector);
    }
}
