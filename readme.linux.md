# Linux Compiling Guide #

## Dependencies ##
Python 3.6.2
SFML 2.4 
CSFML 2.4

## Compiling ##

cd path/to/SpaceStation14

python RUN_THIS.py

cd Resources

wget http://84.195.252.227/static/ResourcePack.zip

open solution

build

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
You forgot SFML or CSFML. Install the packages

### Server ###
```
cd bin/Server
mono SpaceStation14_Server.exe
```
