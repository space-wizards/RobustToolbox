texture LightTexture;    
sampler lightSampler = sampler_state      
{
	Texture   = <LightTexture>;
	
	MipFilter = Point;
	MinFilter = Point;
	MagFilter = Point;
	
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
	
	MipFilter = Point;
	MinFilter = Point;
	MagFilter = Point;
	
	AddressU = Clamp;
	AddressV = Clamp;
};

texture MaskTexture;
sampler maskSampler = sampler_state
{
	Texture = <MaskTexture>;
	
	MipFilter = Point;
	MinFilter = Point;
	MagFilter = Point;
	
	AddressU = Wrap;
	AddressV = Wrap;
};

float4 AmbientLight;
float4 MaskProps;
float4 LightPositionData;
float4 DiffuseColor;

float4 DrawTilesInversePlayerViewPS(float2 TexCoord : TEXCOORD0) : COLOR0
{
	float4 pv = tex2D(playerViewSampler, TexCoord);
	float v = 1 - min(length(pv.rgb),1);
	float4 s = tex2D(sceneSampler, TexCoord);
	float4 t = tex2D(lightSampler, TexCoord);
	return float4(t.rgb + s.rgb, v);
}

float4 LightBlendPS(float2 TexCoord  : TEXCOORD0) : COLOR0
{
	float4 a = AmbientLight;
	float4 l = tex2D(lightSampler, TexCoord); //Sample light/shadows
	l = max(l,a); // Set a minimum level of light
	float4 c = tex2D(sceneSampler, TexCoord); //Sample scene color
	float4 pv = tex2D(playerViewSampler, TexCoord); // Sample player view
	
	float2 masktc = TexCoord;
	masktc.x = masktc.x * MaskProps.x * MaskProps.z;
	masktc.y = masktc.y * MaskProps.y * MaskProps.w;
	
	float4 t = tex2D(maskSampler, masktc); // Sample mask
	
	//Generate scuzz for occluded areas
	/*float4 t;	
	float2 lines;
	lines.x = 1 * (TexCoord.x * MaskProps.x + MaskProps.z);
	lines.y = 1 * (TexCoord.y * MaskProps.y + MaskProps.w);
	float s = (sin(lines.x + lines.y + lines.y) + sin(lines.x - lines.y));
	t.rgb = 0;
	if(s > 0.5)
	{
		t.rgb = 0.1;
	}*/
	
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


float4 MaskLightPS(float2 TexCoord : TEXCOORD0) : COLOR0
{
	float4 p = MaskProps;
	float2 tc = TexCoord;
	float4 d = DiffuseColor;
	float t;
	if(p.x > 0) // x is rot 90 degrees
	{ // We just flip the axes.
		t = tc.x;
		tc.x = tc.y;
		tc.y = t;
	}
	if(p.y > 0) // y is flip horizontally
	{
		tc.x = 1 - tc.x;
	}
	if(p.z > 0) // z is flip vertically
	{
		tc.y = 1 - tc.y;
	}
	
	float4 l = tex2D(lightSampler, TexCoord);
	l = float4(l.r * d.r, l.g * d.g, l.b * d.b, l.a); 
	float4 m = tex2D(maskSampler, tc);
	
	return float4(mul(l.rgb, m.r), l.a);
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



technique MaskLight
{
	pass P0
	{
		VertexShader = compile vs_2_0 vs_main();
		PixelShader = compile ps_2_0 MaskLightPS();
	}
}

technique DrawTilesInversePlayerView
{
	pass P0
	{
		VertexShader = compile vs_2_0 vs_main();
		PixelShader = compile ps_2_0 DrawTilesInversePlayerViewPS();
	}
}