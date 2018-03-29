cbuffer ScreenSizeBuffer : register(b0)
{
    float Width;
    float Height;
    float2 Padding__;
}

cbuffer ShiftBuffer : register(b1)
{
    float RShift;
    float GShift;
    float BShift;
    float Padding1__;
}

RWTexture2D<float4> Tex : register(u0);

[numthreads(16, 16, 1)]
void CS(uint3 dtid : SV_DispatchThreadID)
{
    float x = (dtid.x + RShift);
    float y = (dtid.y + GShift);
    Tex[dtid.xy] = float4(x / Width, y / Height, BShift, 1);
}