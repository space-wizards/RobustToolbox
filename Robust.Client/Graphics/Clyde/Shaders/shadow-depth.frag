
// xy: A, zw: B
varying highp vec4 fragPos;
// x: actual angle, y: horizontal (1) / vertical (-1)
varying highp vec2 fragAngle;

void main()
{
    // Stuff that needs to be inferred to avoid interpolation issues.
    highp vec2 rayNormal = vec2(cos(fragAngle.x), -sin(fragAngle.x));

    // Depth calculation accounting for interpolation.
    highp float dist;

    if (fragAngle.y > 0.0) {
        // Line is horizontal
        dist = abs(fragPos.y / rayNormal.y);
    } else {
        // Line is vertical
        dist = abs(fragPos.x / rayNormal.x);
    }

    // Main body.
#ifdef HAS_DFDX
    highp float dx = dFdx(dist);
    highp float dy = dFdy(dist); // I'm aware derivative of y makes no sense here but oh well.
#else
    highp float dx = 1.0;
    highp float dy = 1.0;
#endif
    gl_FragColor = zClydeShadowDepthPack(vec2(dist, dist * dist + 0.25 * (dx*dx + dy*dy)));
}

