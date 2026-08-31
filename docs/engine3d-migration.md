# Robust 3D engine migration

This fork intentionally makes a source- and protocol-incompatible break from the
2D RobustToolbox engine. There is no legacy 2D runtime mode. Old systems are
replaced in dependency order instead of being kept behind compatibility flags.

## Spatial conventions

- Right-handed coordinates.
- `+X` is east, `+Y` is north, and `+Z` is up.
- Positions and scales use `System.Numerics.Vector3`.
- Orientations use normalized `System.Numerics.Quaternion` values.
- Transforms use `System.Numerics.Matrix4x4` and its row-vector convention.
- Networked entity and map positions carry all three axes.
- Spatial bounds use `Box3`; planar `Box2` bounds are not valid world bounds.

## Replacement order

1. Spatial math, coordinates, transform hierarchy, and network snapshots.
2. Three-dimensional broadphase primitives and entity lookup.
3. Three-dimensional rigid bodies, collision shapes, queries, and joints.
4. Volumetric map chunks and `Vector3i` cell addressing.
5. Camera, mesh, material, depth, lighting, and shadow render passes.
6. Input ray casting, interaction, visibility, PVS, and navigation.
7. Content systems and assets.

Each layer is migrated against the new contract. It must not reintroduce XY
projection helpers into runtime simulation code merely to keep an old subsystem
working.

## Current cut

The first layer has started:

- `MapCoordinates`, `EntityCoordinates`, and `NetCoordinates` use `Vector3`.
- `TransformComponent` uses `Vector3`, `Quaternion`, and `Matrix4x4`.
- Local scale is part of the transform and network state.
- `SpatialMath`, `SpatialTransform`, and `Box3` define the initial 3D math core.
- `Robust.Client3D` is a runnable bootstrap client with an OpenGL 3.3 mesh pass,
  perspective camera, depth buffer, and a volumetric 3D room.
- The local playable slice has camera-relative movement, mouse look, gravity,
  jumping, and fixed-step character collision against transformed `Box3` bounds.
- `Robust.Client3D.Tests` locks down grounding, obstacle blocking, jump/landing,
  and deterministic movement before the controller is moved server-side.

The bootstrap client is intentionally isolated from the legacy client assembly.
It proves the new spatial contract and GPU path can run while the old planar
world pipeline is replaced. It can be launched from the engine root with:

```powershell
dotnet build Robust.Client3D\Robust.Client3D.csproj
bin\Client3D\Robust.Client3D.exe
```

The main shared-engine build is deliberately red at this boundary because the
remaining component trees, physics, map grid, visibility, and lookup systems
still require planar inputs. The next gate is not to project these values back
to XY; it is to replace their `Box2`, `Angle`, and 2D physics contracts.

## Acceptance gates

### Spatial core

- Parent/child transforms preserve position, orientation, and scale on all axes.
- Transform state round-trips over the network without losing Z or quaternion data.
- Reparenting preserves the complete world transform.

### Broadphase and physics

- `Box3`, sphere, capsule, convex hull, and triangle-mesh queries are supported.
- Server-authoritative bodies move and collide on all axes.
- Prediction and reconciliation include 3D position, orientation, and velocity.

### Maps

- Chunks are addressable on X, Y, and Z.
- Floors, ceilings, stairs, lifts, and holes have distinct collision and visibility.
- Navigation can cross vertical links without special-casing map IDs as floors.

### Renderer

- Perspective and orthographic 3D cameras share the same world transform contract.
- Opaque and transparent mesh passes use depth testing correctly.
- glTF meshes, materials, and animations load through the resource cache.
- Picking casts a world-space ray and returns a 3D hit.

### First playable slice

- A client connects to a server and spawns inside a volumetric test room.
- The player walks, falls, collides, aims, and fires on all three axes.
- Another client observes the same transform and projectile results.
