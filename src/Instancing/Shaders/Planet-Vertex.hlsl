cbuffer ProjView : register(b0)
{
    float4x4 View;
    float4x4 Proj;
}

struct VertexIn
{
    float3 Position : POSITION0;
    float3 Normal : NORMAL0;
    float2 TexCoord : TEXCOORD0;
};

struct FragmentIn
{
    float4 Position : SV_Position;
    float3 Position_WorldSpace : POSITION0;
    float3 Normal : NORMAL0;
    float2 TexCoord : TEXCOORD0;
};

FragmentIn VS(VertexIn input)
{
    FragmentIn output;
    float4 pos = float4(input.Position, 1);
    output.Position_WorldSpace = input.Position;
    output.Position = mul(Proj, mul(View, pos));
    output.Normal = input.Normal;
    output.TexCoord = input.TexCoord * float2(10, 6);
    return output;
}
