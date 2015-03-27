#!/bin/bash
# This works from the root folder.  Keep that in mind.
mkdir -p bin
cp -v Third-Party/*.dll bin/
cp -v Third-Party/*.so bin/Client
cp -v Third-Party/*.so.2.2.0 bin/Client
