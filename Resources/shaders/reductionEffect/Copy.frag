

uniform sampler2D inputSampler;

vec4 CopyPS()
{
	return texture2D(inputSampler, gl_TexCoord[0]);
}

void main()
{
	gl_FragColor = CopyPS();
} 