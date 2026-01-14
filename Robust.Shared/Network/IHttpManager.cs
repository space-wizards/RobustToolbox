using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Robust.Shared.Network;

public interface IHttpManager
{
    Task<Stream> GetStreamAsync(Uri uri, CancellationToken cancel);

    Task<string> GetStringAsync(Uri uri, CancellationToken cancel);

    Task<T?> GetFromJsonAsync<T>(Uri uri, CancellationToken cancel = default);
}
