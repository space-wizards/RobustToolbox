﻿using Robust.Server.Player;

namespace Robust.Server.Console
{
    public interface IConGroupControllerImplementation
    {
        bool CanCommand(IPlayerSession session, string cmdName);
        bool CanAdminPlace(IPlayerSession session);
        bool CanScript(IPlayerSession session);
        bool CanAdminMenu(IPlayerSession session);
        bool CanAdminReloadPrototypes(IPlayerSession session);
    }
}
