using System.Collections.Generic;
using OpenToolkit.GraphicsLibraryFramework;
using Robust.Shared.Utility;

namespace Robust.Client.Graphics.Clyde
{
    internal partial class Clyde
    {
        private sealed unsafe partial class GlfwWindowingImpl
        {
            // Can't use ClydeHandle because it's 64 bit.
            // TODO: this should be MONITOR ID.
            private int _nextWindowId = 1;
            private readonly Dictionary<int, GlfwMonitorReg> _monitors = new();

            public IEnumerable<MonitorReg> AllMonitors => _monitors.Values;

            private void InitMonitors()
            {
                var monitors = GLFW.GetMonitorsRaw(out var count);

                for (var i = 0; i < count; i++)
                {
                    SetupMonitor(monitors[i]);
                }
            }

            private void SetupMonitor(Monitor* monitor)
            {
                var handle = _nextWindowId++;

                DebugTools.Assert(GLFW.GetMonitorUserPointer(monitor) == null,
                    "GLFW window already has user pointer??");

                var name = GLFW.GetMonitorName(monitor);
                var videoMode = GLFW.GetVideoMode(monitor);
                var impl = new MonitorHandle(handle, name, (videoMode->Width, videoMode->Height),
                    videoMode->RefreshRate);

                _clyde._monitorHandles.Add(impl);

                GLFW.SetMonitorUserPointer(monitor, (void*) handle);
                _monitors[handle] = new GlfwMonitorReg
                {
                    Id = handle,
                    Handle = impl,
                    Monitor = monitor
                };
            }

            private void DestroyMonitor(Monitor* monitor)
            {
                var ptr = GLFW.GetMonitorUserPointer(monitor);

                if (ptr == null)
                {
                    var name = GLFW.GetMonitorName(monitor);
                    _sawmill.Warning("clyde.win", $"Monitor '{name}' had no user pointer set??");
                    return;
                }

                if (_monitors.TryGetValue((int) ptr, out var reg))
                {
                    _monitors.Remove((int) ptr);
                    _clyde._monitorHandles.Remove(reg.Handle);
                }

                GLFW.SetMonitorUserPointer(monitor, null);
            }

            private class GlfwMonitorReg : MonitorReg
            {
                public int Id;
                public Monitor* Monitor;
            }
        }
    }
}
