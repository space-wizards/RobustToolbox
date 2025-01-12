using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Robust.Client.UserInterface;
using SDL3;

namespace Robust.Client.Graphics.Clyde;

internal partial class Clyde
{
    private sealed partial class Sdl3WindowingImpl : IFileDialogManager
    {
        public async Task<Stream?> OpenFile(FileDialogFilters? filters = null)
        {
            var fileName = await ShowFileDialogOfType(SDL.SDL_FILEDIALOG_OPENFILE, filters);
            if (fileName == null)
                return null;

            return File.OpenRead(fileName);
        }

        public async Task<(Stream fileStream, bool alreadyExisted)?> SaveFile(FileDialogFilters? filters = null, bool truncate = true)
        {
            var fileName = await ShowFileDialogOfType(SDL.SDL_FILEDIALOG_SAVEFILE, filters);
            if (fileName == null)
                return null;

            try
            {
                return (File.Open(fileName, truncate ? FileMode.Truncate : FileMode.Open), true);
            }
            catch (FileNotFoundException)
            {
                return (File.Open(fileName, FileMode.Create), false);
            }
        }

        private unsafe Task<string?> ShowFileDialogOfType(int type, FileDialogFilters? filters)
        {
            var props = SDL.SDL_CreateProperties();

            SDL.SDL_DialogFileFilter* filtersAlloc = null;
            if (filters != null)
            {
                filtersAlloc = (SDL.SDL_DialogFileFilter*)NativeMemory.Alloc(
                    (UIntPtr)filters.Groups.Count,
                    (UIntPtr)sizeof(SDL.SDL_DialogFileFilter));

                SDL.SDL_SetNumberProperty(props, SDL.SDL_PROP_FILE_DIALOG_NFILTERS_NUMBER, filters.Groups.Count);
                SDL.SDL_SetPointerProperty(props, SDL.SDL_PROP_FILE_DIALOG_FILTERS_POINTER, (nint)filtersAlloc);

                // All these mallocs aren't gonna win any performance awards, but oh well.
                for (var i = 0; i < filters.Groups.Count; i++)
                {
                    var (name, pattern) = ConvertFilterGroup(filters.Groups[i]);
                    filtersAlloc[i].name = StringToNative(name);
                    filtersAlloc[i].pattern = StringToNative(pattern);
                }
            }

            var task = ShowFileDialogWithProperties(type, props);

            SDL.SDL_DestroyProperties(props);

            if (filtersAlloc != null)
            {
                for (var i = 0; i < filters!.Groups.Count; i++)
                {
                    var filter = filtersAlloc[i];
                    NativeMemory.Free(filter.name);
                    NativeMemory.Free(filter.pattern);
                }
            }

            return task;
        }

        private static unsafe byte* StringToNative(string str)
        {
            var byteCount = Encoding.UTF8.GetByteCount(str);

            var mem = (byte*) NativeMemory.Alloc((nuint)(byteCount + 1));
            Encoding.UTF8.GetBytes(str, new Span<byte>(mem, byteCount));
            mem[byteCount] = 0; // null-terminate

            return mem;
        }

        private (string name, string pattern) ConvertFilterGroup(FileDialogFilters.Group group)
        {
            var name = string.Join(", ", group.Extensions.Select(e => $"*.{e}"));
            var pattern = string.Join(";", group.Extensions);
            return (name, pattern);
        }

        private unsafe Task<string?> ShowFileDialogWithProperties(int type, uint properties)
        {
            var tcs = new TaskCompletionSource<string?>();

            var gcHandle = GCHandle.Alloc(new FileDialogState
            {
                Parent = this,
                Tcs = tcs
            });

            SDL.SDL_ShowFileDialogWithProperties(
                type,
                &FileDialogCallback,
                (void*)GCHandle.ToIntPtr(gcHandle),
                properties);

            return tcs.Task;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static unsafe void FileDialogCallback(void* userdata, byte** filelist, int filter)
        {
            var stateHandle = GCHandle.FromIntPtr((IntPtr)userdata);
            var state = (FileDialogState)stateHandle.Target!;
            stateHandle.Free();

            if (filelist == null)
            {
                // Error
                state.Parent._sawmill.Error("File dialog failed: {error}", SDL.SDL_GetError());
                state.Tcs.SetResult(null);
                return;
            }

            // Handles null (cancelled/none selected) transparently.
            var str = Marshal.PtrToStringUTF8((nint) filelist[0]);
            state.Tcs.SetResult(str);
        }

        private sealed class FileDialogState
        {
            public required Sdl3WindowingImpl Parent;
            public required TaskCompletionSource<string?> Tcs;
        }
    }
}
