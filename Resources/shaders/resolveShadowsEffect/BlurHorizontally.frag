uniform vec2 renderTargetSize;


uniform sampler2D shadowMapSampler;
uniform sampler2D inputSampler;
const float minBlur = 1.0f;
const float maxBlur = 20.0f;
const int g_cKernelSize = 13;
const vec2 OffsetAndWeight[g_cKernelSize] =
{
    { -6, 0.002216 },
    { -5, 0.008764 },
    { -4, 0.026995 },
    { -3, 0.064759 },
    { -2, 0.120985 },
    { -1, 0.176033 },
    {  0, 0.199471 },
    {  1, 0.176033 },
    {  2, 0.120985 },
    {  3, 0.064759 },
    {  4, 0.026995 },
    {  5, 0.008764 },
    {  6, 0.002216 },
};

varying vec2 TexCoord;

vec4 BlurHorizontallyPS()
{
	  float sum=0;
	  float Distance = texture2D( inputSampler, TexCoord).b;
	  
      for (int i = 0; i < g_cKernelSize; i++)
	  {    
        sum += texture2D( inputSampler, TexCoord + OffsetAndWeight[i].x * mix(minBlur, maxBlur , Distance)/renderTargetSize.x * vec2(1,0) ).r * OffsetAndWeight[i].y;
      }
	  
	  vec4 result = sum;
	  result.b = Distance;
	  
	  result.a = 1;
      return result;
}

void main()
{
	gl_FragColor = BlurHorizontallyPS();
}