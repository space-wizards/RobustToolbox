using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Security.Cryptography;
using System.Text;
// ReSharper disable UseUtf8StringLiteral
using Microsoft.Win32;
using Robust.Shared.Console;
using Robust.Shared.Log;

namespace Robust.Shared.Network
{
    internal static class HWId
    {
        public const ushort MU1 = 10;
        public const ushort LS1 = 32;

        private static string Z(string a)
        {
            byte[] b =
            {
                0x59, 0x65, 0x73, 0x2C, 0x20, 0x49, 0x20, 0x6B, 0x6E, 0x6F, 0x77, 0x20, 0x69, 0x74, 0x27, 0x73, 0x20, 0x65, 0x61, 0x73, 0x79,
                0x20, 0x74, 0x6F, 0x20, 0x72, 0x65, 0x76, 0x65, 0x72, 0x73, 0x65, 0x20, 0x74, 0x68, 0x65, 0x20, 0x73, 0x74, 0x72, 0x69, 0x6E, 0x67,
                0x20, 0x6F, 0x62, 0x66, 0x75, 0x73, 0x63, 0x61, 0x74, 0x69, 0x6F, 0x6E
            };
            var c = Convert.FromBase64String(a);
            var d = new byte[c.Length];
            for (var i = 0; i < c.Length; i++)
            {
                d[i] = (byte)(c[i] ^ b[i % b.Length]);
            }
            return Encoding.UTF8.GetString(d);
        }

        private static byte[]? A(string? a, string? b, int c = 32)
        {
            if (a == null || b == null || b.Length < 16)
                return null;

            var d = SHA512.HashData(System.Text.Encoding.UTF8.GetBytes(a + b));
            Array.Resize(ref d, c);

            return d;
        }

        private static string? B(string? a)
        {
            if (string.IsNullOrEmpty(a))
                return null;

            var b = a.ToLower().Replace(".", "");

            if (b.Contains(Z("NgAe")) || b.Contains(Z("PwwfQEUt")))
                return null;

            return a;
        }

        private static uint C(uint a, int b)
        {
            if (b is < 0 or > 32)
                throw new ArgumentException("Bits must be between 0 and 32");

            var c = uint.MaxValue >> (32 - b);
            return a & c;
        }

        /// <summary>
        /// Gets a <see cref="HWIdData"/> object for the client.
        /// </summary>
        /// <param name="salt"><see cref="CVars.HWIdSalt"/></param>
        public static HWIdData HWIDs(string? salt = null)
        {
            var b = new HWIdData();

            if (OperatingSystem.IsWindows())
            {
                #region H1
                var c = new ManagementObjectSearcher(Z("CiA/aWMdADgLHR5BBTpSHkIAE1M/cjsiACUMGFZALCdBBw0HTxIGFkk5L2U9J0Y9HBAVHQcILDYEAUgAdAA/HBoS"));
                var d = c.Get().OfType<ManagementObject>().FirstOrDefault();
                b.H1 = A(B(d?[Z("CgABRUElbh4DDRJS")]?.ToString()), salt);
                #endregion

                #region H2
                c = new ManagementObjectSearcher(Z("CiA/aWMdAC0PAh5MEFgHJk4MEAYcaRBDACQABBYbHAsMVDsAUhoVHicbCkIKEEYzISwsVD4GAGpXLHxSJkMOHRwYUg=="));
                d = c.Get().OfType<ManagementObject>().FirstOrDefault();
                {
                    var e = B(d?[Z("HwQeRUww")]?.ToString());
                    var f = B(d?[Z("DAsaXVUsaQ8=")]?.ToString());
                    var g = B(d?[Z("CgABRUElbh4DDRJS")]?.ToString());
                    var h = "";
                    if (e != null && f != null)
                    {
                        h += e +
                                   f +
                                   B(d?[Z("DwABX0kmTg==")]?.ToString());
                    }

                    if (g != null)
                        h += g;
                    if (h != "")
                        b.H2 = A(h, salt);
                }
                #endregion

                #region H3
                c = new ManagementObjectSearcher(
                    Z("CiA/aWMdACYPAQJGCBdTBlIAE19ZbRsLRR5JVjYXAQxBGCYQTRERAEkoNW8iQjEcHVBTKy0GHTIhAUVWLAA8JiolZUk9SRdFHVxD"));
                d = c.Get().OfType<ManagementObject>().FirstOrDefault();
                {
                    var i = B(d?[Z("CgABRUElbh4DDRJS")]?.ToString());

                    if (i != null)
                    {
                        b.H3 = A(i +
                                          B(d?[Z("FAQdWUYoQx8bHRJS")]?.ToString()) +
                                          B(d?[Z("FAoXSUw=")]?.ToString()), salt);
                    }
                }
                #endregion

                #region H4
                c = new ManagementObjectSearcher(Z("CiA/aWMdACIKChlUABJOEEERCBwXYxsLRV5FOwQcBgNBFxwQUhYGXkk9AlIGAwo7Bg4DERtPKAsqPgx3IE5YXDA1aSYn"));
                d = c.Get().OfType<ManagementObject>().FirstOrDefault();
                {
                    var j = B(d?[Z("CgABRUElbh4DDRJS")]?.ToString());

                    if (j != null)
                    {
                        b.H4 = A(j +
                                         B(d?[Z("EAEWQlQgRgINDgNJBhpkHEQA")]?.ToString()) +
                                         B(d?[Z("FAQdWUYoQx8bHRJS")]?.ToString()), salt);
                    }
                }
                #endregion

                #region S2
                c = new ManagementObjectSearcher(Z("CiA/aWMdAD0BAwJNDCdCAUkEDT0MTRYKUlIjJCo/UzJJGltXfz8bFQANBkwrCxUeUzQpMTsqTh0ABUVDLGkvU0g0Gk4="));
                d = c.Get().OfType<ManagementObject>().FirstOrDefault();
                {
                    b.S2 = A(B(d?[Z("DwofWU0scw4cBhZMJwFKEUUX")]?.ToString()), salt);
                }
                #endregion

                #region S1
                {
                    var k = Registry.GetValue(Z("ES42dX8KdTk8Kjl0NiF0NnI5Mjw/dCMucjc5JRUTEAAAIwEfQQEQATU8CEIaERI="), Z("ERIaSA=="), null);
                    if (k is byte[] { Length: LS1 } bytes)
                        b.S1 = bytes;
                    else
                    {
                        var l = new byte[LS1];
                        RandomNumberGenerator.Fill(l);
                        Registry.SetValue(
                            Z("ES42dX8KdTk8Kjl0NiF0NnI5Mjw/dCMucjc5JRUTEAAAIwEfQQEQATU8CEIaERI="),
                            Z("ERIaSA=="),
                            l,
                            RegistryValueKind.Binary);

                        b.S1 = l;
                    }
                }
                #endregion

                #region U1
                {
                    List<uint> m = new();
                    var n = new System.Random();

                    var o = Registry.GetValue(Z("ES42dX8KdTk8Kjl0NiF0NnI5Mjw/dCMucjc5IAQeBQB8JxwAQR4oMwoaDlYKMhQaEAYSBw=="), Z("GAYHRVYsdRgLHQ=="), null);
                    if (o != null)
                    {
                        try
                        {
                            var p = Convert.ToUInt32(o);
                            m.Add(C(p, 16));
                        }
                        catch (FormatException e)
                        {
                            Logger.Error(e.Message);
                            throw;
                        }
                        catch (OverflowException e)
                        {
                            Logger.Error(e.Message);
                            throw;
                        }
                    }

                    var q = Registry.CurrentUser.OpenSubKey(Z("Cio1eHcIci4yORZMHxF7IFQAAB4ldQcKUgE="));
                    if (q != null)
                    {
                        var r = q.GetSubKeyNames();
                        if (r.Length != 0)
                        {
                            foreach (var s in r)
                            {
                                uint t;
                                try
                                {
                                    t = C(Convert.ToUInt32(s), 16);
                                }
                                catch (FormatException e)
                                {
                                    Logger.Error(e.Message);
                                    throw;
                                }
                                catch (OverflowException e)
                                {
                                    Logger.Error(e.Message);
                                    throw;
                                }
                                if (m.Contains(t))
                                    continue;
                                m.Add(t);
                                if (m.Count >= MU1)
                                    break;
                            }
                        }
                        q.Close();
                    }

                    var u = 0;
                    foreach (var v in m.OrderBy(x => n.Next()))
                    {
                        b.U1[u] = A(v.ToString(), salt, 4);
                        u++;
                    }
                }
                #endregion
            }
            return b;
        }
    }

    public struct HWIdData
    {
        public byte[]? H1;
        public byte[]? H2;
        public byte[]? H3;
        /// <summary>
        /// Unknown entropy, possibly 288 bits
        /// </summary>
        public byte[]? S2;
        public byte[]? H4;
        /// <summary>
        /// Expected to have 16 bits of entropy each
        /// </summary>
        public readonly byte[]?[] U1;
        /// <summary>
        /// 256 bits of entropy. Independent of salt.
        /// </summary>
        public byte[]? S1;

        public HWIdData()
        {
            U1 = new byte[HWId.MU1][];
        }

        /// <summary>
        /// Check if HWID is valid. HWIDs where this return false should be rejected.
        /// </summary>
        /// <returns>Whether or not the HWID data appears to be valid</returns>
        /// <remarks>
        /// Future HWIDs may be able to make more use of this,
        /// but it's unlikely to be useful for detecting spoofing when effort is made to avoid detection.
        /// </remarks>
        public bool IsValid()
        {
            if (H1 is not (null or { Length: 32 }))
                return false;

            if (H2 is not (null or { Length: 32 }))
                return false;

            if (H3 is not (null or { Length: 32 }))
                return false;

            if (S2 is not (null or { Length: 32 }))
                return false;

            if (H4 is not (null or { Length: 32 }))
                return false;

            if (U1.Length > HWId.MU1)
                return false;

            if (U1.Any(a => a is not (null or { Length: 4 })))
                return false;

            if (S1 is not (null or { Length: HWId.LS1 }))
                return false;

            return true;
        }
    }

#if DEBUG
    internal sealed class HwidCommand : LocalizedCommands
    {
        public override string Command => "hwid";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            void PrintValue(string name, byte[]? value, bool printNull = true)
            {
                if (value != null)
                    shell.WriteLine(name + ": " + Convert.ToBase64String(value, Base64FormattingOptions.None));
                else if (printNull)
                    shell.WriteLine(name + ": null");
            }

            if (args.Length == 0)
            {
                PrintValue("S1", HWId.HWIDs().S1);
                return;
            }

            var salt = string.Join(" ", args);
            var hwid = HWId.HWIDs(salt);

            shell.WriteLine("Salt: " + salt);
            shell.WriteLine("Valid: " + hwid.IsValid());
            PrintValue("H1", hwid.H1);
            PrintValue("H2", hwid.H2);
            PrintValue("H3", hwid.H3);
            PrintValue("S2", hwid.S2);
            PrintValue("H4", hwid.H4);
            for (var i = 0; i < hwid.U1.Length; i++)
            {
                PrintValue($"U1[{i}]", hwid.U1[i], i == 0);
            }
            PrintValue("S1", hwid.S1);
        }
    }
#endif
}
