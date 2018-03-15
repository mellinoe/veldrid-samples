#version 330 core

out vec4 fsin_ClipPos;
out vec3 fsin_TexCoord;

void main()
{
    fsin_TexCoord = vec3((gl_VertexID << 1) & 2, gl_VertexID & 2, gl_VertexID & 2);
    gl_Position = vec4(fsin_TexCoord.xy * 2.0f - 1.0f, 0.0f, 1.0f);
    gl_Position.z = gl_Position.z * 2.0 - gl_Position.w;
    fsin_ClipPos = gl_Position;
}
