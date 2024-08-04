// Vertex position.
/*layout (location = 0)*/ attribute vec2 aPos;
// Texture coordinates.
/*layout (location = 1)*/ attribute vec2 tCoord;
/*layout (location = 2)*/ attribute vec2 tCoord2;
// Colour modulation.
/*layout (location = 3)*/ attribute vec4 modulate;

varying vec2 UV;
varying vec2 UV2;
varying vec2 Pos;
varying vec4 VtxModulate;

// Maybe we should merge these CPU side.
// idk yet.
uniform mat3 modelMatrix;

// Allows us to do texture atlassing with texture coordinates 0->1
// Input texture coordinates get mapped to this range.
uniform vec4 modifyUV;
// TODO CLYDE Is this still needed?

// [SHADER_HEADER_CODE]

void main()
{
    vec3 transformed = projectionMatrix * viewMatrix * modelMatrix * vec3(aPos, 1.0);
    vec2 VERTEX = transformed.xy;

    // [SHADER_CODE]

    // Pixel snapping to avoid sampling issues on nvidia.
    VERTEX += 1.0;
    VERTEX /= SCREEN_PIXEL_SIZE*2.0;
    VERTEX = floor(VERTEX + 0.5);
    VERTEX *= SCREEN_PIXEL_SIZE*2.0;
    VERTEX -= 1.0;

    gl_Position = vec4(VERTEX, 0.0, 1.0);
    Pos = (VERTEX + 1.0) / 2.0;
    UV = mix(modifyUV.xy, modifyUV.zw, tCoord);
    UV2 = tCoord2;

    // Negative modulation is being used as a hacky way to squeeze in lighting data.
    // I.e., negative modulation implies we ignore the lighting.
    if (modulate.x < 0.0)
    {
        VtxModulate = -1.0 - zFromSrgb(-1.0 - modulate);
    }
    else
    {
        VtxModulate = zFromSrgb(modulate);
    }
}
