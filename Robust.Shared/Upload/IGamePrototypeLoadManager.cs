using System;
using Robust.Shared.Serialization;

namespace Robust.Shared.Upload;

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
