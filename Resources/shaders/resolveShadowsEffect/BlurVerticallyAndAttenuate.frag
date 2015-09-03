uniform vec4 MaskProps;
uniform vec4 DiffuseColor;
uniform vec2 renderTargetSize;
uniform float AttenuateShadows;

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

vec4 BlurVerticallyPS()
{
	  float sum=0;
	  float Distance = texture2D( inputSampler, gl_TexCoord[0]).b;
	  
      for (int i = 0; i < g_cKernelSize; i++)
	  {    
        sum += texture2D( inputSampler, gl_TexCoord[0] + OffsetAndWeight[i].x * mix(minBlur, maxBlur , Distance)/renderTargetSize.x * vec2(0,1) ).r * OffsetAndWeight[i].y;
      }
	  
	  float d = 2 * length(gl_TexCoord[0] - 0.5f);
	  float attenuation = pow(clamp(1.0f - d,0,1),1.0f);
	  
	  vec4 result = sum * attenuation;
	  result.a = 1;
      return result;
}

void main()
{
	gl_FragColor = BlurVerticallyPS();
}