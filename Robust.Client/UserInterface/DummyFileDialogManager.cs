using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using TerraFX.Interop.Windows;

namespace Robust.Client.UserInterface
{
    /// <summary>
    ///     Treats ever file dialog operation as cancelled.
    /// </summary>
    internal sealed class DummyFileDialogManager : IFileDialogManager
    {
        public Task<Stream?> OpenFile(FileDialogFilters? filters = null)
        {
            return Task.FromResult<Stream?>(null);
        }

        public async IAsyncEnumerable<FileInFolder> OpenFolder()
        {
            yield break;
        }
        public Task<(Stream fileStream, bool alreadyExisted)?> SaveFile(FileDialogFilters? filters = null)
        {
            return Task.FromResult<(Stream fileStream, bool alreadyExisted)?>(null);
        }
    }
}
