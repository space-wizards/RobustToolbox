uniform sampler2D inputSampler;
uniform vec2 renderTargetSize;

vec4 ComputeDistancesPS()
{
	vec4 color = texture2D(inputSampler,gl_TexCoord[0]);
	float Distance;
	if (color.a > .3)
		Distance = length(gl_TexCoord[0] - .5);
	else 
		Distance = 1.0;
	
	Distance *= renderTargetSize.x;
	
	return vec4 (Distance, 0,0,1);
		
}


void main()
{
  gl_FragColor = ComputeDistancesPS();
}
