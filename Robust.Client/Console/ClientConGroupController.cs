﻿using System;
using Robust.Shared.Console;

namespace Robust.Client.Console
{
    public sealed class ClientConGroupController : ConGroupController, IClientConGroupController
    {
        private IClientConGroupImplementation? _implementation;
        public event Action? ConGroupUpdated;

        IClientConGroupImplementation? IClientConGroupController.Implementation
        {
            set
            {
                if (_implementation != null)
                {
                    _implementation.ConGroupUpdated -= GroupUpdated;
                }

                _implementation = value!;
                _implementation.ConGroupUpdated += GroupUpdated;
            }
        }

        public bool CanCommand(string cmdName)
        {
            return _implementation?.CanCommand(cmdName) ?? true;
        }

        public bool CanViewVar()
        {
            return _implementation?.CanViewVar() ?? false;
        }

        public bool CanAdminPlace()
        {
            return _implementation?.CanAdminPlace() ?? false;
        }

        public bool CanScript()
        {
            return _implementation?.CanScript() ?? false;
        }

        public bool CanAdminMenu()
        {
            return _implementation?.CanAdminMenu() ?? false;
        }

        private void GroupUpdated()
        {
            ConGroupUpdated?.Invoke();
        }
    }
}
