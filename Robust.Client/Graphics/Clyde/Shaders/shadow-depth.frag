#version 330 core

in float dist;

layout(location = 0) out vec4 depth;

void main()
{
    depth = vec4(vec3(dist), 1);
}
