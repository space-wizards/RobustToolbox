@echo off
echo Assembling files...
mkdir ResourcePack
mkdir ResourcePack\Fonts
mkdir ResourcePack\Shaders
mkdir ResourcePack\TAI
mkdir ResourcePack\Textures
mkdir ResourcePack\ParticleSystems
mkdir ResourcePack\Animations
copy Fonts\* ResourcePack\Fonts > nul
copy textures\*.png ResourcePack\Textures > nul
copy textures\Unatlased\*.png ResourcePack\Textures > nul
copy textures\*.TAI ResourcePack\TAI > nul
copy Shaders\* ResourcePack\Shaders > nul
for /D %%d in (Shaders\*) do (
	mkdir ResourcePack\Shaders\%%~nd > nul
	copy %%d\* ResourcePack\Shaders\%%~nd > nul
)
copy ParticleSystems\*.xml ResourcePack\ParticleSystems > nul
copy textures\Animations\*.xml ResourcePack\Animations > nul
echo Compressing...
del ResourcePack.zip
cd ResourcePack
..\..\Tools\7za a -tzip ..\ResourcePack.zip * > nul
cd ..
echo Cleaning up...
rd /S /Q ResourcePack

echo Resource pack build complete.
pause