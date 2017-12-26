using SS14.Client.Input;
using SS14.Client.UserInterface;

namespace SS14.Client.Interfaces.UserInterface
{
    public interface IUserInterfaceManager
    {
        /// <summary>
        ///     Clears and disposes of all UI components.
        ///     Highly destructive!
        /// </summary>
        void DisposeAllComponents();

        Control StateRoot { get; }

        /// <summary>
        ///     The "root" control to which all other controls are parented,
        ///     potentially indirectly.
        /// </summary>
        Control RootControl { get; }

        void Initialize();

        void Popup(string contents, string title="Alert!");

        void UnhandledKeyDown(KeyEventArgs args);

        void UnhandledKeyUp(KeyEventArgs args);

        void UnhandledMouseDown(MouseButtonEventArgs args);

        void UnhandledMouseUp(MouseButtonEventArgs args);
    }
}
