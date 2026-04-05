using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Network;

namespace Robust.Client.Utility;

internal sealed class PiShockManager : IPiShockManager, IPostInjectInit
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly HttpClientHolder _http = default!;
    [Dependency] private readonly ILogManager _logManager = default!;

    private ISawmill _sawmill = default!;

    private bool _enabled;
    private string _username = string.Empty;
    private string _apiKey = string.Empty;
    private string _shareCode = string.Empty;
    private int _maxIntensity;
    private int _maxDuration;

    private const string ApiUrl = "https://do.pishock.com/api/apioperate";
    private const string AppName = "RobustToolbox";

    public void PostInject()
    {
        _sawmill = _logManager.GetSawmill("pishock");

        _cfg.OnValueChanged(CVars.PiShockEnabled, v => _enabled = v, invokeImmediately: true);
        _cfg.OnValueChanged(CVars.PiShockUsername, v => _username = v, invokeImmediately: true);
        _cfg.OnValueChanged(CVars.PiShockApiKey, v => _apiKey = v, invokeImmediately: true);
        _cfg.OnValueChanged(CVars.PiShockShareCode, v => _shareCode = v, invokeImmediately: true);
        _cfg.OnValueChanged(CVars.PiShockMaxIntensity, v => _maxIntensity = Math.Clamp(v, 1, 100), invokeImmediately: true);
        _cfg.OnValueChanged(CVars.PiShockMaxDuration, v => _maxDuration = Math.Clamp(v, 1, 15), invokeImmediately: true);
    }

    public void TryOperate(PiShockOp op, int intensity, int duration)
    {
        if (!_enabled)
            return;

        if (string.IsNullOrEmpty(_username) || string.IsNullOrEmpty(_apiKey) || string.IsNullOrEmpty(_shareCode))
        {
            _sawmill.Warning("piShock is enabled but credentials are not fully configured.");
            return;
        }

        intensity = Math.Clamp(intensity, 1, _maxIntensity);
        duration = Math.Clamp(duration, 1, _maxDuration);

        _ = PostOperationAsync(_username, _apiKey, _shareCode, op, intensity, duration);
    }

    private async Task PostOperationAsync(
        string username, string apiKey, string shareCode,
        PiShockOp op, int intensity, int duration)
    {
        try
        {
            var payload = JsonSerializer.Serialize(new
            {
                Username = username,
                Apikey = apiKey,
                Code = shareCode,
                Name = AppName,
                Op = (int) op,
                Duration = duration,
                Intensity = intensity,
            });

            using var content = new StringContent(payload, Encoding.UTF8, "application/json");
            var response = await _http.Client.PostAsync(ApiUrl, content);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                _sawmill.Warning($"piShock API returned HTTP {(int) response.StatusCode}: {body}");
            else if (body != "Operation Successful.")
                _sawmill.Warning($"piShock API: {body}");
        }
        catch (Exception ex)
        {
            _sawmill.Error($"Failed to send piShock operation: {ex.Message}");
        }
    }
}
