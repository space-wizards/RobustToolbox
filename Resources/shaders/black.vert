attribute vec2 a_texCoord0;
attribute vec3 a_position;
attribute vec4 a_color;

varying vec4 Color;

uniform sampler2D sourceSampler;

void main()
{
	
	vec4 ColorSample = texture2D(sourceSampler,a_texCoord0);
	Color = 0;
	
	if(ColorSample.a > 0.0)
	{
		Color.a = 1.0;
	}
	
	
	gl_Position = gl_ModelViewProjectionMatrix * gl_Vertex;

}