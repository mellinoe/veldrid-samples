#include <metal_stdlib>
using namespace metal;

struct ProjView
{
    float4x4 View;
    float4x4 Proj;
};

struct RotationInfo
{
    float LocalRotation;
    float GlobalRotation;
    float2 padding0;
};

struct VertexIn
{
    float3 Position[[attribute(0)]];
    float3 Normal[[attribute(1)]];;
    float2 TexCoord[[attribute(2)]];;
    float3 InstancePosition[[attribute(3)]];;
    float3 InstanceRotation[[attribute(4)]];;
    float3 InstanceScale[[attribute(5)]];;
    int InstanceTexArrayIndex[[attribute(6)]];;
};

struct FragmentIn
{
    float4 Position[[position]];
    float3 Position_WorldSpace[[attribute(0)]];
    float3 Normal[[attribute(1)]];
    float3 TexCoord[[attribute(2)]];
};

vertex FragmentIn VS(
    VertexIn input[[stage_in]],
    constant ProjView& projView[[buffer(2)]],
    constant RotationInfo& rotInfo[[buffer(3)]])
{
    float cosX = cos(input.InstanceRotation.x);
    float sinX = sin(input.InstanceRotation.x);
    float3x3 instanceRotX =
    {
        1, 0, 0,
        0, cosX, -sinX,
        0, sinX, cosX
    };

    float cosY = cos(input.InstanceRotation.y + rotInfo.LocalRotation);
    float sinY = sin(input.InstanceRotation.y + rotInfo.LocalRotation);
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

    float3x3 instanceRotFull = instanceRotZ * instanceRotY * instanceRotZ;
    float3x3 scalingMat = { input.InstanceScale.x, 0, 0, 0, input.InstanceScale.y, 0, 0, 0, input.InstanceScale.z };

    float globalCos = cos(-rotInfo.GlobalRotation);
    float globalSin = sin(-rotInfo.GlobalRotation);

    float3x3 globalRotMat =
    {
        globalCos, 0, globalSin,
        0, 1, 0,
        -globalSin, 0, globalCos
    };

    FragmentIn output;
    float3 transformedPos = (scalingMat * instanceRotFull * input.Position) + input.InstancePosition;
    transformedPos = globalRotMat * transformedPos;
    float4 pos = float4(transformedPos, 1);
    output.Position_WorldSpace = transformedPos;
    output.Position = projView.Proj * projView.View * pos;
    output.Normal = normalize(globalRotMat * instanceRotFull * input.Normal);
    output.TexCoord = float3(input.TexCoord, input.InstanceTexArrayIndex);
    return output;
}
