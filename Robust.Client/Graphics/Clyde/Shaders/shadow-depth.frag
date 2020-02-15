#version 330 core

in vec4 pos;

layout(location = 0) out vec4 depth;

void main()
{
    depth = vec4(((pos.z / pos.w) + 1.0) * 0.5, 0, 0, 1);
}
