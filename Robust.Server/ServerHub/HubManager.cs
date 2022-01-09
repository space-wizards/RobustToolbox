using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Server.ServerHub;

internal sealed class HubManager
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ILogManager _log = default!;

    private ISawmill _sawmill = default!;

    private string? _advertiseUrl;
    private string _masterUrl = "";
    private TimeSpan _nextPing;
    private TimeSpan _interval;

    private bool _active;
    private bool _firstAdvertisement = true;
    private readonly HttpClient _httpClient;

    public HubManager()
    {
        _httpClient = new HttpClient();

        var assembly = typeof(HubManager).Assembly.GetName();
        if (assembly is { Name: { } name, Version: { } version })
            _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(name, version.ToString()));
    }

    public async void Start()
    {
        _sawmill = _log.GetSawmill("hub");

        var activate = _cfg.GetCVar(CVars.HubAdvertise);
        if (!activate)
            return;

        _cfg.OnValueChanged(CVars.HubAdvertiseInterval, i => _interval = TimeSpan.FromSeconds(i), true);
        _cfg.OnValueChanged(CVars.HubMasterUrl, s => _masterUrl = s, true);

        var url = _cfg.GetCVar(CVars.HubServerUrl);
        if (string.IsNullOrEmpty(url))
        {
            _sawmill.Info("hub.server_url unset. Trying to determine IP address automatically...");
            try
            {
                url = await GuessAddress();
            }
            catch (Exception e)
            {
                _sawmill.Log(LogLevel.Error, e, "Failed to determine address for hub advertisement!");
                return;
            }

            _sawmill.Info("Guessed server address to be {ServerHubAddress}", url);
        }

        _active = true;
        _advertiseUrl = url;
    }

    public void Heartbeat()
    {
        if (!_active || _advertiseUrl == null)
            return;

        if (_nextPing > _timing.RealTime)
            return;

        _nextPing = _timing.RealTime + _interval;

        SendPing();
    }

    private async void SendPing()
    {
        DebugTools.AssertNotNull(_advertiseUrl);

        var apiUrl = $"{_masterUrl}api/servers/advertise";

        try
        {
            using var response = await _httpClient.PostAsJsonAsync(apiUrl, new AdvertiseRequest(_advertiseUrl!));

            if (!response.IsSuccessStatusCode)
            {
                var errorText = await response.Content.ReadAsStringAsync();
                _sawmill.Log(
                    LogLevel.Error,
                    "Error status while advertising server: [{StatusCode}] {Response}",
                    response.StatusCode,
                    errorText);
                return;
            }

            if (_firstAdvertisement)
            {
                _sawmill.Info("Successfully advertised to hub with address {ServerHubAddress}", _advertiseUrl);
                _firstAdvertisement = false;
            }
        }
        catch (Exception e)
        {
            _sawmill.Log(LogLevel.Error, e, $"Exception while trying to advertise server to hub");
        }
    }

    private async Task<string?> GuessAddress()
    {
        var ipifyUrl = _cfg.GetCVar(CVars.HubIpifyUrl);

        var req = await _httpClient.GetFromJsonAsync<IpResponse>(ipifyUrl);

        return $"ss14://{req!.Ip}:{_cfg.GetCVar(CVars.NetPort)}/";
    }

    private sealed record IpResponse(string Ip);

    // ReSharper disable once NotAccessedPositionalProperty.Local
    private sealed record AdvertiseRequest(string Address);
}
