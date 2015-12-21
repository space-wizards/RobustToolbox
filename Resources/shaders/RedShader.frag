#version 120
uniform sampler2D TextureUnit0;


void main()
{
	
	gl_FragColor =   texture2D(TextureUnit0,gl_TexCoord[0].xy) + vec4(.5,0,0,0);
}
