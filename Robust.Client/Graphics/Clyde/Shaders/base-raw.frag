varying highp vec2 UV;

uniform sampler2D TEXTURE;
uniform sampler2D lightMap;
uniform highp vec4 modulate;

#ifdef HAS_UNIFORM_BUFFERS
layout (std140) uniform uniformConstants
{
    vec2 SCREEN_PIXEL_SIZE;
    float TIME;
};
#else
uniform highp vec2 SCREEN_PIXEL_SIZE;
uniform highp float TIME;
#endif

uniform highp vec2 TEXTURE_PIXEL_SIZE;

// [SHADER_HEADER_CODE]

void main()
{
    highp vec4 FRAGCOORD = gl_FragCoord;

    lowp vec4 COLOR = vec4(0.0);

    // [SHADER_CODE]

    gl_FragColor = COLOR;
}
