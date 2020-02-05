#version 330 core

layout (location = 0) in vec3 aPos;

out float dist;

uniform mat4 projectionMatrix;
uniform mat4 lightMatrix;

void main()
{
    vec4 rel = lightMatrix * vec4(aPos, 1);
    gl_Position = projectionMatrix * rel;
    dist = distance(vec2(0), rel.xy);
}
