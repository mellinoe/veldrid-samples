cbuffer ScreenSizeBuffer : register(b0)
{
    float Width;
    float Height;
    float2 Padding__;
}

RWTexture2D<float4> Tex : register(u0);

[numthreads(1, 1, 1)]
void CS(uint3 dtid : SV_DispatchThreadID)
{
    Tex[dtid.xy] = float4(dtid.x / Width, dtid.y / Height, 0, 1);
}