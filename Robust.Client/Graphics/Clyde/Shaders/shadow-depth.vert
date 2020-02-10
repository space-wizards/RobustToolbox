#version 330 core

layout (location = 0) in vec3 aPos;

//out vec4 pos;
out vec2 dist;

uniform mat4 projectionMatrix;
uniform mat4 lightMatrix;

void main()
{
    vec4 rel = lightMatrix * vec4(aPos, 1);
    dist = rel.xy;
    gl_Position = projectionMatrix * rel;
    //pos = gl_Position;
}
