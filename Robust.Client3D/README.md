# Robust.Client3D

This is the first runnable client of the incompatible 3D engine fork. It is a
small bootstrap executable rather than a compatibility mode for the legacy 2D
client. It consumes the new `SpatialTransform` contract directly and owns its
OpenGL window, perspective camera, depth buffer, mesh upload, and render loop.

Build and run from the RobustToolbox directory:

```powershell
dotnet build Robust.Client3D\Robust.Client3D.csproj
bin\Client3D\Robust.Client3D.exe
```

Controls:

- `WASD` moves relative to the camera.
- Mouse movement rotates the third-person camera.
- `Space` jumps.
- `Escape` exits.

For a deterministic smoke run that exits and captures the final framebuffer:

```powershell
bin\Client3D\Robust.Client3D.exe --frames=150 --autoplay --screenshot=bin\Client3D\playable-smoke.bmp
```

The default run remains open until the window is closed. The room and its
objects use full XYZ positions, quaternion rotations, non-uniform 3D scales,
perspective projection, and depth-tested rendering. The player uses a fixed-step
kinematic controller with gravity, jumping, and collision against the `Box3`
bounds generated from the visible room geometry.
