using ShaderGen;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace AnimatedMesh
{
    public struct BonesInfo
    {
        public const int MaxBones = 64;

        [ArraySize(MaxBones)]
        public Matrix4x4[] BoneTransformations;

        public unsafe struct Blittable
        {
            public fixed float Data[64 * MaxBones];
        }

        public unsafe Blittable GetBlittable()
        {
            Blittable b;
            fixed (Matrix4x4* mPtr = BoneTransformations)
            {
                Unsafe.CopyBlock(&b, mPtr, (uint)(sizeof(Matrix4x4) * MaxBones));
            }

            return b;
        }
    }
}
