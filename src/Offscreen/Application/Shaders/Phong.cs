using ShaderGen;
using System;
using System.Numerics;
using static ShaderGen.ShaderBuiltins;

[assembly: ShaderSet("Phong", "Offscreen.Shaders.Phong.VS", "Offscreen.Shaders.Phong.FS")]

namespace Offscreen.Shaders
{
    public class Phong
    {
        [ResourceSet(0)]
        public OffscreenApplication.UniformInfo UBO;

        public struct VSIn
        {
            [PositionSemantic] public Vector3 Position;
            [TextureCoordinateSemantic] public Vector2 UV;
            [ColorSemantic] public Vector3 Color;
            [NormalSemantic] public Vector3 Normal;
        }

        public struct FSIn
        {
            [SystemPositionSemantic] public Vector4 Position;
            [NormalSemantic] public Vector3 Normal;
            [ColorSemantic] public Vector3 Color;
            [TextureCoordinateSemantic] public Vector3 EyePos;
            [TextureCoordinateSemantic] public Vector3 LightVec;
        }

        [VertexShader]
        public FSIn VS(VSIn input)
        {
            Vector4 v4Pos = new Vector4(input.Position, 1);
            FSIn output;
            output.Normal = Vector4.Normalize(Mul(UBO.Model, new Vector4(input.Normal, 1))).XYZ();
            output.Normal = input.Normal;
            output.Color = input.Color;
            output.Position = Mul(UBO.Projection, Mul(UBO.View, (Mul(UBO.Model, v4Pos))));
            Vector4 eyePos = Mul(UBO.View, Mul(UBO.Model, v4Pos));
            output.EyePos = eyePos.XYZ();
            Vector4 eyeLightPos = Mul(UBO.View, UBO.LightPos);
            output.LightVec = Vector3.Normalize(UBO.LightPos.XYZ() - output.EyePos);
            return output;
        }

        [FragmentShader]
        public Vector4 FS(FSIn input)
        {
            Vector3 Eye = Vector3.Normalize(-input.EyePos);
            Vector3 Reflected = Vector3.Normalize(Vector3.Reflect(-input.LightVec, input.Normal));

            Vector4 IAmbient = new Vector4(0.1f, 0.1f, 0.1f, 1.0f);
            Vector4 IDiffuse = new Vector4(MathF.Max(Vector3.Dot(input.Normal, input.LightVec), 0f));
            float specular = 0.75f;
            Vector4 ISpecular = new Vector4(0.0f);
            if (Vector3.Dot(input.EyePos, input.Normal) < 0.0)
            {
                ISpecular = new Vector4(0.5f, 0.5f, 0.5f, 1.0f) * MathF.Pow(MathF.Max(Vector3.Dot(Reflected, Eye), 0.0f), 16.0f) * specular;
            }

            return (IAmbient + IDiffuse) * new Vector4(input.Color, 1.0f) + ISpecular;
        }
    }
}
