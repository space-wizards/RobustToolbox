varying highp vec2 pos;

// BE SURE TO CHECK THE CORRESPONDING UNPACK CODE AT:
//  RobustToolbox/Resources/Shaders/Internal/shadow_cast_shared.swsl
// Also, it'd be nice if these functions were put into some sort of common code
lowp vec4 ffTwoPack(highp vec2 val) {
#ifdef HAS_FLOAT_TEXTURES
    return vec4(val, 0.0, 1.0);
#else
    highp vec2 valH = floor(val);
    return vec4(valH / 255.0, val - valH);
#endif
}

void main()
{
    highp vec2 adjustedPos = pos;
    if (!gl_FrontFacing)
    {
        // Slightly bias back faces inwards.
        // This fixes being able to see a few no-lighting pixels (like space)
        // "behind" walls at certain angles/positions.
        //adjustedPos -= sign(adjustedPos) * 1.5/32.0;
    }
    highp float dist = length(adjustedPos);
#ifdef HAS_DFDX
    highp float dx = dFdx(dist);
    highp float dy = dFdy(dist); // I'm aware derivative of y makes no sense here but oh well.
#else
    highp float dx = 1.0;
    highp float dy = 1.0;
#endif
    gl_FragColor = ffTwoPack(vec2(dist, dist * dist + 0.25 * (dx*dx + dy*dy)));
}

