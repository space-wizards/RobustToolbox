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

// xy: A, zw: B
varying vec4 fragPos;
// x: actual angle, y: horizontal (1) / vertical (-1)
varying vec2 fragAngle;

// Note: This is *not* the standard projectionMatrix!
uniform vec2 shadowLightCentre;

uniform float shadowOverlapSide;

// this constant ought to be moved to z-library
// also deal with the reference to it in shadow_cast_shared
const highp float PI = 3.1415926535897932384626433;

// expands wall edges a little to prevent holes
const highp float DEPTH_LEFTRIGHT_EXPAND_BIAS = 0.001;
// added to zbufferDepth BEFORE divide
// really just keep at 1.0 (keeps it away from the near clipping plane)
const highp float DEPTH_ZBUFFER_PREDIV_BIAS = 1.0;

void main()
{
    // aPos is clockwise, but we need anticlockwise so swap it here
    vec2 pA = aPos.zw - shadowLightCentre;
    vec2 pB = aPos.xy - shadowLightCentre;
    float xA = atan(pA.y, -pA.x);
    float xB = atan(pB.y, -pB.x);

    // expand bias
    float lrSignBias = sign(xB - xA) * DEPTH_LEFTRIGHT_EXPAND_BIAS;
    xA -= lrSignBias;
    xB += lrSignBias;

    // We need to reliably detect a clip, as opposed to, say, a backdrawn face.
    // So a clip is when the angular area is >= 180 degrees (which is not possible with a quad and always occurs when wrapping)
    if (abs(xA - xB) >= PI)
    {
        // Oh no! It clipped...

        // If such that xA is on the right side and xB is on the left:
        //  Pass 1: Adjust left boundary past left edge
        //  Pass 2: Adjust right boundary past right edge

        // If such that xA is on the left side and xB is on the right!
        //  Pass 1: Adjust left boundary past right edge
        //  Pass 2: Adjust right boundary past left edge

        if (shadowOverlapSide < 0.5)
        {
            // ...and we're adjusting the left edge...
            xA += sign(xB - xA) * PI * 2.0;
        }
        else
        {
            // ...and we're adjusting the right edge...
            xB += sign(xA - xB) * PI * 2.0;
        }
    }

    fragPos = vec4(pA, pB);
    fragAngle = vec2(mix(xA, xB, subVertex.x), abs(pA.x - pB.x) - abs(pA.y - pB.y));

    // Depth divide MUST be implemented here no matter what,
    //  because GLES SL 1.00 doesn't have gl_FragDepth.
    // Keep in mind: Ultimately, this doesn't matter, because we use the colour buffer for actual casting,
    //  and we don't really need to have correction
    float zbufferDepth = 1.0 - (1.0 / (length(mix(pA, pB, subVertex.x)) + DEPTH_ZBUFFER_PREDIV_BIAS));

    gl_Position = vec4(mix(xA, xB, subVertex.x) / PI, mix(1.0, -1.0, subVertex.y), zbufferDepth, 1.0);
}
