cbuffer ProjView : register(b0)
{
    float4x4 View;
    float4x4 Proj;
}

struct VertexIn
{
    float2 Position : POSITION0;
    float4 Color : COLOR0;
    float xOff : POSITION1;
};

struct FragmentIn
{
    float4 Position : SV_Position;
    float4 Color : COLOR0;
};

FragmentIn VS(VertexIn input)
{
    FragmentIn output;
    float4 offsetPos = float4(input.Position.x + input.xOff, input.Position.y, 0, 1);
    output.Position = mul(Proj, mul(View, offsetPos));
    output.Color = input.Color;
    return output;
}
