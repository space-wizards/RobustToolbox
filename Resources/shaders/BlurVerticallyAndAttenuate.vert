attribute vec4 a_color;
attribute vec3 a_position;
attribute vec2 a_texCoord0;

varying vec2 TexCoord;
uniform vec2 renderTargetSize;

void main()
{
	TexCoord = a_texCoord0;
	gl_Position = gl_ModelViewProjectionMatrix * gl_Vertex + 0.5 * vec4(-1/renderTargetSize.x,1/renderTargetSize.y,0,0);
}