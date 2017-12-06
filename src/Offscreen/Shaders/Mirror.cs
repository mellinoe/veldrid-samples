using ShaderGen;
using System;
using System.Numerics;
using static ShaderGen.ShaderBuiltins;

[assembly: ShaderSet("Mirror", "Offscreen.Shaders.Mirror.VS", "Offscreen.Shaders.Mirror.FS")]

namespace Offscreen.Shaders
{
    public class Mirror
    {
        public OffscreenApplication.UniformInfo UBO;
        public Texture2DResource ReflectionMap;
        public SamplerResource ReflectionMapSampler;
        public Texture2DResource ColorMap;
        public SamplerResource ColorMapSampler;

        public struct VSIn
        {
            [PositionSemantic] public Vector3 Position;
            [TextureCoordinateSemantic] public Vector2 UV;
            [ColorSemantic] public Vector3 Color;
            [NormalSemantic] public Vector3 Normal;
        }

        public struct FSIn
        {
            [SystemPositionSemantic] public Vector4 SysPosition;
            [TextureCoordinateSemantic] public Vector2 UV;
            [PositionSemantic] public Vector4 Position;
        }

        [VertexShader]
        public FSIn VS(VSIn input)
        {
            FSIn output;
            output.UV = input.UV;
            output.Position = Mul(UBO.Projection, Mul(UBO.Model, new Vector4(input.Position, 1f)));
            output.SysPosition = output.Position;
            return output;
        }

        [FragmentShader]
        public Vector4 FS(FSIn input)
        {
            Vector4 tmp = new Vector4(1.0f / input.Position.W);
            Vector4 projCoord = input.Position * tmp;
            Vector4 outFragColor;

            // Scale and bias
            projCoord += new Vector4(1.0f);
            projCoord *= new Vector4(0.5f);

            // Slow single pass blur
            // For demonstration purposes only
            const float blurSize = 1.0f / 512.0f;

            Vector4 color = Sample(ColorMap, ColorMapSampler, input.UV);
            outFragColor = color * 0.25f;

            // if (gl_FrontFacing)
            {
                // Only render mirrored scene on front facing (upper) side of mirror surface
                Vector4 reflection = new Vector4(0.0f);
                for (int x = -3; x <= 3; x++)
                {
                    for (int y = -3; y <= 3; y++)
                    {
                        reflection += Sample(
                            ReflectionMap,
                            ReflectionMapSampler,
                            new Vector2(projCoord.X + x * blurSize, projCoord.Y + y * blurSize)) / 49.0f;
                    }
                }
                outFragColor += reflection * 1.5f * (color.X);
            }

            return outFragColor;
        }
    }
}
