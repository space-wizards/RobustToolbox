// UV coordinates in texture-space. I.e., (0,0) is the corner of the texture currently being used to draw.
// When drawing a sprite from a texture atlas, (0,0) is the corner of the atlas, not the specific sprite being drawn.
varying highp vec2 UV;

// UV coordinates in quad-space. I.e., when drawing a sprite from a texture atlas (0,0) is the corner of the sprite
// currently being drawn.
varying highp vec2 UV2;

// TBH I'm not sure what this is for. I think it is scree  UV coordiantes, i.e., FRAGCOORD.xy * SCREEN_PIXEL_SIZE ?
// TODO CLYDE Is this still needed?
varying highp vec2 Pos;

// Vertex colour modulation. Note that negative values imply that the LIGHTMAP should be ignored. This is used to avoid
// having to set the texture to a white/blank texture for sprites that have no light shading applied.
varying highp vec4 VtxModulate;

// The current light map. Unless disabled, this is automatically sampled to create the LIGHT vector, which is then used
// to modulate the output colour.
// TODO CLYDE consistent shader variable naming
uniform sampler2D lightMap;

// [SHADER_HEADER_CODE]

void main()
{
    highp vec4 FRAGCOORD = gl_FragCoord;

    // The output colour. This should get set by the shader code block.
    // This will get modified by the LIGHT and MODULATE vectors.
    lowp vec4 COLOR;

    // The light colour, usually sampled from the LIGHTMAP
    lowp vec4 LIGHT;

    // Colour modulation vector.
    highp vec4 MODULATE;

    // Sample the texture outside of the branch / with uniform control flow.
    LIGHT = texture2D(lightMap, Pos);

    if (VtxModulate.x < 0.0)
    {
        // Negative VtxModulate implies unshaded/no lighting.
        MODULATE = -1.0 - VtxModulate;
        LIGHT = vec4(1.0);
    }
    else
    {
        MODULATE = VtxModulate;
    }

    // TODO CLYDE consistent shader variable naming
    // Requires breaking changes.
    lowp vec3 lightSample = LIGHT.xyz;

    // [SHADER_CODE]

    LIGHT.xyz = lightSample;

    gl_FragColor = zAdjustResult(COLOR * MODULATE * LIGHT);
}
