#version 330 core

layout (location = 0) in vec3 aPos;

uniform mat4 projectionMatrix;
uniform mat4 lightMatrix;

void main()
{
    gl_Position = projectionMatrix * lightMatrix * vec4(aPos, 1);
}
