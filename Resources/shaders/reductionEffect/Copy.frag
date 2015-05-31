varying vec2 v_texCoord0;

uniform sampler2D inputSampler;

vec4 CopyPS()
{
	return texture2D(inputSampler, v_texCoord0);
}

void main()
{
	gl_FragColor = CopyPS();
} 