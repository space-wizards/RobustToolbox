using System;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace Robust.Shared.Network
{
    internal static class HWId
    {
        public static byte[] Calc()
        {
            if (OperatingSystem.IsWindows())
            {
                var a = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_BaseBoard");
                var obj = a.Get().Cast<ManagementObject>().First();
                var prop = obj.Properties["SerialNumber"];
                return Hash((string) prop.Value);
            }

            return Array.Empty<byte>();
        }

        private static byte[] Hash(string str)
        {
            using var hasher = SHA256.Create();
            return hasher.ComputeHash(Encoding.UTF8.GetBytes(str));
        }
    }
}
