struct FragmentIn
{
    float4 Position : SV_Position;
    float4 ClipPos : POSITION0;
    float3 TexCoord : TEXCOORD0;
};

FragmentIn VS(uint vertexID : SV_VertexID)
{
    FragmentIn output;
    output.TexCoord = float3((vertexID << 1) & 2, vertexID & 2, vertexID & 2);
    output.Position = float4(output.TexCoord.xy * 2.0f - 1.0f, 0.0f, 1.0f);
    output.ClipPos = output.Position;

    return output;
}
