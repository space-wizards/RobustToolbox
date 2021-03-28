using System.IO;
using System.Threading.Tasks;

namespace Robust.Client.UserInterface
{
    /// <summary>
    ///     Manager for opening of file dialogs.
    /// </summary>
    /// <remarks>
    ///     File dialogs are native to the OS being ran on.
    ///     All operations are asynchronous to prevent locking up the main thread while the user makes his pick.
    /// </remarks>
    public interface IFileDialogManager
    {
        /// <summary>
        ///     Open a file dialog used for opening a single file.
        /// </summary>
        /// <returns>
        /// The file stream for the file the user opened.
        /// <see langword="null" /> if the user cancelled the action.
        /// </returns>
        Task<Stream?> OpenFile(FileDialogFilters? filters = null);

        /// <summary>
        ///     Open a file dialog used for saving a single file.
        /// </summary>
        /// <returns>
        /// The file stream the user chose to save to, and whether the file already existed.
        /// Null if the user cancelled the action.
        /// </returns>
        Task<(Stream fileStream, bool alreadyExisted)?> SaveFile();
    }
}