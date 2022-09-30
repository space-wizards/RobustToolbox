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

*None yet*

### New features

* Added CVars to control Lidgren's <abbr title="Maximum Transmission Unit">MTU</abbr> parameters:
  * `net.mtu`
  * `net.mtu_expand`
  * `net.mtu_expand_frequency`
  * `net.mtu_expand_fail_attempts`

### Bugfixes

*None yet*

### Other

* Changed Lidgren to be compiled against `net6.0`. This unlocks `Half` read/write methods.

### Internal

* Renamed Lidgren's assembly to `SpaceWizards.Lidgren.Network`.

