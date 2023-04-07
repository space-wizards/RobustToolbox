using System;
using Lidgren.Network;
using Robust.Shared.Log;
using Robust.Shared.Serialization;

namespace Robust.Shared.Network.Messages.Handshake;

internal sealed class MsgLoginHWIdResponse : NetMessage
{
    private readonly ISawmill _sawmill = default!;
    public HWIdData HWId;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        HWId = new HWIdData();

        if (buffer.ReadBoolean())
            HWId.H1 = buffer.ReadBytes(32);

        if (buffer.ReadBoolean())
            HWId.H2 = buffer.ReadBytes(32);

        if (buffer.ReadBoolean())
            HWId.H3 = buffer.ReadBytes(32);

        if (buffer.ReadBoolean())
            HWId.S2 = buffer.ReadBytes(32);

        if (buffer.ReadBoolean())
            HWId.H4 = buffer.ReadBytes(32);

        var count = Math.Min(buffer.ReadUInt16(), Network.HWId.MU1);
        for (var i = 0; i < count; i++)
        {
            HWId.U1[i] = buffer.ReadBytes(4);
        }

        if (buffer.ReadBoolean())
            HWId.S1 = buffer.ReadBytes(Network.HWId.LS1);

        if (HWId.IsValid())
            return;
        _sawmill.Warning($"Received an invalid HWID."); // TODO Add useful information. Reject the HWID and connection?
        // The HWID shouldn't be entirely rejected without also rejecting the connection.
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        if (HWId.H1 != null)
        {
            buffer.Write(true);
            buffer.Write(HWId.H1);
        }
        else
        {
            buffer.Write(false);
        }

        if (HWId.H2 != null)
        {
            buffer.Write(true);
            buffer.Write(HWId.H2);
        }
        else
        {
            buffer.Write(false);
        }

        if (HWId.H3 != null)
        {
            buffer.Write(true);
            buffer.Write(HWId.H3);
        }
        else
        {
            buffer.Write(false);
        }

        if (HWId.S2 != null)
        {
            buffer.Write(true);
            buffer.Write(HWId.S2);
        }
        else
        {
            buffer.Write(false);
        }

        if (HWId.H4 != null)
        {
            buffer.Write(true);
            buffer.Write(HWId.H4);
        }
        else
        {
            buffer.Write(false);
        }

        var u1Count = (ushort) 0;
        foreach (var i in HWId.U1)
        {
            if (i != null)
                u1Count++;
        }
        buffer.Write(u1Count);
        for (var i = 0; i < u1Count; i++)
        {
            buffer.Write(HWId.U1[i]);
        }

        if (HWId.S1 != null)
        {
            buffer.Write(true);
            buffer.Write(HWId.S1);
        }
        else
        {
            buffer.Write(false);
        }
    }
}
