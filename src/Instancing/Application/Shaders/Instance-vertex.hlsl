cbuffer ProjView : register(b0)
{
    float4x4 View;
    float4x4 Proj;
}

cbuffer RotationInfo : register(b1)
{
    float LocalRotation;
    float GlobalRotation;
    float2 padding0;
}

struct VertexIn
{
    float3 Position : POSITION0;
    float3 Normal : NORMAL0;
    float2 TexCoord : TEXCOORD0;
    float3 InstancePosition : POSITION1;
    float3 InstanceRotation : TEXCOORD1;
    float3 InstanceScale : TEXCOORD2;
    int InstanceTexArrayIndex : TEXCOORD3;
};

struct FragmentIn
{
    float4 Position : SV_Position;
    float3 Position_WorldSpace : POSITION0;
    float3 Normal : NORMAL0;
    float3 TexCoord : TEXCOORD0;
};

FragmentIn VS(VertexIn input)
{
    float cosX = cos(input.InstanceRotation.x);
    float sinX = sin(input.InstanceRotation.x);
    float3x3 instanceRotX =
    {
        1, 0, 0,
        0, cosX, -sinX,
        0, sinX, cosX
    };

    float cosY = cos(input.InstanceRotation.y + LocalRotation);
    float sinY = sin(input.InstanceRotation.y + LocalRotation);
    float3x3 instanceRotY =
    {
        cosY, 0, sinY,
        0, 1, 0,
        -sinY, 0, cosY
    };

    float cosZ = cos(input.InstanceRotation.z);
    float sinZ = sin(input.InstanceRotation.z);
    float3x3 instanceRotZ =
    {
        cosZ, -sinZ, 0,
        sinZ, cosZ, 0,
        0, 0, 1
    };

    float3x3 instanceRotFull = mul(instanceRotZ, mul(instanceRotY, instanceRotZ));
    float3x3 scalingMat = { input.InstanceScale.x, 0, 0, 0, input.InstanceScale.y, 0, 0, 0, input.InstanceScale.z };

    float globalCos = cos(GlobalRotation);
    float globalSin = sin(GlobalRotation);

    float3x3 globalRotMat =
    {
        globalCos, 0, globalSin,
        0, 1, 0,
        -globalSin, 0, globalCos
    };

    FragmentIn output;
    float3 transformedPos = (mul(scalingMat, mul(instanceRotFull, input.Position)) + input.InstancePosition);
    transformedPos = mul(globalRotMat, transformedPos);
    float4 pos = float4(transformedPos, 1);
    output.Position_WorldSpace = transformedPos;
    output.Position = mul(Proj, mul(View, pos));
    output.Normal = normalize(mul(globalRotMat, mul(instanceRotFull, input.Normal)));
    output.TexCoord = float3(input.TexCoord, input.InstanceTexArrayIndex);
    return output;
}
