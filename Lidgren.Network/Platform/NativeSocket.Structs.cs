using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

#pragma warning disable 649
namespace Lidgren.Network {

  public static partial class NativeSockets {

    [SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Local")]
    [StructLayout(LayoutKind.Sequential, Size = SockAddrStorageSize)]
    private struct SockAddr {

      public ushort SocketAddressFamily;

    }

    [SuppressMessage("ReSharper", "NotAccessedField.Local")]
    private struct SockAddrIn {

      public ushort SocketAddressFamily;

      public ushort Port;

      public uint InAddr;

      public ulong Zero;

    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    private struct InAddr6 {

    }

    [SuppressMessage("ReSharper", "NotAccessedField.Local")]
    private struct SockAddrIn6 {

      public ushort SocketAddressFamily;

      public ushort Port;

      public uint FlowInfo;

      public InAddr6 InAddr;

      public uint InScopeId;

    }

  }

}