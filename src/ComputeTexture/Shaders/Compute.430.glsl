#version 430

uniform ScreenSizeBuffer
{
    float ScreenWidth;
    float ScreenHeight;
    vec2 Padding_;
};

layout(rgba32f) uniform image2D Tex;

layout(local_size_x = 1, local_size_y = 1, local_size_z = 1) in;

void main()
{
    imageStore(Tex, ivec2(gl_GlobalInvocationID.xy), vec4(gl_GlobalInvocationID.x / ScreenWidth, gl_GlobalInvocationID.y / ScreenHeight, 0, 1));
}
