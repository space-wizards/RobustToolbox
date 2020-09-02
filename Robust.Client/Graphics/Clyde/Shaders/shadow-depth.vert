// Polar-coordinate mapper.
// While inspired by https://www.gamasutra.com/blogs/RobWare/20180226/313491/Fast_2D_shadows_in_Unity_using_1D_shadow_mapping.php ,
//  has one major difference:
// The assumption here is that the shadow sampling must be reduced.
// The original cardinal-direction mapper as written by PJB used 4 separate views.
// As such, it's still an increase in performance to only render 2 views.
// And as such, a line can be split across the 2 views.

// xy: A, zw: B
attribute vec4 aPos;
// x: deflection(0=A/1=B) y: height
attribute vec2 subVertex;

varying vec2 pos;

// Note: This is *not* the standard projectionMatrix!
uniform vec2 shadowLightCentre;

uniform float shadowOverlapSide;

// this constant ought to be moved to z-library
// also deal with the reference to it in shadow_cast_shared
const highp float PI = 3.1415926535897932384626433;

float calcX(vec2 rel)
{
    return atan(rel.y, -rel.x) / PI;
}

void main()
{
    // aPos is clockwise, but we need anticlockwise so swap it here
    vec2 pA = aPos.zw - shadowLightCentre;
    vec2 pB = aPos.xy - shadowLightCentre;
    float xA = calcX(pA);
    float xB = calcX(pB);

    // We need to reliably detect a clip, as opposed to, say, a backdrawn face.
    // So a clip is when the angular area is >= 180 degrees (which is not possible with a quad and always occurs when wrapping)
    if (abs(xA - xB) >= 1.0)
    {
        // Oh no! It clipped...
        if (xA > xB)
        {
            // ...such that xA is on the right side and xB is on the left!
            if (shadowOverlapSide > 0.5)
            {
                // ...move the left boundary past the left edge!
                xA -= 2.0;
            }
            else
            {
                // ...move the right boundary past the right edge!
                xB += 2.0;
            }
        }
        else
        {
            // ...such that xA is on the left side and xB is on the right!
            if (shadowOverlapSide > 0.5)
            {
                // ...move the left boundary past the right edge!
                xA += 2.0;
            }
            else
            {
                // ...move the right boundary past the left edge!
                xB -= 2.0;
            }
        }
    }

    pos = mix(pA, pB, subVertex.x);

    // Implement a depth divide here for now
    float depth = 1.0 - (1.0 / length(pos));

    gl_Position = vec4(mix(xA, xB, subVertex.x), mix(1.0, -1.0, subVertex.y), depth, 1.0);
}
