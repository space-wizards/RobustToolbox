using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using TerraFX.Interop.Windows;

namespace Robust.Shared.Utility;

internal static class FileHelper
{
    /// <summary>
    /// Try to open a file for reading. If the file does not exist, the operation fails without exception.
    /// </summary>
    /// <remarks>
    /// This API is not atomic and can thus be vulnerable to TOCTOU attacks. Don't use it if that's relevant.
    /// </remarks>
    /// <param name="path">The path to try to open.</param>
    /// <param name="stream">The resulting file stream.</param>
    /// <returns>True if the file existed and was opened.</returns>
    public static bool TryOpenFileRead(string path, [NotNullWhen(true)] out FileStream? stream)
    {
        // On Windows, the separate File.Exists() call alone adds a ton of weight.
        // The alternative however (opening the file and catching the error) is extremely slow because of .NET exceptions.
        // So we manually call the windows API and make the file handle from that. Problem solved!
        if (OperatingSystem.IsWindows())
            return TryGetFileWindows(path, out stream);

        if (!File.Exists(path))
        {
            stream = null;
            return false;
        }

        stream = File.OpenRead(path);
        return true;
    }

    private static unsafe bool TryGetFileWindows(string path, [NotNullWhen(true)] out FileStream? stream)
    {
        if (path.EndsWith("\\"))
        {
            stream = null;
            return false;
        }

        try
        {
            HANDLE file;
            fixed (char* pPath = path)
            {
                file = Windows.CreateFileW(
                    pPath,
                    Windows.GENERIC_READ,
                    FILE.FILE_SHARE_READ,
                    null,
                    OPEN.OPEN_EXISTING,
                    FILE.FILE_ATTRIBUTE_NORMAL,
                    HANDLE.NULL);
            }

            if (file == HANDLE.INVALID_VALUE)
            {
                var lastError = Marshal.GetLastSystemError();
                if (lastError is ERROR.ERROR_FILE_NOT_FOUND or ERROR.ERROR_PATH_NOT_FOUND)
                {
                    stream = null;
                    return false;
                }

                Marshal.ThrowExceptionForHR(Windows.HRESULT_FROM_WIN32(lastError));
            }

            var sf = new SafeFileHandle(file, ownsHandle: true);
            stream = new FileStream(sf, FileAccess.Read);
            return true;

        }
        catch (UnauthorizedAccessException)
        {
            // UnauthorizedAccessException aka this is a folder not a file because of course that is what that means.
            // Who the fuck though this was the right way of handling that? This should clearly just be a
            // ERROR_FILE_NOT_FOUND or some other result like that,
            // https://github.com/dotnet/runtime/issues/70275
            if (Directory.Exists(path))
            {
                stream = null;
                return false;
            }

            throw;
        }
    }
}
