using Robust.Shared.Upload;

namespace Robust.Client.Upload;

public sealed class GamePrototypeLoadManager : SharedPrototypeLoadManager
{
    // The server already accepted this upload.
    protected override bool ValidateUploads => false;

    public override  void SendGamePrototype(string prototype)
    {
        NetManager.ClientSendMessage(new GamePrototypeLoadMessage { PrototypeData = prototype });
    }
}
