using ShaderGen;
using System.Numerics;

[assembly: ShaderSet("TintShader", "ImageTint.Shaders.TintShader.VS", "ImageTint.Shaders.TintShader.FS")]

namespace ImageTint.Shaders
{
    public class TintShader
    {
        public Texture2DResource Input;
        public SamplerResource Sampler;
        public TintInfo Tint;

        [VertexShader]
        public FragmentInput VS(VertexInput input)
        {
            FragmentInput output;
            output.Position = new Vector4(input.Position, 0, 1);
            output.TextureCoordinates = input.TextureCoordinates;
            return output;
        }

        [FragmentShader]
        public Vector4 FS(FragmentInput input)
        {
            Vector2 texCoords = input.TextureCoordinates;
            Vector4 inputColor = ShaderBuiltins.Sample(Input, Sampler, texCoords);
            Vector4 tintedColor = new Vector4(inputColor.XYZ() * Tint.RGBTintColor, inputColor.Z);
            return tintedColor;
        }

        public struct VertexInput
        {
            [PositionSemantic]
            public Vector2 Position;
            [TextureCoordinateSemantic]
            public Vector2 TextureCoordinates;
        }

        public struct FragmentInput
        {
            [SystemPositionSemantic]
            public Vector4 Position;
            [TextureCoordinateSemantic]
            public Vector2 TextureCoordinates;
        }
    }
}
