#!/bin/bash
prebuild-nant.sh

nant

#nunit-console Bin/SS14.Test.dll -nodots -xml=NUnit.Results.xml
echo NUnit disabled.

#7za a SS14.7z Bin/*
echo Packaging disabled.