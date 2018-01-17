using ShaderGen;
using System.Numerics;

namespace AnimatedMesh
{
    public unsafe struct AnimatedVertex
    {
        [PositionSemantic]
        public Vector3 Position;
        [TextureCoordinateSemantic]
        public Vector2 UV;
        [TextureCoordinateSemantic]
        public Vector4 BoneWeights;
        [TextureCoordinateSemantic]
        public UInt4 BoneIndices;

        public void AddBone(uint id, float weight)
        {
            if (BoneWeights.X == 0)
            {
                BoneWeights.X = weight;
                BoneIndices.X = id;
            }
            else if (BoneWeights.Y == 0)
            {
                BoneWeights.Y = weight;
                BoneIndices.Y = id;
            }
            else if (BoneWeights.Z == 0)
            {
                BoneWeights.Z = weight;
                BoneIndices.Z = id;
            }
            else if (BoneWeights.W == 0)
            {
                BoneWeights.W = weight;
                BoneIndices.W = id;
            }
        }
    }
}
