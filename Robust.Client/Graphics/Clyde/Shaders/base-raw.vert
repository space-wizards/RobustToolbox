#version 330 core

// Vertex position.
layout (location = 0) in vec2 aPos;
// Texture coordinates.
layout (location = 1) in vec2 tCoord;

out vec2 UV;

// Maybe we should merge these CPU side.
// idk yet.
uniform mat3 modelMatrix;
layout (std140) uniform projectionViewMatrices
{
    mat3 projectionMatrix;
    mat3 viewMatrix;
};

layout (std140) uniform uniformConstants
{
    vec2 SCREEN_PIXEL_SIZE;
    float TIME;
};

// Allows us to do texture atlassing with texture coordinates 0->1
// Input texture coordinates get mapped to this range.
uniform vec4 modifyUV;

vec2 pixel_snap(vec2 vertex)
{
    vertex += 1;
    vertex /= SCREEN_PIXEL_SIZE*2;
    vertex = floor(vertex + 0.5);
    vertex *= SCREEN_PIXEL_SIZE*2;
    vertex -= 1;

    return vertex;
}

vec2 apply_mvp(vec2 vertex)
{
    vec3 transformed = projectionMatrix * viewMatrix * modelMatrix * vec3(vertex, 1.0);

    return transformed.xy;
}

// [SHADER_HEADER_CODE]

void main()
{
    vec2 VERTEX = aPos;

    UV = tCoord;

    // [SHADER_CODE]

    gl_Position = vec4(VERTEX, 0.0, 1.0);
}
