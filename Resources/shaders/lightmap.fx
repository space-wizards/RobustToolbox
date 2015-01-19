texture light1;
sampler light1s = sampler_state
{
	Texture = <light1>;
	
	MipFilter = Linear;
	MinFilter = Linear;
	MagFilter = Linear;
	
	AddressU = Clamp;
	AddressV = Clamp;
};
texture light2;
sampler light2s = sampler_state
{
	Texture = <light2>;
	
	MipFilter = Linear;
	MinFilter = Linear;
	MagFilter = Linear;
	
	AddressU = Clamp;
	AddressV = Clamp;
};
texture light3;
sampler light3s = sampler_state
{
	Texture = <light3>;
	
	MipFilter = Linear;
	MinFilter = Linear;
	MagFilter = Linear;
	
	AddressU = Clamp;
	AddressV = Clamp;
};
texture light4;
sampler light4s = sampler_state
{
	Texture = <light4>;
	
	MipFilter = Linear;
	MinFilter = Linear;
	MagFilter = Linear;
	
	AddressU = Clamp;
	AddressV = Clamp;
};
texture light5;
sampler light5s = sampler_state
{
	Texture = <light5>;
	
	MipFilter = Linear;
	MinFilter = Linear;
	MagFilter = Linear;
	
	AddressU = Clamp;
	AddressV = Clamp;
};
texture light6;
sampler light6s = sampler_state
{
	Texture = <light6>;
	
	MipFilter = Linear;
	MinFilter = Linear;
	MagFilter = Linear;
	
	AddressU = Clamp;
	AddressV = Clamp;
};
texture SceneTexture;
sampler sceneSampler = sampler_state
{
	Texture = <SceneTexture>;
	
	MipFilter = Point;
	MinFilter = Point;
	MagFilter = Point;
	
	AddressU = Clamp;
	AddressV = Clamp;
};
#define NUM_LIGHTS 6
float4 LightPosData[NUM_LIGHTS];
float4 Colors[NUM_LIGHTS];

float4 PreLightBlendPS(float2 TexCoord : TEXCOORD0) : COLOR0
{
	float4 l[NUM_LIGHTS];
	float2 ltc[NUM_LIGHTS];
	for(int i = 0;i<NUM_LIGHTS;i++)
	{
		ltc[i] = float2((TexCoord.x - LightPosData[i].x) * LightPosData[i].z, (TexCoord.y - LightPosData[i].y) * LightPosData[i].w);
	}
	l[0] = tex2D(light1s, ltc[0]);
	l[1] = tex2D(light2s, ltc[1]);
	l[2] = tex2D(light3s, ltc[2]);
	l[3] = tex2D(light4s, ltc[3]);
	l[4] = tex2D(light5s, ltc[4]);
	l[5] = tex2D(light6s, ltc[5]);
	
	l[0].rgb = l[0].rgb * Colors[0].rgb;
	l[1].rgb = l[1].rgb * Colors[1].rgb;
	l[2].rgb = l[2].rgb * Colors[2].rgb;
	l[3].rgb = l[3].rgb * Colors[3].rgb;
	l[4].rgb = l[4].rgb * Colors[4].rgb;
	l[5].rgb = l[5].rgb * Colors[5].rgb;
	
	
	float4 s = tex2D(sceneSampler, TexCoord); // sample existing lights
	
	//Add the lights together	
	float r = sqrt(pow(l[0].r, 2) + pow(l[1].r, 2) + pow(l[2].r, 2) + pow(l[3].r, 2) + pow(l[4].r, 2) + pow(l[5].r, 2) + pow(s.r, 2));
	float g = sqrt(pow(l[0].g, 2) + pow(l[1].g, 2) + pow(l[2].g, 2) + pow(l[3].g, 2) + pow(l[4].g, 2) + pow(l[5].g, 2) + pow(s.g, 2));
	float b = sqrt(pow(l[0].b, 2) + pow(l[1].b, 2) + pow(l[2].b, 2) + pow(l[3].b, 2) + pow(l[4].b, 2) + pow(l[5].b, 2) + pow(s.b, 2));
	float4 c = float4(r,g,b, 1);
		
	//Return the light color
	return float4(mul(c.rgb,min(1, 1/max(c.r, max(c.g,c.b)))),1);
}

float4x4 _projectionMatrix;

// Vertex shader input structure
struct VS_INPUT
{
    float4 Position   : POSITION;
    float2 Texture    : TEXCOORD0;
};

float2 TextureDimensions;

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

technique PreLightBlend
{
	pass P0
	{
		VertexShader = compile vs_2_0 vs_main();
		PixelShader = compile ps_2_0 PreLightBlendPS();
	}
}