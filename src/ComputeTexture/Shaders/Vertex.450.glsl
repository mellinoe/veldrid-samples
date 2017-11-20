#version 450
#extension GL_ARB_separate_shader_objects : enable
#extension GL_ARB_shading_language_420pack : enable

layout (location = 0) in vec2 Position;
layout (location = 1) in vec2 TexCoords;
layout (location = 0) out vec2 fsin_TexCoords;

void main()
{
    fsin_TexCoords = TexCoords;
    gl_Position = vec4(Position, 0, 1);
    gl_Position.y = -gl_Position.y; // Correct for Vulkan clip coordinates
}
