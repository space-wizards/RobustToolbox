# Linux Compiling Guide #

## Dependencies ##

There is a good chance you can install these with your package manager.

* Mono 
* SFML and CSFML binding (http://www.sfml-dev.org/download.php)

  The version of SFML and SFML.Net bindings should match. Else the mouse cursor can be stuck or other horrible things can happen.

  The SFML.Net included in the sources currently is for SFML 2.2. If you have a different SFML version you can download the corresponding SFML.Net libraries from the project's homepage and replace the files in `Third-Party` (before compile) or `bin/Client` (after compile). For some reason SFML.Net 2.3 can't be downloaded from the homepage, you have to compile it yourself.

## Compiling ##

Just run
```
xbuild
```
in the root directory. The results will be in the `bin` directory.

After compiling copy the `sfmlnet-*.dll.config` files to the bin/Client directory.
```
cp Third-Party/sfmlnet-*.dll.config bin/Client
```

## Run ##

### Client ###
```
cd bin/Client
mono SpaceStation14.exe
```

If you get errors similar to this:
```
Unhandled Exception:
System.DllNotFoundException: csfml-graphics-2
.
.
.
```

Then you either forgot to copy the `sfmlnet-*.dll.config` files after compiling or your csfml library is in a non-standard location. If the latter, adjust the `sfmlnet-*.dll.config` files to point to the right libraries.

### Server ###
```
cd bin/Server
mono SpaceStation14_Server.exe
```
