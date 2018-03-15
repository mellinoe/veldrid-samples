using ShaderGen;
using System.Numerics;

[assembly: ShaderSet("Animated", "AnimatedMesh.Shaders.Animated.VS", "AnimatedMesh.Shaders.Animated.FS")]

namespace AnimatedMesh.Shaders
{
    public partial class Animated
    {
        public struct FragmentInput
        {
            [SystemPositionSemantic]
            public Vector4 Position;
            [TextureCoordinateSemantic]
            public Vector2 UV;
        }

        public Matrix4x4 Projection;
        public Matrix4x4 View;
        public Matrix4x4 World;

        public BoneAnimInfo Bones;

        public Texture2DResource SurfaceTex;
        public SamplerResource SurfaceSampler;

        [VertexShader]
        public FragmentInput VS(AnimatedVertex input)
        {
            Matrix4x4 boneTransformation = Bones.BonesTransformations[input.BoneIndices.X] * input.BoneWeights.X;
            boneTransformation += Bones.BonesTransformations[input.BoneIndices.Y] * input.BoneWeights.Y;
            boneTransformation += Bones.BonesTransformations[input.BoneIndices.Z] * input.BoneWeights.Z;
            boneTransformation += Bones.BonesTransformations[input.BoneIndices.W] * input.BoneWeights.W;

            FragmentInput output;
            output.Position = Vector4.Transform(
                Vector4.Transform(
                    Vector4.Transform(
                        Vector4.Transform(
                            new Vector4(input.Position, 1),
                            boneTransformation),
                        World),
                    View),
                Projection);
            output.UV = input.UV;
            return output;
        }

        [FragmentShader]
        public Vector4 FS(FragmentInput input)
        {
            return ShaderBuiltins.Sample(SurfaceTex, SurfaceSampler, input.UV);
        }
    }
}
