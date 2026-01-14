using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Robust.Shared.Network;

public sealed class HttpManager : IHttpManagerInternal
{
    private readonly HttpClient _client = new();

    void IHttpManagerInternal.Shutdown()
    {
        _client.CancelPendingRequests();
    }

    public async Task<Stream> GetStreamAsync(Uri uri, CancellationToken cancel = default)
    {
        return await _client.GetStreamAsync(uri, cancel);
    }

    public async Task<string> GetStringAsync(Uri uri, CancellationToken cancel = default)
    {
        return await _client.GetStringAsync(uri, cancel);
    }

    public async Task<T?> GetFromJsonAsync<T>(Uri uri, CancellationToken cancel = default)
    {
        return await _client.GetFromJsonAsync<T>(uri, cancel);
    }
}
