using System;
using System.IO;
using System.Security.Cryptography;
using Microsoft.Win32;
using Robust.Client.Utility;
using Robust.Shared.IoC;
using Robust.Shared.Network;

namespace Robust.Client.HWId;

internal sealed class BasicHWId : IHWId
{
    [Dependency] private readonly IGameControllerInternal _gameController = default!;

    public const int LengthHwid = 32;

    public byte[] GetLegacy()
    {
        if (OperatingSystem.IsWindows())
            return GetWindowsHWid("Hwid");

        return [];
    }

    public byte[] GetModern()
    {
        byte[] raw;

        if (OperatingSystem.IsWindows())
            raw = GetWindowsHWid("Hwid2");
        else
            raw = GetFileHWid();

        return [0, ..raw];
    }

    private static byte[] GetWindowsHWid(string keyName)
    {
        const string keyPath = @"HKEY_CURRENT_USER\SOFTWARE\Space Wizards\Robust";

        var regKey = Registry.GetValue(keyPath, keyName, null);
        if (regKey is byte[] { Length: LengthHwid } bytes)
            return bytes;

        var newId = new byte[LengthHwid];
        RandomNumberGenerator.Fill(newId);
        Registry.SetValue(
            keyPath,
            keyName,
            newId,
            RegistryValueKind.Binary);

        return newId;
    }

    private byte[] GetFileHWid()
    {
        var path = UserDataDir.GetRootUserDataDir(_gameController);
        var hwidPath = Path.Combine(path, ".hwid");

        var value = ReadHWidFile(hwidPath);
        if (value != null)
            return value;

        value = RandomNumberGenerator.GetBytes(LengthHwid);
        File.WriteAllBytes(hwidPath, value);

        return value;
    }

    private static byte[]? ReadHWidFile(string path)
    {
        try
        {
            var value = File.ReadAllBytes(path);
            if (value.Length == LengthHwid)
                return value;
        }
        catch (FileNotFoundException)
        {
            // First time the file won't exist.
        }

        return null;
    }
}
