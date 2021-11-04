using System;
using System.Security.Cryptography;
using Microsoft.Win32;
using Robust.Shared.Console;

namespace Robust.Shared.Network
{
    internal static class HWId
    {
        public const int LengthHwid = 32;

        public static byte[] Calc()
        {
            if (OperatingSystem.IsWindows())
            {
                var regKey = Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\Space Wizards\Robust", "Hwid", null);
                if (regKey is byte[] { Length: LengthHwid } bytes)
                    return bytes;

                var newId = new byte[LengthHwid];
                RandomNumberGenerator.Fill(newId);
                Registry.SetValue(
                    @"HKEY_CURRENT_USER\SOFTWARE\Space Wizards\Robust",
                    "Hwid",
                    newId,
                    RegistryValueKind.Binary);

                return newId;
            }

            return Array.Empty<byte>();
        }
    }

#if DEBUG
    internal sealed class HwidCommand : IConsoleCommand
    {
        public string Command => "hwid";
        public string Description => "Returns the current HWID.";
        public string Help => "Returns the current HWID.";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            shell.WriteLine(Convert.ToBase64String(HWId.Calc(), Base64FormattingOptions.None));
        }
    }
#endif
}
