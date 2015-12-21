#version 120
uniform vec2 renderTargetSize;

uniform sampler2D shadowMapSampler;
uniform sampler2D inputSampler;
const float minBlur = 1.0;
const float maxBlur = 20.0;
const int g_cKernelSize = 13;
const vec2 weights_offsets0 = vec2( -6, 0.002216 );
const vec2 weights_offsets1 = vec2( -5, 0.008764 );
const vec2 weights_offsets2 = vec2( -4, 0.026995 );
const vec2 weights_offsets3 = vec2( -3, 0.064759 );
const vec2 weights_offsets4 = vec2( -2, 0.120985 );
const vec2 weights_offsets5 = vec2( -1, 0.176033 );
const vec2 weights_offsets6 = vec2(  0, 0.199471 );
const vec2 weights_offsets7 = vec2(  1, 0.176033 );
const vec2 weights_offsets8 = vec2(  2, 0.120985 );
const vec2 weights_offsets9 = vec2(  3, 0.064759 );
const vec2 weights_offsets10 = vec2(  4, 0.026995 );
const vec2 weights_offsets11 = vec2(  5, 0.008764 );
const vec2 weights_offsets12 = vec2(  6, 0.002216 );
uniform vec2 OffsetAndWeight[g_cKernelSize] = vec2[g_cKernelSize]
(
    weights_offsets0,
    weights_offsets1,
    weights_offsets2,
    weights_offsets3,
    weights_offsets4,
    weights_offsets5,
    weights_offsets6,
    weights_offsets7,
    weights_offsets8,
    weights_offsets9,
    weights_offsets10,
    weights_offsets11,
    weights_offsets12
);

vec4 BlurHorizontallyPS()
{
	  float sum=0.;
	  float Distance = texture2D( inputSampler, gl_TexCoord[0].xy).b;
	  
      for (int i = 0; i < g_cKernelSize; i++)
	  {    
        sum += texture2D( inputSampler, gl_TexCoord[0].xy + OffsetAndWeight[i].x * mix(minBlur, maxBlur , Distance)/renderTargetSize.x * vec2(1,0) ).r * OffsetAndWeight[i].y;
      }
	  
	  vec4 result = vec4(sum);
	  result.b = Distance;
	  
	  result.a = 1.;
      return result;
}

void main()
{
	gl_FragColor = BlurHorizontallyPS();
}