# SS14 Map Format

Just a quick write-up on the map format. I'm not making this too formal but it's better than pretending my code is self documenting.

So, right now, map files are YAML. A map file is contained in a single YAML document.

The root node of the map file is a mapping. The members of this mapping are various sections that will be documented below.

A map can contain one or more grids, and all the entities on those grids will be stored. Other data that is serialized by entities is also possible.

## Sections

### The `meta` Section

This contains some basic info that might be useful, such as map version.

Fields:

* `format`: Version identifier. **The current version is `2`.** Can be used to bail out early for unsupported map files.
* `name`: A name. Simple huh. Can be left out.
* `author`: Authorship info. Also simple. Can be left out.
* `postmapinit`: Whether this map is "post map init". This means that presets such as procedural generation have applied.
                 In general, maps touched only via map editing mode will have this false. Maps saved mid game will not.
                 Default value is true if left out.

### The `tilemap` section

Numeric tile IDs are not guaranteed to be consistent between different versions of the game. As such, each map file that contains grids must also contain a `tilemap` section, mapping tile definition names to the tile IDs used to encode them in the `grids` section.

The section is a map of numeric ID: tile definition name.

### The `grids` Section

Contains data for all the grids. The section is an ordered sequence. Each sequence is made up of a single grid's data. That data:

* `settings`: Mapping with grid-specific parameters. Those values:
  * `tilesize`: An integer representing the length of one side of a grid tile, in world units (meters).
  * `chunksize`: An integer representing the tile dimensions of a chunk in this grid. Basically, when chunksize is `x`, a single chunk contains a square region of `x` by `x` tiles.
  * `snapsize`: A float representing snap grid size.

* `chunks`: A sequence containing the actual chunk data for this grid. See below.

Grids also have their **grid entity** serialized like regular entities.

### The `entities` Section

Contains data for all entities on the map. Just like grids these are stored in an indexed list, and an entity declaration is pretty much just like a prototype.

Each entity has a `type` field which specifies which prototype it is, and the components list works as overrides in the same way as prototype parenting.
Each entity also has a numerical `uid` field, which is used to give this entity an unique identifier when referenced by other entities.

#### Chunk Data

Each entry into the `chunks` sequence is a mapping for a single chunk of the grid. A chunk has two fields:

* `ind`: The chunk index.
* `tiles`: Base64 encoded tile data. See below.

Tile data is a binary array of the tile data of a chunk. Tiles are ordered without gaps, in row-major order.

Tiles are 4 bytes in size (`ushort` for Tile ID, `ushort` for tile metadata field, little endian) Thus, since the amount of tiles is equal to `chunksize * chunksize`, the tile data per chunk is exactly `chunksize * chunksize * 4` bytes long.

Tile IDs should be mapped to tile definitions via the `tilemap` section.

## Blueprints

Blueprints are just map files with one grid.

## Data Serialization Specifics

How some data is serialized to be able to be cross-referenced in a map file is documented here.

### Common Data Types

Most data types are resolved in the same way as prototype YAML data. So, a vector 2 is just serialized as single scalar `{x},{y}`. Etc...

Numbers are serialized using `CultureInfo.InvariantCulture`.

Distances, coordinates, etc... are measured in meters. That said, things such as scaling transforms might mess those up. It at least holds true for a specific coordinate system though.

Angles are serialized in radians.

### `IEntity` References

Direct hard `IEntity` references are stored as entity UID, it is simply decoded differently upon request via `ObjectSerializer.DataField<T>` and such.

### Entity UIDs

In-game `EntityUid` instances are either:

* Serialized as YAML `null` if the entity referenced to is not included in the map saving. If it's on a different grid, for example.
* An integer representing the `uid` of the serialized entity.

### Grid IDs

In-game `GridId` instances are either:

* Serialized as YAML `null` if the grid that was referenced is nullspace or not included in this map file.
* Integer representing the index in the `grids` section of the map file corresponding to the referenced grid.

## Misc Notes

The map format isn't meant to be optimal right now. There's tons of things we could improve about it.

For example, YAML is bloated as hell. It's used because it was easier to implement (other data infrastructure such as prototypes is also YAML). Even moving to JSON might be a better idea.

There is tons of wasted space on say mapping keys. The idea is that since Git compresses files that are checked in anyways this should be fine. There's tons of repeated data here. In some tests with stationstation I've gotten more than 90% compression ratios (that is, input is >10x larger than compressed) with just gzip.

Ideally you should be able to serialize all but the most conveluted game logic to map files.

Tile data is little endian. I wanna repeat this because Microsoft doesn't document this *anywhere* on `System.IO.BinaryWriter` and the first conclusive answer I got from Google was an angry rant that Microsoft hates standards. That actually reminds me: **Map files are UTF-8**. Just sayin'
