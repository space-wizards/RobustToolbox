varying highp vec2 UV;

uniform sampler2D lightMap;
uniform highp vec4 modulate;

// [SHADER_HEADER_CODE]

void main()
{
    highp vec4 FRAGCOORD = gl_FragCoord;

    lowp vec4 COLOR = vec4(0.0);

    // [SHADER_CODE]

    gl_FragColor = zAdjustResult(COLOR);
}
