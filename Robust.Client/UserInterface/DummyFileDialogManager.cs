using System.Threading.Tasks;
using Robust.Client.Interfaces.UserInterface;

namespace Robust.Client.UserInterface
{
    /// <summary>
    ///     Treats ever file dialog operation as cancelled.
    /// </summary>
    internal sealed class DummyFileDialogManager : IFileDialogManager
    {
        public Task<string> OpenFile(FileDialogFilters filters = null)
        {
            return Task.FromResult<string>(null);
        }

        public Task<string> SaveFile()
        {
            return Task.FromResult<string>(null);
        }

        public Task<string> OpenFolder()
        {
            return Task.FromResult<string>(null);
        }
    }
}
