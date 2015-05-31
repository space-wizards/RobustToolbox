varying vec2 v_texCoord0;
uniform sampler2D inputSampler;
uniform vec2 renderTargetSize;

vec4 ComputeDistancesPS()
{
	vec4 color = texture2D(inputSampler,v_texCoord0);
	float Distance;
	if (color.a > .3)
		Distance = length(v_texCoord0 - .5);
	else 
		Distance = 1.0;
	
	Distance *= renderTargetSize.x;
	
	return vec4 (Distance, 0,0,1);
		
}


void main()
{
  gl_FragColor = ComputeDistancesPS();
}
