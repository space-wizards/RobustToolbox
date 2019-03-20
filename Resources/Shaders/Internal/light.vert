#version 330 core

// Vertex position.
layout (location = 0) in vec2 aPos;

out vec2 worldPosition;

uniform mat3 modelMatrix;

layout (std140) uniform projectionViewMatrices
{
    mat3 projectionMatrix;
    mat3 viewMatrix;
};

void main()
{
    vec3 transformed = modelMatrix * vec3(aPos, 1.0);
    worldPosition = transformed.xy;
    transformed = projectionMatrix * viewMatrix * transformed;

    gl_Position = vec4(transformed, 1.0);
}
