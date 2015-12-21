#version 120


uniform sampler2D inputSampler;

vec4 CopyPS()
{
	return texture2D(inputSampler, gl_TexCoord[0].xy);
}

void main()
{
	gl_FragColor = CopyPS();
} 