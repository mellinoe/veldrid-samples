struct FragmentIn
{
    float4 Position : SV_Position;
    float3 Position_WorldSpace : POSITION0;
    float3 Normal : NORMAL0;
    float3 TexCoord : TEXCOORD0;
};

cbuffer LightInfo : register(b2)
{
    float3 LightDirection;
    float padding0;
    float3 CameraPosition;
    float padding1;
}

Texture2DArray Tex : register(t0);
SamplerState Samp : register(s0);

float4 FS(FragmentIn input) : SV_Target0
{
    float4 texColor = Tex.Sample(Samp, input.TexCoord);

    float diffuseIntensity = saturate(dot(input.Normal, -LightDirection));
    float4 diffuseColor = texColor * diffuseIntensity;

    // Specular color
    float4 specColor = float4(0, 0, 0, 0);
    float3 lightColor = float3(1, 1, 1);
    float specPower = 5.0f;
    float specIntensity = 0.3f;
    float3 vertexToEye = -normalize(input.Position_WorldSpace - CameraPosition);
    float3 lightReflect = normalize(reflect(LightDirection, input.Normal));
    float specularFactor = dot(vertexToEye, lightReflect);
    if (specularFactor > 0)
    {
        specularFactor = pow(abs(specularFactor), specPower);
        specColor = float4(lightColor * specIntensity * specularFactor, 1.0f) * texColor;
    }

    return diffuseColor + specColor;
}
