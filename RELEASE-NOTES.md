# Release notes for RobustToolbox.

<!--
Template for new versions:

## Master

### Breaking changes

*None yet*

### New features

*None yet*

### Bugfixes

* Made entity deletion more resilient against exceptions. Should fix several bugs.

### Other

*None yet*

### Internal

*None yet*

-->

## 0.61.0.0

### Breaking changes

* IMap and IMapGrid have been removed. Just use the associated components directly.

### Other

* AudioSystem has been refactored.

## 0.60.0.0

### Breaking changes

* ISerializationHooks.BeforeSerialization() has been removed. Use custom type serializers instead.

### New features

* Added function to UserInterfaceSystem that returns list of BUIs that a client has open.

### Bugfixes

* Fixed various container related broadphase bugs which could result in entities getting stuck with a null-broadphase.
* Fixed client fixture state handling bug that caused the client to incorrectly disable collision.

### Other

* Misc PVS optimisations

### Internal

* Removed redundant grid-init physics logic 
* Modified garbage collection for entity spawning profiling.

## 0.59.0.0

### Breaking changes

* Various transform related methods have been removed from MapGrids
* TransformSystem.SetCoordinates() arguments have changed and now allow an entity to be sent to nullspace

### Bugfixes

* Fixed an entity lookup bug that sometimes failed to return entities in StaticSundriesTrees

### Other

* The EntitySystem.Resolve<> methods have been change to protected

## 0.58.1.1

### Bugfixes

* Fixed some container shutdown errors
* Fixed LookupFlags.Static not acting as a full replacement for LookupFlags.Anchored

## 0.58.1.0

### Other

* Physics collision changed and body type changed events no longer get raised before initialisation

## 0.58.0.0

### Breaking changes

* Some TransformComponent functions have been moved to the system.
* Container insert, remove, and shutdown function arguments and functionality has changed.
* Physics entities without fixtures now automatically disable collision.

### New features

* Added command to profile entity spawning

### Bugfixes

* EntityLookup/BroadphaseComponent tracking has been overhauled, which should hopefully fix various broadphase bugs.

### Other

* Component.Owner is now marked as obsolete.

## 0.57.0.4

### Bugfixes

* Made entity deletion more resilient against exceptions. Should fix several bugs.

## 0.57.0.2 and 0.57.0.3

### Bugfixes

* Fixed more entity-lookup bugs.

## 0.57.0.1

### Bugfixes

* Fixed entity lookup bug that was causing crashes.

### 0.57.0.0

### Breaking changes

* EntityLookupComponent has been merged into BroadphaseComponent. The data that was previously stored in this tree is now stored across the 3 trees on BroadphaseComponent.

### New features

* EntityLookup has had its flags updated to reflect the merge of EntityLookupComponent and BroadphaseComponent, with the new flags reflecting each tree: Dynamic, Static, and Sundries. Dynamic and Static store physics bodies that are collidable and Sundries stores everything else (apart from grids).

### Internal

* EntityLookup and Broadphase have had their data de-duplicated, dropping the AABBs stored on the server by half. This also means MoveEvent updates will be much faster.
* PVS mover updates has had their performance improved slightly.
* Physics LinkedList nodes for contacts will no longer be re-made for every contact and will just be cleared when re-used.
* Sprite / Light dynamictree allocations on the client have been dropped by using static lambdas.
* The physics contact buffer for each FixtureProxy is now pooled.

## 0.56.1.1

### Bugfixes

* Fix PVS sometimes not sending an entity's parents.
* Fix velocity preservation on parenting changes.

## 0.56.1.0

### New features

* Update pt-BR locale with more localizations
* Separated PVS entity budget into an entity creation budget and a pvs-entry budget.

### Bugfixes

* Fix VV type handler removal.
* System errors during component removal should no longer result in undeletable entities.

### Other

* The ordering of component removals and shutdowns during entity deltion has changed (see #3355).
* Improved Box2Serializer
* Removed uses IEnumerables from EntityLookupSystem.
* Optimized client entity spawning by 15%.
* Modified how the rendering tree handles entity movement.
* Improved grid enumeration allocs.
* Fixed a bunch of build warnings (see #3329 and #3289 for details)

## 0.56.0.2

### Bugfixes

* Rename \_lib.ftl to \_engine_lib.ftl to avoid overwriting

## 0.56.0.1

### Bugfixes

* Fix instantiation of data records containing value types

## 0.56.0.0

### Breaking changes

* `CastShadows` moved to `SharedPointLightComponent` from clientside, now networked

### New features

* New type handler helpers added to V^3
* Added pt-BR locale

### Bugfixes

* Fixed audio fallback coords

### Other

* Improved PVS performance by using `for` over `forEach`
* Improved Vec2 inverse allocations

## 0.55.5.0

### New features

* Added a method to pass in physics transforms for getting nearest point.

### Bugfixes

* Prevent singular sprite matrices.
* Fix obsolete warnings in tests.

### Other

* Significantly reduce physics contact allocations.

## 0.55.4.1

### Breaking changes

* Removed `SI`, `SIoC`, `I`, `IoC`, `SE` and `CE` VV command prefixes.
  * `SI`, `SIoC`, `I` and `IoC` are replaced by VV paths under `/ioc/` and `/c/ioc/`.
  * `SE` and `CE` are replaced by VV paths under `/system/` and `/c/system`.

### New features

* Added CVars to control Lidgren's <abbr title="Maximum Transmission Unit">MTU</abbr> parameters:
  * `net.mtu`
  * `net.mtu_expand`
  * `net.mtu_expand_frequency`
  * `net.mtu_expand_fail_attempts`
* Added a whole load of features to ViewVariables.
  * Added VV Paths, which allow you to refer to an object by a path, e.g. `/entity/1234/Transform/WorldPosition`
  * Added VV Domains, which allow you to add "handlers" for the top-most VV Path segment, e.g. `/entity` is a domain and so is `/player`...
  * Added VV Type Handlers, which allow you to add "custom paths" under specific types, even dynamically!
  * Added VV Path networking, which allows you to read/write/invoke paths remotely, both from server to client and from client to server.
  * Added `vvread`, `vvwrite` and `vvinvoke` commands, which allow you to read, write and invoke VV paths.
  * Added autocompletion to all VV commands.
  * Please note that the VV GUI still remains the same. It will be updated to use these new features in the future.

### Other

* Changed Lidgren to be compiled against `net6.0`. This unlocks `Half` read/write methods.
* Lidgren has been updated to [0.2.2](https://github.com/space-wizards/SpaceWizards.Lidgren.Network/blob/v0.2.2/RELEASE-NOTES.md). Not all the changes since 0.1.0 are new here, since this is the first version where we're properly tracking this in release notes.
* Robust.Client now uses our own [NFluidsynth](https://github.com/space-wizards/SpaceWizards.NFluidsynth) [nuget package](https://www.nuget.org/packages/SpaceWizards.NFluidsynth).

### Internal

* Renamed Lidgren's assembly to `SpaceWizards.Lidgren.Network`.
* Rogue `obj/` folders inside Lidgren no longer break the build.
* Renamed NFluidsynth's assembly to `SpaceWizards.NFluidsynth`
