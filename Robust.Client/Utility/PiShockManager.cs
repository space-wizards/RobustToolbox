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

    private const string ApiUrl = "https://do.pishock.com/api/apioperate";
    private const string AppName = "RobustToolbox";

    public void PostInject()
    {
        _sawmill = _logManager.GetSawmill("pishock");
    }

    public void TryOperate(PiShockOp op, int intensity, int duration)
    {
        if (!_cfg.GetCVar(CVars.PiShockEnabled))
            return;

        var username = _cfg.GetCVar(CVars.PiShockUsername);
        var apiKey = _cfg.GetCVar(CVars.PiShockApiKey);
        var shareCode = _cfg.GetCVar(CVars.PiShockShareCode);

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(shareCode))
        {
            _sawmill.Warning("piShock is enabled but credentials are not fully configured.");
            return;
        }

        var maxIntensity = Math.Clamp(_cfg.GetCVar(CVars.PiShockMaxIntensity), 1, 100);
        var maxDuration = Math.Clamp(_cfg.GetCVar(CVars.PiShockMaxDuration), 1, 15);

        intensity = Math.Clamp(intensity, 1, maxIntensity);
        duration = Math.Clamp(duration, 1, maxDuration);

        _ = PostOperationAsync(username, apiKey, shareCode, op, intensity, duration);
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

            if (!response.IsSuccessStatusCode)
                _sawmill.Warning($"piShock API returned {(int) response.StatusCode} {response.StatusCode}.");
        }
        catch (Exception ex)
        {
            _sawmill.Error($"Failed to send piShock operation: {ex.Message}");
        }
    }
}
