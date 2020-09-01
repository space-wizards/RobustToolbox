varying highp vec2 pos;

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
    gl_FragColor = zClydeShadowDepthPack(vec2(dist, dist * dist + 0.25 * (dx*dx + dy*dy)));
}

