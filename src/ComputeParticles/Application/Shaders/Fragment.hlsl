struct PixelInput
{
    float4 Position : SV_Position;
    float4 Color : Color0;
};

float4 FS(PixelInput input) : SV_Target0
{
    return input.Color;
}