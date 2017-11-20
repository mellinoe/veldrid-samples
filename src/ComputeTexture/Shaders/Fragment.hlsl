Texture2D Tex : register(t0);
Texture2D Tex11 : register(t1);
Texture2D Tex22 : register(t2);
SamplerState SS : register(s0);

struct PixelInput
{
    float4 Position : SV_Position;
    float2 TexCoords : TEXCOORD0;
};

float4 FS(PixelInput input) : SV_Target0
{
    return Tex.Sample(SS, input.TexCoords) + Tex11.Sample(SS, input.TexCoords) * 0.01 + Tex22.Sample(SS, input.TexCoords) * 0.01;

}
