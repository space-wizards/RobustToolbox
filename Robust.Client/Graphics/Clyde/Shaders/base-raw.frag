varying highp vec2 UV;
varying highp vec2 UV2;

// TODO CLYDE consistent shader variable naming
uniform sampler2D lightMap;

// [SHADER_HEADER_CODE]

void main()
{
    highp vec4 FRAGCOORD = gl_FragCoord;

    lowp vec4 COLOR = vec4(0.0);

    // [SHADER_CODE]

    // NOTE: You may want to add modulation here. Problem: Game doesn't like that.
    // In particular, walls disappear.
    gl_FragColor = zAdjustResult(COLOR);
}
