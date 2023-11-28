#define VS_SHADERMODEL vs_5_0
#define PS_SHADERMODEL ps_5_0

#include "lightUtil.fxh"

float4x4 world;
float4x4 view;
float4x4 projection;
float4x4 inverseTransposeWorld;

float3 color;
float filter;
struct VertexShaderInput
{
    float4 Position : POSITION;
    float3 Normal : NORMAL0; 
    float2 TexCoord: TEXCOORD;
};

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float2 TexCoord : TEXCOORD0;
    float4 Normal : TEXCOORD1;
    float4 WorldPos : TEXCOORD2;
};
struct PSO
{
    float4 color : COLOR0;
    float4 normal : COLOR1;
    float4 position : COLOR2;
    
};

struct PSOB
{
    float4 color : COLOR0;
    float4 normal : COLOR1;
    float4 position : COLOR2;
    float4 bloomFilter : COLOR3;
};



texture colorTexture;
sampler2D colorSampler = sampler_state
{
    Texture = (colorTexture);
    AddressU = WRAP;
    AddressV = WRAP;
    MagFilter = LINEAR;
    MinFilter = LINEAR;
    Mipfilter = LINEAR;
};
texture emissiveTexture;
sampler2D emissiveSampler = sampler_state
{
    Texture = (emissiveTexture);
    AddressU = WRAP;
    AddressV = WRAP;
    MagFilter = LINEAR;
    MinFilter = LINEAR;
    Mipfilter = LINEAR;
};
VertexShaderOutput ColorVS(in VertexShaderInput input)
{
    VertexShaderOutput output = (VertexShaderOutput) 0;
    float4 worldPosition = mul(input.Position, world);
    float4 viewPosition = mul(worldPosition, view);
    float4 screenPos = mul(viewPosition, projection);
    output.WorldPos = worldPosition;
    output.Position = screenPos;
    output.Normal = mul(float4(input.Normal, 1), inverseTransposeWorld);
    output.TexCoord = input.TexCoord ;
    return output;
}
PSO ColorPS(VertexShaderOutput input)
{
    PSO output;
    float3 n = normalize(input.Normal.xyz);
  
    float3 normal = (n + 1.0) * 0.5;

    output.color = float4(color, KD);
    output.normal = float4(normal, KS);
    output.position = float4(input.WorldPos.xyz, shininess);
    return output;
}
PSO LightDisPS(VertexShaderOutput input)
{
    PSO output;
    float3 n = normalize(input.Normal.xyz);
  
    float3 normal = (n + 1.0) * 0.5;
        
    output.color = float4(color, 0);
    output.normal = float4(0, 0, 0, 1); //rgb=0 light dis, a=1 bloom en
    output.position = float4(input.WorldPos.xyz, 0);
    return output;
}
PSOB LightDisBPS(VertexShaderOutput input)
{
    PSO prev = LightDisPS(input);
    PSOB output;
    output.color = prev.color;
    output.normal = prev.normal;
    output.position = prev.position;
    output.bloomFilter = prev.color * prev.normal.a;
    return output;
}
PSO TexPS(VertexShaderOutput input)
{
    PSO output;
    float3 n = normalize(input.Normal.xyz);
  
    float3 normal = (n + 1.0) * 0.5;

    float3 texColor = tex2D(colorSampler, input.TexCoord).rgb;
    
    output.color = float4(texColor, KD);
    output.normal = float4(normal, KS);
    output.position = float4(input.WorldPos.xyz, shininess);
    return output;
}
PSOB TexBPS(VertexShaderOutput input)
{
    PSO prev = TexPS(input);
    PSOB output;
    output.color = prev.color;
    output.normal = prev.normal;
    output.position = prev.position;
    output.bloomFilter = float4(0, 0, 0, 1);

    return output;
}
PSO ColorEmissiveTexPS(VertexShaderOutput input)
{
    PSO output;
    float3 n = normalize(input.Normal.xyz);
  
    float3 normal = (n + 1.0) * 0.5;

    float3 texColor;
    
    float3 emTexColor = tex2D(emissiveSampler, input.TexCoord).rgb;
    
    if (any(emTexColor.rgb))
    {
        texColor = emTexColor;
        output.normal = float4(0, 0, 0, 1); //rgb=0 light dis, a=1 bloom en
    }
    else
    {
        texColor = tex2D(colorSampler, input.TexCoord).rgb;
        output.normal = float4(normal, KS);
    }
    
    output.color = float4(texColor, KD);
    
    output.position = float4(input.WorldPos.xyz, shininess);
    return output;
}

PSOB ColorEmissiveTexBPS(VertexShaderOutput input)
{
    PSO prev = ColorEmissiveTexPS(input);
        
    PSOB output;
    output.color = prev.color;
    output.normal = prev.normal;
    output.position = prev.position;
    
    if (prev.color.a == 0)
        output.bloomFilter = float4(prev.color.rgb, 1);    
    else
        output.bloomFilter = float4(0, 0, 0, 1);
    return output;
}

technique basicColor
{
    pass P0
    {
        AlphaBlendEnable = FALSE;
        VertexShader = compile VS_SHADERMODEL ColorVS();
        PixelShader = compile PS_SHADERMODEL ColorPS();
    }
}
technique color_lightDis_bloomEn
{
    pass P0
    {
        AlphaBlendEnable = FALSE;
        VertexShader = compile VS_SHADERMODEL ColorVS();
        PixelShader = compile PS_SHADERMODEL LightDisBPS();
    }
};
technique color_lightDis
{
    pass P0
    {
        AlphaBlendEnable = FALSE;
        VertexShader = compile VS_SHADERMODEL ColorVS();
        PixelShader = compile PS_SHADERMODEL LightDisPS();
    }
};

technique colorTex_lightEn_bloomEn
{
    pass P0
    {
        AlphaBlendEnable = FALSE;
        VertexShader = compile VS_SHADERMODEL ColorVS();
        PixelShader = compile PS_SHADERMODEL TexBPS();
    }
};
technique colorTex_lightEn
{
    pass P0
    {
        AlphaBlendEnable = FALSE;
        VertexShader = compile VS_SHADERMODEL ColorVS();
        PixelShader = compile PS_SHADERMODEL TexPS();
    }
};
technique colorEmTex_lightEn_bloomEn
{
    pass P0
    {
        AlphaBlendEnable = FALSE;
        VertexShader = compile VS_SHADERMODEL ColorVS();
        PixelShader = compile PS_SHADERMODEL ColorEmissiveTexBPS();
    }
};
technique colorEmTex_lightEn
{
    pass P0
    {
        AlphaBlendEnable = FALSE;
        VertexShader = compile VS_SHADERMODEL ColorVS();
        PixelShader = compile PS_SHADERMODEL ColorEmissiveTexPS();
    }
};