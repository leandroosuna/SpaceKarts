#define VS_SHADERMODEL vs_5_0
#define PS_SHADERMODEL ps_5_0

float4x4 world;
float4x4 view;
float4x4 projection;
float4x4 inverseTransposeWorld;

float3 color;
float filter;
int lightEnabled;

float KA, KD, KS, shininess;

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
    float Depth : TEXCOORD3;
};
struct PSO
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
    Mipfilter = LINEAR ;
};
texture normalTexture;
sampler2D normalSampler = sampler_state
{
    Texture = (normalTexture);
    ADDRESSU = WRAP;
    ADDRESSV = WRAP;
    MINFILTER = LINEAR;
    MAGFILTER = LINEAR;
    MIPFILTER = LINEAR;
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
float zNear, zFar;

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
    
    float depthNonLinear = input.Position.z / input.Position.w;
    
    float depthLinear = (2.0 * zNear) / (zFar + zNear - depthNonLinear * (zFar - zNear));
    output.Depth = depthLinear;
    return output;
}
PSO ColorPS(VertexShaderOutput input)
{
    PSO output;
    float3 n = normalize(input.Normal.xyz);
  
    float3 normal = (n + 1.0) * 0.5;

    if(lightEnabled)
    {
        output.color = float4(color, KD);
    }
    else
    {
        output.color = float4(color, 0);
    }
    output.normal = float4(normal, KS);
    output.position = float4(input.WorldPos.xyz, shininess);
    output.bloomFilter = float4(color * (1 - lightEnabled), 1);
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
    output.bloomFilter = float4(texColor * (1 - lightEnabled), input.Depth);
    return output;
}
float3 getNormalFromMap(float2 textureCoordinates, float3 worldPosition, float3 worldNormal)
{
    float3 tangentNormal = tex2D(normalSampler, textureCoordinates).xyz * 2.0 - 1.0;

    float3 Q1 = ddx(worldPosition);
    float3 Q2 = ddy(worldPosition);
    float2 st1 = ddx(textureCoordinates);
    float2 st2 = ddy(textureCoordinates);

    worldNormal = normalize(worldNormal.xyz);
    float3 T = normalize(Q1 * st2.y - Q2 * st1.y);
    float3 B = -normalize(cross(worldNormal, T));
    float3x3 TBN = float3x3(T, B, worldNormal);

    return normalize(mul(tangentNormal, TBN));
}
PSO TexNormalPS(VertexShaderOutput input)
{
    PSO output;
    float3 n = normalize(input.Normal.xyz);
  
    float3 normal = getNormalFromMap(input.TexCoord, input.WorldPos.xyz, n);

    float3 texColor = tex2D(colorSampler, input.TexCoord).rgb;
    
    output.color = float4(texColor, KD);
    output.normal = float4(normal, KS);
    output.position = float4(input.WorldPos.xyz, shininess);
    output.bloomFilter = float4(texColor * (1 - lightEnabled), input.Depth);
    return output;
}
PSO TexNormalEmissivePS(VertexShaderOutput input)
{
    PSO output;
    float3 n = normalize(input.Normal.xyz);
    float4 texColor;
    float3 emTexColor = tex2D(emissiveSampler, input.TexCoord).rgb;
    
    if (any(emTexColor.rgb))
    {
        texColor = float4(emTexColor, 0);
        output.normal = float4(0, 0, 0, 1);
    }
    else
    {
        texColor = float4(tex2D(colorSampler, input.TexCoord).rgb, KD);
        output.normal = float4(getNormalFromMap(input.TexCoord, input.WorldPos.xyz, n), KS);
    }
    
    output.color = texColor;
    output.position = float4(input.WorldPos.xyz, shininess);
    output.bloomFilter = float4(texColor.rgb * (1 - lightEnabled), input.Depth);
    return output;
}
/*
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
*/
technique color_solid
{
    pass P0
    {
        AlphaBlendEnable = FALSE;
        VertexShader = compile VS_SHADERMODEL ColorVS();
        PixelShader = compile PS_SHADERMODEL ColorPS();
    }
}
technique color_tex
{
    pass P0
    {
        AlphaBlendEnable = FALSE;
        VertexShader = compile VS_SHADERMODEL ColorVS();
        PixelShader = compile PS_SHADERMODEL TexPS();
    }
};
technique color_tex_normal
{
    pass P0
    {
        AlphaBlendEnable = FALSE;
        VertexShader = compile VS_SHADERMODEL ColorVS();
        PixelShader = compile PS_SHADERMODEL TexNormalPS();
    }
};
technique color_tex_normal_emissive
{
    pass P0
    {
        AlphaBlendEnable = FALSE;
        VertexShader = compile VS_SHADERMODEL ColorVS();
        PixelShader = compile PS_SHADERMODEL TexNormalEmissivePS();
    }
};