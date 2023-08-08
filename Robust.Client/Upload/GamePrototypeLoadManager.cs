using Robust.Shared.Upload;

namespace Robust.Client.Upload;

public sealed class GamePrototypeLoadManager : SharedPrototypeLoadManager
{
    public override  void SendGamePrototype(string prototype)
    {
        NetManager.ClientSendMessage(new GamePrototypeLoadMessage { PrototypeData = prototype });
    }
}
