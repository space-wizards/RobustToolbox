#version 330 core

in vec2 pos;

layout(location = 0) out vec4 depth;

void main()
{
    float dist = distance(pos, vec2(0));
    // Slightly bias back faces inwards.
    // This fixes being able to see a few pixels "behind" walls at certain angles.
    dist -= gl_FrontFacing ? 0 : 0.05;
    depth = vec4(dist, 0, 0, 1);
}
