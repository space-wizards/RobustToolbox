// This is the main vertex shader used for rendering of 2D sprites.
#version 450 core

// Vertex position.
layout (location = 0) in vec2 aPos;
// Texture coordinates.
layout (location = 1) in vec2 tCoord;

out vec2 TexCoord;

// Maybe we should merge these CPU side.
// idk yet.
uniform mat3 modelMatrix;
uniform mat3 viewMatrix;
uniform mat3 projectionMatrix;

void main()
{
    vec3 transformed = projectionMatrix * viewMatrix * modelMatrix * vec3(aPos, 1.0);
    gl_Position = vec4(transformed, 1.0);
    TexCoord = tCoord;
}
