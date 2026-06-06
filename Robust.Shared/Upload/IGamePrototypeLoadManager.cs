using System;
using Robust.Shared.Serialization;

namespace Robust.Shared.Upload;

[NotContentImplementable]
public interface IGamePrototypeLoadManager
{
    public void Initialize();
    public void SendGamePrototype(string prototype);
}

[Serializable, NetSerializable]
public sealed class ReplayPrototypeUploadMsg
{
    public string PrototypeData = default!;
}
