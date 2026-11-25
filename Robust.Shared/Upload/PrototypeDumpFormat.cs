using System.Collections.Generic;
using System.Text;
using Robust.Shared.Network.Transfer;

namespace Robust.Shared.Upload;

/// <summary>
///     Wire format for prototype dumps sent over <see cref="ITransferManager"/>.
///     The stream starts with a little-endian ack key, prototype count and sequential flag,
///     followed by length-prefixed prototypes.
/// </summary>
internal static class PrototypeDumpFormat
{
    public const int LengthSize = sizeof(int);

    public static long GetPayloadSize(long currentBytes, string data)
    {
        return currentBytes + LengthSize + Encoding.UTF8.GetByteCount(data);
    }

    public static bool TryGetPayloadLength(IReadOnlyList<string> prototypes, out int length)
    {
        long total = LengthSize * 3;
        for (var i = 0; i < prototypes.Count; i++)
        {
            total += LengthSize + Encoding.UTF8.GetByteCount(prototypes[i]);
            if (total > int.MaxValue)
            {
                length = 0;
                return false;
            }
        }

        length = (int) total;
        return true;
    }
}
