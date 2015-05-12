attribute vec4 a_color;
attribute vec3 a_position;
attribute vec2 a_texCoord;

varying vec2 texCoords;

void main()	
{
	texCoords = a_texCoord;
	gl_Position = gl_ModelViewProjectionMatrix * gl_Vertex;
	
} 