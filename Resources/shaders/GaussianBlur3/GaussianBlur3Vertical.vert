attribute vec4 a_color;
attribute vec3 a_position;
attribute vec2 a_texCoord0;

varying vec2 TexCoord;

void main()
{
	// transform the vertex position
    gl_Position = gl_ModelViewProjectionMatrix * gl_Vertex;

    // transform the texture coordinates
    gl_TexCoord[0] = gl_TextureMatrix[0] * gl_MultiTexCoord0;

    // forward the vertex color
   gl_FrontColor = gl_Color;
}
