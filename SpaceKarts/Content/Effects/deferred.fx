#define VS_SHADERMODEL vs_5_0
#define PS_SHADERMODEL ps_5_0
#include "draw.fxh"
#include "lightUtil.fxh"
#include "blur.fxh"

float4x4 world;
float4x4 view;
float4x4 projection;


float2 screenSize;
float radius;

struct PVSI
{
    float4 Position : POSITION;
    float2 TexCoord : TEXCOORD;
};
struct PVSO
{
    float4 Position : SV_POSITION;
    float2 TexCoord : TEXCOORD;
};
struct PSO
{
    float4 color : COLOR0;
    float4 blurH : COLOR1;
    float4 blurV : COLOR2;
};

texture colorMap;
sampler colorSampler = sampler_state
{
    Texture = (colorMap);
    AddressU = CLAMP;
    AddressV = CLAMP;
    MagFilter = LINEAR;
    MinFilter = LINEAR;
    Mipfilter = LINEAR;
};
texture normalMap;
sampler normalMapSampler = sampler_state
{
    Texture = (normalMap);
    AddressU = CLAMP;
    AddressV = CLAMP;
    MagFilter = LINEAR;
    MinFilter = LINEAR;
    Mipfilter = LINEAR;
};

texture positionMap;
sampler positionMapSampler = sampler_state
{
    Texture = (positionMap);
    AddressU = CLAMP;
    AddressV = CLAMP;
    MagFilter = LINEAR;
    MinFilter = LINEAR;
    Mipfilter = LINEAR;
};
texture prevPositionMap;
sampler prevPositionMapSampler = sampler_state
{
    Texture = (prevPositionMap);
    AddressU = CLAMP;
    AddressV = CLAMP;
    MagFilter = LINEAR;
    MinFilter = LINEAR;
    Mipfilter = LINEAR;
};
texture lightMap;
sampler lightMapSampler = sampler_state
{
    Texture = (lightMap);
    AddressU = CLAMP;
    AddressV = CLAMP;
    MagFilter = LINEAR;
    MinFilter = LINEAR;
    Mipfilter = LINEAR;
};
texture bloomFilter;
sampler bloomFilterSampler = sampler_state
{
    Texture = (bloomFilter);
    AddressU = CLAMP;
    AddressV = CLAMP;
    MagFilter = POINT;
    MinFilter = POINT;
    Mipfilter = POINT;
};
PVSO PostVS(PVSI input)
{
    PVSO output;
    output.Position = input.Position;
    output.TexCoord = input.TexCoord;
    return output;
}

PVSO PointLightVS(PVSI input)
{
    PVSO output;
    float4 worldPosition = mul(input.Position, world);
    float4 viewPosition = mul(worldPosition, view);
    float4 screenPos = mul(viewPosition, projection);
    
    //output.ScreenPos = screenPos;
    output.Position = screenPos;
    output.TexCoord = input.TexCoord;
    return output;
}

float4 ColorPS(PVSO input) : COLOR
{
    return float4(tex2D(colorSampler, input.TexCoord).rgb, 1);
}
float4 DepthPS(PVSO input) : COLOR
{
    
    float d = distance(tex2D(positionMapSampler, input.TexCoord).rgb, float3(0, 0, 0)) / 2000;
    return float4(d, d, d, 1);
}
float4 NormalPS(PVSO input) : COLOR
{
    float3 normal = tex2D(normalMapSampler, input.TexCoord).rgb;
    return float4(normal, 1);
}
float4 TypePS(PVSO input) : COLOR
{
    float type = tex2D(normalMapSampler, input.TexCoord).a;
    return float4(type, type, type, 1);
}
float4 AmbientLightPS(PVSO input) : COLOR
{
    float4 colorRaw = tex2D(colorSampler, input.TexCoord);
    float3 color = colorRaw.rgb;
    float KD = colorRaw.a;
    
    float4 normalRaw = tex2D(normalMapSampler, input.TexCoord);
    
    if (KD == 0.0 )
    {
        return float4(1,1,1, 1);
    }
    
    float KS = normalRaw.a;
    float4 worldRaw = tex2D(positionMapSampler, input.TexCoord);
    float shininess = worldRaw.a * 60;
    
    float3 normal = normalize((normalRaw.rgb * 2.0) - 1);
    float3 worldPos = worldRaw.rgb;
    
    return float4(getPixelAmbient(worldPos, normal, KD, KS, shininess), 1);
}
static const int kernel_r = 6;
static const int kernel_size = 13;
static const float Kernel[kernel_size] =
{
    0.002216, 0.008764, 0.026995, 0.064759, 0.120985, 0.176033, 0.199471, 0.176033, 0.120985, 0.064759, 0.026995, 0.008764, 0.002216,
};

PSO AmbientLightBPS(PVSO input)
{
    float4 prev = AmbientLightPS(input);
    PSO output;
    output.color = prev;
    
    float4 hColor = float4(0, 0, 0, 1);
    float4 vColor = float4(0, 0, 0, 1);
    
    for (int i = 0; i < kernel_size; i++)
    {
        float2 scaledTextureCoordinatesH = input.TexCoord + float2((float) (i - kernel_r) / screenSize.x, 0);
        float2 scaledTextureCoordinatesV = input.TexCoord + float2(0, (float) (i - kernel_r) / screenSize.y);
        hColor += tex2D(bloomFilterSampler, scaledTextureCoordinatesH) * Kernel[i];
        vColor += tex2D(bloomFilterSampler, scaledTextureCoordinatesV) * Kernel[i];
    }
    
    output.blurH = hColor;
    output.blurV = vColor;
    return output;
}
float sqr(float x)
{
    return x * x;
}
float attenuate_no_cusp(float distance, float radius, float max_intensity, float falloff)
{
    float s = distance / radius;

    if (s >= 1.0)
        return 0.0;

    float s2 = sqr(s);

    return max_intensity * sqr(1 - s2) / (1 + falloff * s2);
}
float4 PointLightPS(PVSO input) : COLOR
{
    float2 sceneCoord = input.Position.xy / screenSize;
    
    float4 colorRaw = tex2D(colorSampler, sceneCoord);
    float3 color = colorRaw.rgb;
    float KD = colorRaw.a;
    
    float4 normalRaw = tex2D(normalMapSampler, sceneCoord);
    
    if (KD == 0.0)
    {
        return float4(1,1,1, 1);
    }
    
    float KS = normalRaw.a;
    float4 worldRaw = tex2D(positionMapSampler, sceneCoord);
    float shininess = worldRaw.a * 60;
   
    
    float3 normal = normalize((normalRaw.rgb * 2.0) - 1);
    float3 worldPos = worldRaw.rgb;

    //return float4(lightDiffuseColor * 0.5, 1);
    
    //float scaling = 1- smoothstep(0, radius, distance(worldPos, lightPosition));
    
    float scaling = attenuate_no_cusp(distance(worldPos, lightPosition), radius, 3, 6);
    //red border for debug
    //if (scaling == 0)
    //    return float4(1, 0, 0, 1);
    
    return float4(getPixelColorNoAmbient(worldPos, normal, KD, KS, shininess) * scaling, 1);
    
}
PSO PointLightBPS(PVSO input)
{
    float4 prev = PointLightPS(input);
    PSO output = (PSO)0;
    output.color = prev;
    output.blurH = float4(0, 0, 0, 1);
    output.blurV = float4(0, 0, 0, 1);
    return output;
}
texture blurH;
sampler2D blurHSampler = sampler_state
{
    Texture = (blurH);
    MagFilter = Linear;
    MinFilter = Linear;
    AddressU = Clamp;
    AddressV = Clamp;
};
texture blurV;
sampler2D blurVSampler = sampler_state
{
    Texture = (blurV);
    MagFilter = Linear;
    MinFilter = Linear;
    AddressU = Clamp;
    AddressV = Clamp;
};

float4 IntegrateBPS(PVSO input) : COLOR
{
    float4 colorRaw = tex2D(colorSampler, input.TexCoord);
    float3 color = colorRaw.rgb;
    float4 lightRaw = tex2D(lightMapSampler, input.TexCoord);
    float3 light = lightRaw.rgb;
    
    float4 blurHColor = tex2D(blurHSampler, input.TexCoord);
    float4 blurVColor = tex2D(blurVSampler, input.TexCoord);
    
    return float4(color*0.8 * light, 1) + blurHColor * 1.2 + blurVColor * 1.2;
}
int motionBlurIntensity;
float4 IntegrateMBPS(PVSO input) : COLOR
{
    float3 baseColor = tex2D(colorSampler, input.TexCoord).rgb;
    float3 light = tex2D(lightMapSampler, input.TexCoord).rgb;

    float3 position = tex2D(positionMapSampler, input.TexCoord).xyz;
    float3 prevPosition = tex2D(prevPositionMapSampler, input.TexCoord).xyz;
    
    float4 screenPos = mul(mul(float4(position, 1), view), projection);
    float2 screenPos2D = screenPos.xy / screenPos.z;
    float4 prevScreenPos = mul(mul(float4(prevPosition, 1), view), projection);
    float2 prevScreenPos2D = prevScreenPos.xy / prevScreenPos.z;
    
    float2 velocity = (screenPos2D - prevScreenPos2D);
    velocity.y = -velocity.y;
    /*
    if (velocity.x == screenPos2D.x && velocity.y == screenPos2D.y)
        return float4(1, 0, 0, 1);
    if(velocity.x == screenPos2D.x && velocity.y == screenPos2D.y)
        return float4(1, 0, 1, 1);
    if (velocity.x == 0 && velocity.y == 0)
        return float4(1,0, 0, 1);
    return float4(velocity.x, velocity.y,0,1);
    */
    
    
    float2 tex = input.TexCoord;
    tex += velocity;
    
    for (int i = 1; i < motionBlurIntensity; ++i, tex += velocity)
    {
	    float3 currentColor = tex2D(colorSampler, tex).rgb;
	    baseColor += currentColor;
    }
    float3 finalColor = (baseColor / motionBlurIntensity) * light;
    return float4(finalColor, 1);
}
float4 IntegratePS(PVSO input) : COLOR
{
    
    float4 colorRaw = tex2D(colorSampler, input.TexCoord);
    float3 color = colorRaw.rgb;

    float3 light = tex2D(lightMapSampler, input.TexCoord).rgb;

    float filter = colorRaw.a;

    if(filter == 0)
        return float4(color, 1);
    
    return float4(color*light, 1);
    
}

technique post_color
{
    pass P0
    {
        AlphaBlendEnable = FALSE;
        VertexShader = compile VS_SHADERMODEL PostVS();
        PixelShader = compile PS_SHADERMODEL ColorPS();
    }
};
technique post_normal
{
    pass P0
    {
        AlphaBlendEnable = FALSE;
        VertexShader = compile VS_SHADERMODEL PostVS();
        PixelShader = compile PS_SHADERMODEL NormalPS();
    }
};
technique post_depth
{
    pass P0
    {
        AlphaBlendEnable = FALSE;
        VertexShader = compile VS_SHADERMODEL PostVS();
        PixelShader = compile PS_SHADERMODEL DepthPS();
    }
};
technique post_type
{
    pass P0
    {
        AlphaBlendEnable = FALSE;
        VertexShader = compile VS_SHADERMODEL PostVS();
        PixelShader = compile PS_SHADERMODEL TypePS();
    }
};

technique point_light_bloom
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL PointLightVS();
        PixelShader = compile PS_SHADERMODEL PointLightBPS();
    }
}
technique point_light
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL PointLightVS();
        PixelShader = compile PS_SHADERMODEL PointLightPS();
    }
}
technique ambient_light_bloom
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL PostVS();
        PixelShader = compile PS_SHADERMODEL AmbientLightBPS();
    }
}
technique ambient_light
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL PostVS();
        PixelShader = compile PS_SHADERMODEL AmbientLightPS();
    }
}
technique integrate
{
    pass P0
    {
        AlphaBlendEnable = FALSE;
        VertexShader = compile VS_SHADERMODEL PostVS();
        PixelShader = compile PS_SHADERMODEL IntegratePS();
    }
}

technique integrate_bloom
{
    pass P0
    {
        AlphaBlendEnable = FALSE;
        VertexShader = compile VS_SHADERMODEL PostVS();
        PixelShader = compile PS_SHADERMODEL IntegrateBPS();
    }
}

technique integrate_motion_blur
{
    pass P0
    {
        AlphaBlendEnable = FALSE;
        VertexShader = compile VS_SHADERMODEL PostVS();
        PixelShader = compile PS_SHADERMODEL IntegrateMBPS();
    }
}