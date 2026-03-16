using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Robust.Shared.Network;

public static class HttpManagerExtensions
{
    extension(IHttpManager http)
    {
        public async Task<Stream?> TryGetStreamAsync(Uri uri, CancellationToken cancel)
        {
            try
            {
                return await http.GetStreamAsync(uri, cancel);
            }
            catch
            {
                return null;
            }
        }

        public async Task<string?> TryGetStringAsync(Uri uri, CancellationToken cancel)
        {
            try
            {
                return await http.GetStringAsync(uri, cancel);
            }
            catch
            {
                return null;
            }
        }

        public async Task<byte[]?> TryGetByteArrayAsync(Uri uri, CancellationToken cancel)
        {
            try
            {
                return await http.GetByteArrayAsync(uri, cancel);
            }
            catch
            {
                return null;
            }
        }

        public async Task<T?> TryGetFromJsonAsync<T>(Uri uri, CancellationToken cancel)
        {
            try
            {
                return await http.GetFromJsonAsync<T>(uri, cancel);
            }
            catch
            {
                return default;
            }
        }

        public async Task TryCopyToAsync(Uri uri, Stream stream, CancellationToken cancel)
        {
            try
            {
                await http.CopyToAsync(uri, stream, cancel);
            }
            catch
            {
                // ignored
            }
        }
    }
}
