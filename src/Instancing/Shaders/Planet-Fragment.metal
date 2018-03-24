#include <metal_stdlib>
using namespace metal;

struct FragmentIn
{
    float4 Position[[position]];
    float3 Position_WorldSpace[[attribute(0)]];
    float3 Normal[[attribute(1)]];
    float2 TexCoord[[attribute(2)]];
};

struct LightInfo
{
    float3 LightDirection;
    float padding0;
    float3 CameraPosition;
    float padding1;
};

fragment float4 FS(
    FragmentIn input[[stage_in]],
    constant LightInfo& lightInfo[[buffer(2)]],
    texture2d<float> tex[[texture(0)]],
    sampler samp[[sampler(0)]])
{
    float4 texColor = tex.sample(samp, input.TexCoord);

    float diffuseIntensity = saturate(dot(input.Normal, -lightInfo.LightDirection));
    float4 diffuseColor = texColor * diffuseIntensity;

    // Specular color
    float4 specColor = float4(0, 0, 0, 0);
    float3 lightColor = float3(1, 1, 1);
    float specPower = 5.0f;
    float specIntensity = 0.3f;
    float3 vertexToEye = -normalize(input.Position_WorldSpace - lightInfo.CameraPosition);
    float3 lightReflect = normalize(reflect(lightInfo.LightDirection, input.Normal));
    float specularFactor = dot(vertexToEye, lightReflect);
    if (specularFactor > 0)
    {
        specularFactor = pow(abs(specularFactor), specPower);
        specColor = float4(lightColor * specIntensity * specularFactor, 1.0f) * texColor;
    }

    return diffuseColor + specColor;
}
