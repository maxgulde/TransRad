/* Shader for thermal analysis

Provides techniques for the drawing of textures
Multiply: Multiplies the hemicube texture with the multipliermap

Author: Max Gulde
Last Update: 2018-05-30
*/

#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

Texture2D TexHemi;
sampler2D TexHemiSampler = sampler_state
{
	Texture = <TexHemi>;
};

Texture2D TexMuMap;
sampler2D TexMuMapSampler = sampler_state
{
	Texture = <TexMuMap>;
};

struct VertexShaderOutput
{
	float4 Position : SV_POSITION;
	float4 Color : COLOR0;
	float2 TextureCoordinates : TEXCOORD0;
};

float4 MainPS(VertexShaderOutput input) : COLOR
{
	float4 color = tex2D(TexHemiSampler, input.TextureCoordinates) * input.Color;
	color.rgb *= tex2D(TexMuMapSampler, input.TextureCoordinates);
	return color;
}

technique Multiply
{
	pass P0
	{
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};