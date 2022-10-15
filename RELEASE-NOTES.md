# Release notes for RobustToolbox.

<!--
Template for new versions:

## Master

### Breaking changes

*None yet*

### New features

*None yet*

### Bugfixes

*None yet*

### Other

*None yet*

### Internal

*None yet*

-->

## Master

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

### Bugfixes

*None yet*

### Other

* Changed Lidgren to be compiled against `net6.0`. This unlocks `Half` read/write methods.
* Lidgren has been updated to [0.2.2](https://github.com/space-wizards/SpaceWizards.Lidgren.Network/blob/v0.2.2/RELEASE-NOTES.md). Not all the changes since 0.1.0 are new here, since this is the first version where we're properly tracking this in release notes.
* Robust.Client now uses our own [NFluidsynth](https://github.com/space-wizards/SpaceWizards.NFluidsynth) [nuget package](https://www.nuget.org/packages/SpaceWizards.NFluidsynth).

### Internal

* Renamed Lidgren's assembly to `SpaceWizards.Lidgren.Network`.
* Rogue `obj/` folders inside Lidgren no longer break the build.
* Renamed NFluidsynth's assembly to `SpaceWizards.NFluidsynth`
