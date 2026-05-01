using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using OpenTK.Audio.OpenAL;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared;
using Robust.Shared.Localization;
using Robust.Shared.Timing;

namespace Robust.Client.Audio;

internal sealed partial class AudioManager
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private unsafe delegate byte AlcReopenDeviceSOFTDelegate(IntPtr device, byte* deviceName, int* attribs);
    [Shared.IoC.Dependency] private readonly IGameTiming _timing = default!;
    [Shared.IoC.Dependency] private readonly IUserInterfaceManager _uiManager = default!;
    [Shared.IoC.Dependency] private readonly ILocalizationManager _loc = default!;
    private AlcReopenDeviceSOFTDelegate? _alcReopenDeviceSOFT;
    private bool _hasReopenExtension;
    private string _currentDeviceName = string.Empty;
    private const int AlcConnected = 0x313;
    private TimeSpan _nextDevicePoll;
    private static readonly TimeSpan DevicePollInterval = TimeSpan.FromSeconds(2);
    private DefaultWindow? _confirmationWindow;
    private string? _previousDeviceName;
    private bool _awaitingConfirmation;

    public event Action? AudioDeviceChanged;

    private void InitializeDeviceSwitch()
    {
        _currentDeviceName = _cfg.GetCVar(CVars.AudioDevice);

        if (HasAlDeviceExtension("ALC_SOFT_reopen_device"))
        {
            var ptr = ALC.GetProcAddress(_openALDevice, "alcReopenDeviceSOFT");
            if (ptr != IntPtr.Zero)
            {
                _alcReopenDeviceSOFT = Marshal.GetDelegateForFunctionPointer<AlcReopenDeviceSOFTDelegate>(ptr);
                _hasReopenExtension = true;
                OpenALSawmill.Debug("ALC_SOFT_reopen_device extension available.");
            }
        }

        if (!_hasReopenExtension)
        {
            OpenALSawmill.Debug("ALC_SOFT_reopen_device extension not available. Live device switching disabled.");
        }

        _nextDevicePoll = _timing.RealTime + DevicePollInterval;
    }

    public IReadOnlyList<string> GetAudioDevices()
    {
        try
        {
            var devices = ALC.GetString(ALDevice.Null, AlcGetStringList.AllDevicesSpecifier);
            if (devices is { Count: > 0 })
                return devices.ToList();
        }
        catch
        {
            // Extension not available, fall through.
        }

        try
        {
            var devices = ALC.GetString(ALDevice.Null, AlcGetStringList.DeviceSpecifier);
            if (devices != null)
                return devices.ToList();
        }
        catch
        {
            // Fall through.
        }

        return Array.Empty<string>();
    }

    public string GetCurrentDeviceName() => _currentDeviceName;

    public bool CanSwitchDevice() => _hasReopenExtension;

    public bool RequestDeviceSwitch(string? deviceName)
    {
        deviceName ??= string.Empty;

        if (_awaitingConfirmation)
        {
            OpenALSawmill.Warning("Device switch already pending confirmation.");
            return false;
        }

        if (!_hasReopenExtension)
        {
            OpenALSawmill.Warning("Live device switching not supported on this platform.");
            return false;
        }

        if (deviceName == _currentDeviceName)
            return false;

        _previousDeviceName = _currentDeviceName;

        if (!ReopenDevice(deviceName))
        {
            OpenALSawmill.Error("Failed to switch audio device to '{0}'.", deviceName);
            return false;
        }

        _awaitingConfirmation = true;
        ShowDeviceConfirmation(deviceName);
        return true;
    }

    private unsafe bool ReopenDevice(string? deviceName)
    {
        if (_alcReopenDeviceSOFT == null)
            return false;

        byte result;

        if (string.IsNullOrEmpty(deviceName))
        {
            result = _alcReopenDeviceSOFT((IntPtr)_openALDevice, null, null);
        }
        else
        {
            var nameBytes = Encoding.UTF8.GetBytes(deviceName + '\0');
            fixed (byte* pName = nameBytes)
            {
                result = _alcReopenDeviceSOFT((IntPtr)_openALDevice, pName, null);
            }
        }

        if (result == 0)
        {
            _checkAlcError(_openALDevice);
            return false;
        }

        _currentDeviceName = deviceName ?? string.Empty;

        _alcDeviceExtensions.Clear();
        var extString = ALC.GetString(_openALDevice, AlcGetString.Extensions) ?? "";
        foreach (var extension in extString.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            _alcDeviceExtensions.Add(extension);

        IsEfxSupported = HasAlDeviceExtension("ALC_EXT_EFX");

        OpenALSawmill.Info("Audio device switched to '{0}'.",
            string.IsNullOrEmpty(deviceName) ? "System Default" : deviceName);
        return true;
    }

    private void ShowDeviceConfirmation(string deviceName)
    {
        CloseConfirmationWindow();

        var window = new DefaultWindow
        {
            Title = _loc.GetString("audio-device-confirm-title"),
            Resizable = false,
        };

        var vBox = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 10,
        };

        var messageText = string.IsNullOrEmpty(deviceName)
            ? _loc.GetString("audio-device-confirm-default")
            : _loc.GetString("audio-device-confirm-message", ("device", deviceName));

        var label = new Label { Text = messageText };
        vBox.AddChild(label);

        var buttonBox = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 10,
            HorizontalAlignment = Control.HAlignment.Center,
        };

        var keepButton = new Button
        {
            Text = _loc.GetString("audio-device-confirm-keep"),
            MinWidth = 100,
        };
        keepButton.OnPressed += _ => ConfirmDeviceSwitch();

        var revertButton = new Button
        {
            Text = _loc.GetString("audio-device-confirm-revert"),
            MinWidth = 100,
        };
        revertButton.OnPressed += _ => RevertDeviceSwitch();

        buttonBox.AddChild(keepButton);
        buttonBox.AddChild(revertButton);
        vBox.AddChild(buttonBox);

        window.Contents.AddChild(vBox);
        window.OnClose += RevertDeviceSwitch;

        _confirmationWindow = window;
        window.OpenCentered();
    }

    private void ConfirmDeviceSwitch()
    {
        _awaitingConfirmation = false;

        _cfg.SetCVar(CVars.AudioDevice, _currentDeviceName);
        _cfg.SaveToFile();

        CloseConfirmationWindow();
        AudioDeviceChanged?.Invoke();
    }

    private void RevertDeviceSwitch()
    {
        if (!_awaitingConfirmation)
            return;

        _awaitingConfirmation = false;
        var previous = _previousDeviceName ?? string.Empty;

        if (!ReopenDevice(previous))
        {
            OpenALSawmill.Error("Failed to revert audio device to '{0}'. Falling back to default.", previous);
            ReopenDevice(null);
        }

        CloseConfirmationWindow();
        AudioDeviceChanged?.Invoke();
    }

    private void CloseConfirmationWindow()
    {
        if (_confirmationWindow == null)
            return;

        _confirmationWindow.OnClose -= RevertDeviceSwitch;
        _confirmationWindow.Close();
        _confirmationWindow.Orphan();
        _confirmationWindow = null;
    }

    public void PollAudioDeviceStatus()
    {
        if (_openALDevice == IntPtr.Zero)
            return;

        if (_timing.RealTime < _nextDevicePoll)
            return;

        _nextDevicePoll = _timing.RealTime + DevicePollInterval;

        var connected = new int[1];
        ALC.GetInteger(_openALDevice, (AlcGetInteger)AlcConnected, 1, connected);

        if (connected[0] == 0)
        {
            OpenALSawmill.Warning("Audio device disconnected. Switching to system default.");

            if (_awaitingConfirmation)
            {
                _awaitingConfirmation = false;
                CloseConfirmationWindow();
            }

            if (ReopenDevice(null))
            {
                _cfg.SetCVar(CVars.AudioDevice, string.Empty);
                _cfg.SaveToFile();
            }

            AudioDeviceChanged?.Invoke();
        }
    }
}
