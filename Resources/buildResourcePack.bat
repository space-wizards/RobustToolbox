@echo off
echo Rendering 3d sprites...
cd SpriteRenderer
if exist output goto outputexists
goto runspriterenderer
:outputexists
echo Cleaning up previous run
rmdir /S /Q output
:runspriterenderer
mkdir output
echo Starting Renderer...
MSpriteRenderer.exe
cd ..
cd textures
echo Moving 3d Sprites into place...
del /S /Q Animations
mkdir Animations
move ..\SpriteRenderer\output\* Animations\
echo Building texture atlases...
call buildAtlases
cd ..
call buildResourcePack-builder.bat