#version 430

in vec2 Position;
in vec2 TexCoords;
out vec2 fsin_TexCoords;

void main()
{
    fsin_TexCoords = TexCoords;
    gl_Position = vec4(Position, 0, 1);
}
