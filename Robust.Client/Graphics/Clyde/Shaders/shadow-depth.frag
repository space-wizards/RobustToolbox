
// x: Angle being queried, y: Angle of closest point of line (is of 90-degree angle to line angle), z: Distance at y
varying highp vec3 fragControl;

void main()
{
    // Thanks to Radrark for finding this for me. There's also a useful diagram, but this is text, so:
    // r = p / cos(theta - phi)
    // r: Distance to line *given angle theta*
    // p: Distance to closest point of line
    // theta: Angle being queried
    // phi: Angle of closest point of line - inherently on 90-degree angle to line angle
    highp float dist = abs(fragControl.z / cos(fragControl.x - fragControl.y));

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

