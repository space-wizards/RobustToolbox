#define numdecals = 5
sampler2D sampler0;
texture2D tex1;
sampler sampler1 = sampler_state
{
	Texture = <tex1>;
	
	//Filter = ANSIOTROPIC;
    //MipFilter = ANISOTROPIC;
    //MinFilter = POINT;
    //MagFilter = ANISOTROPIC;
};


float4x4 _projectionMatrix;
float4 decalParms1[10];
float4 decalParms2[10];
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

    Out.Position = float4(mul(In.Position, _projectionMatrix).xyz, 1.0f);  //apply vertex transformation
    Out.Texture  = In.Texture;          //copy original texcoords

    return Out;                         //return output vertex
}

float4 DColor(float2 texCoord : TEXCOORD0) : COLOR0
{
	return tex2D(sampler1, texCoord);
}

float2 ATXC(float2 texCoord, int i)
{
	return float2(((texCoord.x - decalParms1[i].x) * decalParms2[i].x) + decalParms2[i].z, ((texCoord.y - decalParms1[i].y) * decalParms2[i].y) + decalParms2[i].w);
}

int sum(float4 tosum)
{
	return tosum.x+tosum.y+tosum.z+tosum.w;
}

float4 computeColor(float2 texCoord, float2 atlasCoord, float baseAlpha, int i: TEXCOORD0)
{
	int inbounds = saturate(sum(saturate(float4(texCoord.x - decalParms1[i].x, 
						decalParms1[i].z - texCoord.x,
						texCoord.y - decalParms1[i].y,
						decalParms1[i].w - texCoord.y))));
	
	float4 decalcolor = DColor(atlasCoord);
	return float4(decalcolor.xyz, decalcolor.w * baseAlpha * inbounds);
}

float4 DecalShader(float2 texCoord : TEXCOORD0) : COLOR0
{
	//return float4(texCoord.x, texCoord.y, 0,1);
	float2 ATXCoord;
	
	float4 outputColor = tex2D(sampler0, texCoord);
	
	float4 decalColor = float4(0,0,0,0);
	float4 tempColor = float4(0,0,0,0);
	int numcolors = 0;
	for( uint i = 0;i < 5; i++ )
	{
		ATXCoord = ATXC(texCoord, i);
		tempColor = computeColor(texCoord, ATXCoord, outputColor.w, i);
		float purple = ceil(saturate(length(tempColor.xyz - float3(1,0,1))));
		tempColor = decalColor + mul(tempColor, purple);
		numcolors = numcolors + tempColor.w;
		decalColor = tempColor;
	}
	
	decalColor = mul(decalColor, numcolors);
	
	//1 if sampled color is the gutter purple color.
	//float purple = floor(saturate(length(float3(decalColor.xyz - float3(1,0,1)))));
	
	return outputColor + decalColor;
}

technique drawWithDecal
{
	pass P0
	{
		VertexShader = compile vs_2_a vs_main();
		PixelShader = compile ps_2_a DecalShader();
	}
}
