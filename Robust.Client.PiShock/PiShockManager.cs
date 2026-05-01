using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Robust.Client.PiShockHook;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Network;
using Robust.Shared.Reflection;
using Robust.Shared.Timing;

[assembly: PiShockManagerImpl(typeof(Robust.Client.PiShock.PiShockManager))]

namespace Robust.Client.PiShock;

internal sealed class PiShockManager : IPiShockManager, IPiShockManagerHook
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly HttpClientHolder _http = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private ISawmill _sawmill = default!;

    private bool _enabled;
    private string _username = string.Empty;
    private string _apiKey = string.Empty;
    private string _shareCode = string.Empty;
    private int _maxIntensity;
    private int _maxDuration;
    private float _cooldown;

    private TimeSpan _lastOperationTime = TimeSpan.MinValue;

    private const float MinCooldown = 1.0f;

    private const string ApiUrl = "https://do.pishock.com/api/apioperate";
    private const string AppName = "RobustToolbox";

    public Action<PiShockOp, int, int> Operate { get; }

    public PiShockManager()
    {
        Operate = DoOperate;
    }

    public void PreInitialize(IDependencyCollection dependencies)
    {
        var cfg = dependencies.Resolve<IConfigurationManagerInternal>();
        cfg.LoadCVarsFromAssembly(typeof(PiShockManager).Assembly);

        var refl = dependencies.Resolve<IReflectionManager>();
        refl.LoadAssemblies(typeof(PiShockManager).Assembly);

        dependencies.RegisterInstance<IPiShockManager>(this);
        dependencies.InjectDependencies(this, oneOff: true);
    }

    public void Initialize()
    {
        _sawmill = _logManager.GetSawmill("pishock");

        _cfg.OnValueChanged(PCVars.PiShockEnabled, v => _enabled = v, invokeImmediately: true);
        _cfg.OnValueChanged(PCVars.PiShockUsername, v => _username = v, invokeImmediately: true);
        _cfg.OnValueChanged(PCVars.PiShockApiKey, v => _apiKey = v, invokeImmediately: true);
        _cfg.OnValueChanged(PCVars.PiShockShareCode, v => _shareCode = v, invokeImmediately: true);
        _cfg.OnValueChanged(PCVars.PiShockMaxIntensity, v => _maxIntensity = Math.Clamp(v, 1, 100), invokeImmediately: true);
        _cfg.OnValueChanged(PCVars.PiShockMaxDuration, v => _maxDuration = Math.Clamp(v, 1, 15), invokeImmediately: true);
        _cfg.OnValueChanged(PCVars.PiShockCooldown, OnCooldownChanged, invokeImmediately: true);
    }

    private void DoOperate(PiShockOp op, int intensity, int duration)
    {
        if (!_enabled)
            return;

        if (string.IsNullOrEmpty(_username) || string.IsNullOrEmpty(_apiKey) || string.IsNullOrEmpty(_shareCode))
        {
            _sawmill.Warning("piShock is enabled but credentials are not fully configured.");
            return;
        }

        var now = _timing.RealTime;
        if ((now - _lastOperationTime).TotalSeconds < _cooldown)
        {
            _sawmill.Verbose("Operation dropped due to cooldown.");
            return;
        }

        _lastOperationTime = now;

        intensity = Math.Clamp(intensity, 1, _maxIntensity);
        duration = Math.Clamp(duration, 1, _maxDuration);

        _ = PostAsync(_username, _apiKey, _shareCode, op, intensity, duration);
    }

    private void OnCooldownChanged(float value)
    {
        if (value < MinCooldown)
            _sawmill.Warning($"pishock.cooldown {value}s is below the minimum of {MinCooldown}s, clamping.");
        _cooldown = Math.Max(value, MinCooldown);
    }

    private async Task PostAsync(
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
