texture NoiseTexture;
sampler noiseSampler = sampler_state
{
	Texture = <NoiseTexture>;
	
	MipFilter = Linear;
	MinFilter = Linear;
	MagFilter = Linear;
	
	AddressU = mirror;
	AddressV = mirror;
};

texture SceneTexture;
sampler sceneSampler = sampler_state
{
	Texture = <SceneTexture>;
	
	MipFilter = Linear;
	MinFilter = Linear;
	MagFilter = Linear;
	
	AddressU = Clamp;
	AddressV = Clamp;
};

float4x4 _projectionMatrix;
// Vertex shader input structure
struct VS_INPUT
{
    float4 Position   : POSITION;
    float2 Texture    : TEXCOORD0;
};

// Vertex shader output structure
struct VS_OUTPUT
{
    float4 Position   : POSITION;
    float2 Texture    : TEXCOORD0;
};

VS_OUTPUT vs_main( VS_INPUT In )
{
    VS_OUTPUT Out;                      //create an output vertex

	Out.Position = float4(mul(In.Position, _projectionMatrix).xyz, 1.0f);
	
    Out.Texture  = In.Texture;          //copy original texcoords

    return Out;                         //return output vertex
}

float xTime;
float xOvercast;
//float3 shadowColor;
//float3 midtoneColor;
//float3 highlightColor;
#define pi 3.141592
#define speed 0.3f
#define colorspeed 5

float4 PerlinPS(float2 TexCoord: TEXCOORD0) : COLOR0
{ 
	float4 scene = tex2D(sceneSampler, TexCoord);
	float4 color;
	float timeConst = xTime * speed;
    float2 move1 = -1 * timeConst;
    float2 move2 = float2(cos(timeConst / 2),cos(timeConst));
    float4 perlin = tex2D(noiseSampler, (TexCoord)+timeConst*move1*sin(timeConst * 1.2))/2;
    perlin += tex2D(noiseSampler, (TexCoord)*2+timeConst*move1*cos(timeConst * 1.1)*2)/4;
    perlin += tex2D(noiseSampler, (TexCoord)*4+timeConst*move2*sin(timeConst * 1.3)*2)/8;
    perlin += tex2D(noiseSampler, (TexCoord)*8+timeConst*move2*cos(timeConst * 1.4)*2)/16;
    perlin += tex2D(noiseSampler, (TexCoord)*16+timeConst*move1*sin(timeConst * 1.7))/32;
    perlin += tex2D(noiseSampler, (TexCoord)*32+timeConst*move2*cos(timeConst * 1.5)*2)/32;    
	float c1 = pow(sin(timeConst * pi * colorspeed + 2 * pi / 3), 2);
	float c2 = pow(sin(timeConst * pi * colorspeed), 2);
	float c3 = pow(sin(timeConst * pi * colorspeed - 2 * pi / 3), 2);
	
	float3 shadowColor = float3(c1, c2, c3);
	float3 midtoneColor = float3(c3, c1, c2);
	float3 highlightColor = float3(c2, c3, c1);
    color.rgb = clamp((1.0f-pow(perlin.r, xOvercast)*2.0f) + 0.5f, 0, 1);
    color.a = 1;
	float3 s = clamp(pow(2 * (clamp(color.r, 0, 0.5f) - 0.5), 2), 0, 1) * shadowColor;
	float3 m = clamp(pow(2 * (clamp(color.r, 0.5f, 1) - 1), 2), 0, 1) * midtoneColor;
	if(color.r <= 0.5)
	{
		m = clamp(pow(2 * (clamp(color.r, 0, 0.5f)), 2), 0, 1) * midtoneColor;
	}
	float3 hl = clamp(pow(2 * (clamp(color.r, 0.5f, 1) - 0.5), 2), 0, 1) * highlightColor;
	
	color = float4(s + m + hl, 1);
	
	float3 o; // calculate hard light
	o.r = scene.r <= 0.5 ? 2 * color.r * scene.r : 1 - (2 * (1 - color.r) * (1 - scene.r) );
	o.g = scene.g <= 0.5 ? 2 * color.g * scene.g : 1 - (2 * (1 - color.g) * (1 - scene.g) );
	o.b = scene.b <= 0.5 ? 2 * color.b * scene.b : 1 - (2 * (1 - color.b) * (1 - scene.b) );
	
	//color = color * scene;
	return float4(o,1);
    //return color;
}
 
technique PerlinNoise
{
    pass Pass0
    {
        VertexShader = compile vs_1_1 vs_main();
        PixelShader = compile ps_2_0 PerlinPS();
    }
}