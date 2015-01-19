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

float4 PerlinPS(float2 TexCoord: TEXCOORD0) : COLOR0
{ 
	float4 color;
    float2 move = float2(0,1);
    float4 perlin = tex2D(noiseSampler, (TexCoord)+xTime*move)/2;
    perlin += tex2D(noiseSampler, (TexCoord)*2+xTime*move)/4;
    perlin += tex2D(noiseSampler, (TexCoord)*4+xTime*move)/8;
    perlin += tex2D(noiseSampler, (TexCoord)*8+xTime*move)/16;
    perlin += tex2D(noiseSampler, (TexCoord)*16+xTime*move)/32;
    perlin += tex2D(noiseSampler, (TexCoord)*32+xTime*move)/32;    
    
    color.rgb = 1.0f-pow(perlin.r, xOvercast)*2.0f;
    color.a =1;

    return color;
}
 
technique PerlinNoise
{
    pass Pass0
    {
        VertexShader = compile vs_1_1 vs_main();
        PixelShader = compile ps_2_0 PerlinPS();
    }
}