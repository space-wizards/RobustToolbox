using System.IO;
using System.Threading.Tasks;
using Robust.Client.Graphics;

namespace Robust.Client.UserInterface;

/// <summary>
///     Manager for opening of file dialogs.
/// </summary>
/// <remarks>
///     File dialogs are native to the OS being ran on.
///     All operations are asynchronous to prevent locking up the main thread while the user makes his pick.
/// </remarks>
[NotContentImplementable]
public interface IFileDialogManager
{
    /// <summary>
    /// Open a file dialog used for opening a single file and getting its filename.
    /// </summary>
    /// <returns>
    /// The file stream and name for the file the user opened.
    /// <see langword="null" /> if the user canceled the action.
    /// </returns>
    /// <param name="filters">Filters for file types that the user can select.</param>
    /// <param name="access">What access is desired from the file operation.</param>
    /// <param name="share">
    /// What sharing mode is desired from the file operation.
    /// If null is provided and <paramref name="access"/> is <see cref="FileAccess.Read"/>,
    /// <see cref="FileShare.Read"/> is selected, otherwise <see cref="FileShare.None"/>.
    /// </param>
    Task<(Stream?, string?)> GetFileAndName(
        FileDialogFilters? filters = null,
        FileAccess access = FileAccess.ReadWrite,
        FileShare? share = null);

    /// <summary>
    /// Open a file dialog used for getting the file name of the selected file.
    /// </summary>
    /// <returns>
    /// The file name for the file the user opened.
    /// <see langword="null" /> if the user canceled the action.
    /// </returns>
    /// <inheritdoc cref = "GetFileAndName"/>
    Task<string?> GetName(
        FileDialogFilters? filters = null,
        FileAccess access = FileAccess.ReadWrite,
        FileShare? share = null);

    /// <summary>
    /// Open a file dialog used for opening the selected file.
    /// </summary>
    /// <returns>
    /// The file stream for the file the user opened.
    /// <see langword="null" /> if the user cancelled the action.
    /// </returns>
    /// <inheritdoc cref = "GetFileAndName"/>
    Task<Stream?> OpenFile(
        FileDialogFilters? filters = null,
        FileAccess access = FileAccess.ReadWrite,
        FileShare? share = null);

    /// <summary>
    ///  Open a file dialog used for saving a single file.
    /// </summary>
    /// <returns>
    /// The file stream the user chose to save to, and whether the file already existed.
    /// Null if the user canceled the action.
    /// </returns>
    /// <param name="truncate">Should we truncate an existing file to 0-size then write or append.</param>
    /// <inheritdoc cref = "GetFileAndName"/>
    Task<(Stream fileStream, bool alreadyExisted)?> SaveFile(
        FileDialogFilters? filters = null,
        bool truncate = true,
        FileAccess access = FileAccess.ReadWrite,
        FileShare share = FileShare.None);
}

/// <summary>
/// Internal implementation interface used to connect <see cref="FileDialogManager"/> and <see cref="IClydeInternal"/>.
/// </summary>
internal interface IFileDialogManagerImplementation
{
    Task<string?> OpenFile(FileDialogFilters? filters);
    Task<string?> SaveFile(FileDialogFilters? filters);
}
