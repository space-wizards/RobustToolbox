#ifndef HAS_VARYING_ATTRIBUTE
#define texture2D texture
#define varying out
#define attribute in
#endif


// Vertex position.
/*layout (location = 0)*/ attribute vec2 aPos;
// Texture coordinates.
/*layout (location = 1)*/ attribute vec2 tCoord;

varying vec2 UV;

void main()
{
    UV = tCoord;

    gl_Position = vec4(aPos, 0.0, 1.0);
}
