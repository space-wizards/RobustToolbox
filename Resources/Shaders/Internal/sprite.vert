#version 330 core

// Vertex position.
layout (location = 0) in vec2 aPos;
// Texture coordinates.
layout (location = 1) in vec2 tCoord;

out vec2 UV;
out vec2 Pos;

// Maybe we should merge these CPU side.
// idk yet.
uniform mat3 modelMatrix;
layout (std140) uniform projectionViewMatrices
{
    mat4 projectionMatrix;
    mat4 viewMatrix;
};

layout (std140) uniform uniformConstants
{
    vec2 SCREEN_PIXEL_SIZE;
    float TIME;
};

// Allows us to do texture atlassing with texture coordinates 0->1
// Input texture coordinates get mapped to this range.
uniform vec4 modifyUV;

[SHADER_HEADER_CODE]

void main()
{
    vec4 transformed = projectionMatrix * viewMatrix * mat4(modelMatrix) * vec4(aPos, 1.0, 1.0);
    vec2 VERTEX = transformed.xy;

    [SHADER_CODE]

    // Pixel snapping to avoid sampling issues on nvidia.
    VERTEX += 1;
    VERTEX /= SCREEN_PIXEL_SIZE*2;
    VERTEX = floor(VERTEX + 0.5);
    VERTEX *= SCREEN_PIXEL_SIZE*2;
    VERTEX -= 1;

    gl_Position = vec4(VERTEX, 0.0, 1.0);
    Pos = (VERTEX + 1) / 2;
    UV = mix(modifyUV.xy, modifyUV.zw, tCoord);
}
