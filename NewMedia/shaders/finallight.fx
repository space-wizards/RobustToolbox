texture LightTexture;    
sampler lightSampler = sampler_state      
{
	Texture   = <LightTexture>;
	
	MipFilter = Linear;
	MinFilter = Linear;
	MagFilter = Linear;
	
	AddressU  = Clamp;
	AddressV  = Clamp;
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

texture PlayerViewTexture;
sampler playerViewSampler = sampler_state
{
	Texture = <PlayerViewTexture>;
	
	MipFilter = Linear;
	MinFilter = Linear;
	MagFilter = Linear;
	
	AddressU = Clamp;
	AddressV = Clamp;
};

texture OutOfViewTexture;
sampler outOfViewSampler = sampler_state
{
	Texture = <OutOfViewTexture>;
	
	MipFilter = Linear;
	MinFilter = Linear;
	MagFilter = Linear;
	
	AddressU = Wrap;
	AddressV = Wrap;
};

float4 AmbientLight;
float4 MaskProps;
float4 DiffuseColor;

float4 LightBlendPS(float2 TexCoord  : TEXCOORD0) : COLOR0
{
	float4 a = AmbientLight;
	float4 l = tex2D(lightSampler, TexCoord); //Sample light/shadows
	l = max(l,a); // Set a minimum level of light
	float4 c = tex2D(sceneSampler, TexCoord); //Sample scene color
	float4 pv = mul(tex2D(playerViewSampler, TexCoord), 2); // Sample player view
	
	float2 masktc = TexCoord;
	masktc.x = masktc.x * MaskProps.x;
	masktc.y = masktc.y * MaskProps.y;
	
	float4 t = mul(tex2D(outOfViewSampler, masktc), 0.5); // Sample mask
	
	//Generate scuzz for occluded areas
	t.a = 1;
	//End generate scuzz
	
	float3 h; // calculate hard light
	h.r = l.r <= 0.5 ? 2 * l.r * c.r : 1 - (2 * (1 - l.r) * (1 - c.r) );
	h.g = l.g <= 0.5 ? 2 * l.g * c.g : 1 - (2 * (1 - l.g) * (1 - c.g) );
	h.b = l.b <= 0.5 ? 2 * l.b * c.b : 1 - (2 * (1 - l.b) * (1 - c.b) );

	float4 result;
	result = float4(mul(max(c.rgb*l.rgb, h.rgb), pv.r), 1);
	result = result + mul(t, 1 - pv.r);
	return result;
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

technique FinalLightBlend
{
	pass P0
	{
		
		VertexShader = compile vs_2_0 vs_main();
		PixelShader = compile ps_2_0 LightBlendPS();
	}
}
