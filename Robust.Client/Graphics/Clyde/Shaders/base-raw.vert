// Vertex position.
/*layout (location = 0)*/ attribute vec2 aPos;
// Texture coordinates.
/*layout (location = 1)*/ attribute vec2 tCoord;
/*layout (location = 2)*/ attribute vec2 tCoord2;
// Colour modulation.
/*layout (location = 3)*/ attribute vec4 modulate;

varying vec2 UV;
varying vec2 UV2;

// Maybe we should merge these CPU side.
// idk yet.
uniform mat3 modelMatrix;

// Allows us to do texture atlassing with texture coordinates 0->1
// Input texture coordinates get mapped to this range.
uniform vec4 modifyUV;

vec2 pixel_snap(vec2 vertex)
{
    vertex += 1.0;
    vertex /= SCREEN_PIXEL_SIZE*2.0;
    vertex = floor(vertex + 0.5);
    vertex *= SCREEN_PIXEL_SIZE*2.0;
    vertex -= 1.0;

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
    UV2 = tCoord2;

    // [SHADER_CODE]

    gl_Position = vec4(VERTEX, 0.0, 1.0);
}
