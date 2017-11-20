struct VertexInput
{
    float2 Position : Position0;
    float2 TexCoords : TEXCOORD0;
};

struct PixelInput
{
    float4 Position : SV_Position;
    float2 TexCoords : TEXCOORD0;
};

PixelInput VS(VertexInput input)
{
    PixelInput output;
    output.Position = float4(input.Position, 0, 1);
    output.TexCoords = input.TexCoords;
    return output;
}