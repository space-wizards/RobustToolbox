using Robust.Shared.ViewVariables;

namespace Robust.Client.ViewVariables
{
    public interface IClientViewVariablesManager : IViewVariablesManager
    {
        /// <summary>
        ///     Open a VV window for a locally existing object.
        /// </summary>
        /// <param name="obj">The object to VV.</param>
        void OpenVV(object obj);

        /// <summary>
        ///     Open a VV window for a locally existing object.
        /// </summary>
        /// <param name="path">The VV path to the object to VV.</param>
        void OpenVV(string path);

        /// <summary>
        ///     Open a VV window for a remotely existing object.
        /// </summary>
        /// <param name="selector">The selector to reference the object remotely.</param>
        void OpenVV(ViewVariablesObjectSelector selector);
    }
}
