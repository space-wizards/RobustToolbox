#version 120
uniform sampler2D sourceSampler;
uniform vec2 renderTargetSize;

vec4 ComputeDistancesPS()
{
	vec4 color = texture2D(sourceSampler,gl_TexCoord[0].xy);
	
	float Distance;
	if (color.a > .3)
		Distance = length(gl_TexCoord[0].xy - .5);
	else 
		Distance = 1.0;
		
	return vec4 (Distance, 0,0,1);
		
}


void main()
{
  gl_FragColor = ComputeDistancesPS();
}
