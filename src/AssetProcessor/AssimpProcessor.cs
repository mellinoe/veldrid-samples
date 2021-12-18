using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using AssetPrimitives;
using Assimp;
using Veldrid;
using aiMatrix4x4 = Assimp.Matrix4x4;

namespace AssetProcessor
{
    public class AssimpProcessor : BinaryAssetProcessor<ProcessedModel>
    {
        private static readonly Vector3D ZeroVector = new Vector3D(0, 0, 0);

        private const int MaxVertexBoneWeightCount = 4;
        private const PostProcessSteps AlwaysIncludePostProcessSteps 
            = PostProcessSteps.LimitBoneWeights; // vertex data includes strictly max 4 bone weights/indices

        private ProcessedModelVertexElementSemantic[] ParseVertexElementSemantics(string arg)
            => arg.Split(",").Select(sem => Enum.Parse<ProcessedModelVertexElementSemantic>(sem)).ToArray();

        private static VertexElementFormat GetVertexElementFormatForSemantic(ProcessedModelVertexElementSemantic semantic)
        {
            switch (semantic)
            {
                case ProcessedModelVertexElementSemantic.Position:
                case ProcessedModelVertexElementSemantic.Normal:
                case ProcessedModelVertexElementSemantic.Tangent:
                case ProcessedModelVertexElementSemantic.BiTangent:
                case ProcessedModelVertexElementSemantic.Color:
                    return VertexElementFormat.Float3;
                case ProcessedModelVertexElementSemantic.TextureCoordinate:
                    return VertexElementFormat.Float2;
                case ProcessedModelVertexElementSemantic.BoneWeights:
                    return VertexElementFormat.Float4;
                case ProcessedModelVertexElementSemantic.BoneIndices:
                    return VertexElementFormat.UInt4;
                default:
                    throw new NotImplementedException($"{nameof(GetVertexElementFormatForSemantic)} not implemented for {nameof(semantic)} {semantic}");
            }
        }

        private static Vector3 ParseScale(string scale)
        {
            var parts = scale.Split(',', 3);
            switch (parts.Length)
            {
                case 1:
                    return new Vector3(float.Parse(parts[0]));
                case 3:
                    return new Vector3(float.Parse(parts[0]), float.Parse(parts[1]), float.Parse(parts[2]));
                default:
                    throw new NotImplementedException($"{nameof(ParseScale)} not implemented for {nameof(scale)} {scale}");
            }
        }

        private static Vector3 ParseCenter(string center)
        {
            var parts = center.Split(',', 3);
            if (parts.Length != 3)
                throw new ArgumentException($"{nameof(center)} must be in '<<x>>,<<y>>,<<z>>' format (e.g. 0,0,0 or 1,0,0 etc)");
            return new Vector3(float.Parse(parts[0]), float.Parse(parts[1]), float.Parse(parts[2]));
        }

        private static PostProcessSteps[] ParsePostProcessSteps(string arg)
            => arg.Split("|").Select(step => Enum.Parse<PostProcessSteps>(step)).ToArray();

        private static VertexElementDescription CreateVertexElementDescription(ProcessedModelVertexElementSemantic semantic, VertexElementFormat format)
            => new VertexElementDescription(semantic.ToString(), VertexElementSemantic.TextureCoordinate, format);

        private static NameValueCollection DefaultArgs
            = new NameValueCollection() {
                { nameof(ProcessedModelVertexElementSemantic), "Position,Normal,TextureCoordinate,BoneWeights,BoneIndices" },
                { "Scale", "1" },
                { "Center", "0,0,0" },
                { nameof(PostProcessSteps), "FlipWindingOrder|Triangulate|PreTransformVertices|CalculateTangentSpace|GenerateSmoothNormals" }
            };

        public unsafe override ProcessedModel ProcessT(Stream stream, string extension, NameValueCollection args = null)
        {
            args = args ?? DefaultArgs;
            var inputVertexSemantics = ParseVertexElementSemantics(args[nameof(ProcessedModelVertexElementSemantic)] ?? DefaultArgs[nameof(ProcessedModelVertexElementSemantic)]).ToArray();
            var postProcessSteps = ParsePostProcessSteps(args[nameof(PostProcessSteps)] ?? DefaultArgs[nameof(PostProcessSteps)]);
            var scale = ParseScale(args["Scale"] ?? DefaultArgs["Scale"]);
            var center = ParseCenter(args["Center"] ?? DefaultArgs["Center"]);

            AssimpContext ac = new AssimpContext();
            ac.SetConfig(new Assimp.Configs.VertexBoneWeightLimitConfig(MaxVertexBoneWeightCount));
            Scene scene = ac.ImportFileFromStream(
                stream,
                postProcessSteps.Aggregate(AlwaysIncludePostProcessSteps, (accum, step) => accum | step),
                extension);
            aiMatrix4x4 rootNodeInverseTransform = scene.RootNode.Transform;
            rootNodeInverseTransform.Inverse();

            List<ProcessedMeshPart> parts = new List<ProcessedMeshPart>();
            List<ProcessedAnimation> animations = new List<ProcessedAnimation>();

            HashSet<string> encounteredNames = new HashSet<string>();
            for (int meshIndex = 0; meshIndex < scene.MeshCount; meshIndex++)
            {
                Mesh mesh = scene.Meshes[meshIndex];
                string meshName = mesh.Name;
                if (string.IsNullOrEmpty(meshName))
                {
                    meshName = $"mesh_{meshIndex}";
                }
                int counter = 1;
                while (!encounteredNames.Add(meshName))
                {
                    meshName = mesh.Name + "_" + counter.ToString();
                    counter += 1;
                }
                int vertexCount = mesh.VertexCount;

                var vertexSemantics = inputVertexSemantics;
                if (!mesh.HasBones)
                    vertexSemantics = vertexSemantics.Where(semantic => semantic != ProcessedModelVertexElementSemantic.BoneIndices && semantic != ProcessedModelVertexElementSemantic.BoneWeights).ToArray();

                var vertexDataElementFormats =
                    vertexSemantics.Select(GetVertexElementFormatForSemantic).ToArray();
                var vertexSize = vertexDataElementFormats.Select(FormatHelpers.GetSizeInBytes).Sum(i => (int)i);

                var elementDescriptions = Enumerable.Zip(vertexSemantics, vertexDataElementFormats, CreateVertexElementDescription).ToArray();

                Dictionary<string, uint> boneIDsByName = new Dictionary<string, uint>();
                System.Numerics.Matrix4x4[] boneOffsets = new System.Numerics.Matrix4x4[mesh.BoneCount];

                byte[] vertexData = new byte[vertexCount * vertexSize];
                using (var memoryStream = new MemoryStream(vertexData))
                using (var vertexWriter = new BinaryWriter(memoryStream))
                {
                    var processBones = vertexSemantics.Any(semantic => semantic == ProcessedModelVertexElementSemantic.BoneIndices || semantic == ProcessedModelVertexElementSemantic.BoneWeights);
                    bool hasBones = mesh.HasBones;
                    var vertexWeightBones = new uint[vertexCount, 5];
                    var vertexWeights = new float[vertexCount, 4];

                    if (processBones && hasBones)
                    {
                        for (uint boneID = 0; boneID < mesh.BoneCount; boneID++)
                        {
                            Bone bone = mesh.Bones[(int)boneID];
                            string boneName = bone.Name;
                            int suffix = 1;
                            while (boneIDsByName.ContainsKey(boneName))
                            {
                                boneName = bone.Name + "_" + suffix.ToString();
                                suffix += 1;
                            }

                            boneIDsByName.Add(boneName, boneID);

                            foreach (var weight in bone.VertexWeights)
                            {
                                var vertexIndex = weight.VertexID;
                                var currentIndex = vertexWeightBones[vertexIndex, 4]++;
                                vertexWeightBones[vertexIndex, currentIndex] = boneID;
                                vertexWeights[vertexIndex, currentIndex] = weight.Weight;
                            }

                            System.Numerics.Matrix4x4 offsetMat = bone.OffsetMatrix.ToSystemMatrixTransposed();
                            System.Numerics.Matrix4x4.Decompose(offsetMat, out var offsetScale, out var rot, out var trans);
                            offsetMat = System.Numerics.Matrix4x4.CreateScale(offsetScale)
                                * System.Numerics.Matrix4x4.CreateFromQuaternion(rot)
                                * System.Numerics.Matrix4x4.CreateTranslation(trans);

                            boneOffsets[boneID] = offsetMat;
                        }
                    }

                    for (int i = 0; i < vertexCount; i++)
                    {
                        foreach (var semantic in vertexSemantics)
                        {
                            switch (semantic)
                            {
                                case ProcessedModelVertexElementSemantic.Position:
                                    var position = mesh.Vertices[i];
                                    vertexWriter.Write(position.X * scale.X + center.X);
                                    vertexWriter.Write(position.Y * scale.Y + center.Y);
                                    vertexWriter.Write(position.Z * scale.Z + center.Z);
                                    break;
                                case ProcessedModelVertexElementSemantic.Normal:
                                    var normal = mesh.Normals[i];
                                    vertexWriter.Write(normal.X);
                                    vertexWriter.Write(normal.Y);
                                    vertexWriter.Write(normal.Z);
                                    break;
                                case ProcessedModelVertexElementSemantic.Tangent:
                                    var tangent = mesh.Tangents.Count > i ? mesh.Tangents[i] : ZeroVector;
                                    vertexWriter.Write(tangent.X);
                                    vertexWriter.Write(tangent.Y);
                                    vertexWriter.Write(tangent.Z);
                                    break;
                                case ProcessedModelVertexElementSemantic.BiTangent:
                                    var bitangent = mesh.BiTangents.Count > i ? mesh.BiTangents[i] : ZeroVector;
                                    vertexWriter.Write(bitangent.X);
                                    vertexWriter.Write(bitangent.Y);
                                    vertexWriter.Write(bitangent.Z);
                                    break;
                                case ProcessedModelVertexElementSemantic.Color:
                                    var hasColors = mesh.HasVertexColors(0);
                                    if (hasColors)
                                    {
                                        var color = mesh.VertexColorChannels[0][i];
                                        vertexWriter.Write(color.R);
                                        vertexWriter.Write(color.G);
                                        vertexWriter.Write(color.B);
                                    }
                                    else if (mesh.MaterialIndex >= 0)
                                    {
                                        var meshMaterial = scene.Materials[mesh.MaterialIndex];
                                        var diffuseColor = meshMaterial.ColorDiffuse;
                                        vertexWriter.Write(diffuseColor.R);
                                        vertexWriter.Write(diffuseColor.G);
                                        vertexWriter.Write(diffuseColor.B);
                                    }
                                    else
                                    {
                                        throw new Exception($"Could not find vertex color via vertex color channels or mesh material for vert at index {i}");
                                    }
                                    break;
                                case ProcessedModelVertexElementSemantic.TextureCoordinate:
                                    var hasTextureCoordinates = mesh.HasTextureCoords(0);
                                    if (hasTextureCoordinates)
                                    {
                                        var textureCoordinate = mesh.TextureCoordinateChannels[0][i];
                                        vertexWriter.Write(textureCoordinate.X);
                                        vertexWriter.Write(textureCoordinate.Y);
                                    }
                                    else
                                    {
                                        vertexWriter.Write(0.0f);
                                        vertexWriter.Write(0.0f);
                                    }
                                    break;
                                case ProcessedModelVertexElementSemantic.BoneWeights:
                                    vertexWriter.Write(vertexWeights[i, 0]);
                                    vertexWriter.Write(vertexWeights[i, 1]);
                                    vertexWriter.Write(vertexWeights[i, 2]);
                                    vertexWriter.Write(vertexWeights[i, 3]);
                                    break;
                                case ProcessedModelVertexElementSemantic.BoneIndices:
                                    vertexWriter.Write(vertexWeightBones[i, 0]);
                                    vertexWriter.Write(vertexWeightBones[i, 1]);
                                    vertexWriter.Write(vertexWeightBones[i, 2]);
                                    vertexWriter.Write(vertexWeightBones[i, 3]);
                                    break;
                                default:
                                    throw new NotImplementedException();
                            }
                        }

                    }
                }

                List<int> indices = new List<int>();
                foreach (Face face in mesh.Faces)
                {
                    if (face.IndexCount == 3)
                    {
                        indices.Add(face.Indices[0]);
                        indices.Add(face.Indices[1]);
                        indices.Add(face.Indices[2]);
                    }
                }

                uint indexCount = (uint)indices.Count;

                int[] int32Indices = indices.ToArray();
                byte[] indexData = new byte[indices.Count * sizeof(uint)];
                fixed (byte* indexDataPtr = indexData)
                {
                    fixed (int* int32Ptr = int32Indices)
                    {
                        Buffer.MemoryCopy(int32Ptr, indexDataPtr, indexData.Length, indexData.Length);
                    }
                }

                ProcessedMeshPart part = new ProcessedMeshPart(
                    vertexData,
                    elementDescriptions,
                    indexData,
                    IndexFormat.UInt32,
                    (uint)indices.Count,
                    boneIDsByName,
                    boneOffsets);
                parts.Add(part);
            }

            // Nodes
            Node rootNode = scene.RootNode;
            List<ProcessedNode> processedNodes = new List<ProcessedNode>();
            ConvertNode(rootNode, -1, processedNodes);

            ProcessedNodeSet nodes = new ProcessedNodeSet(processedNodes.ToArray(), 0, rootNodeInverseTransform.ToSystemMatrixTransposed());

            for (int animIndex = 0; animIndex < scene.AnimationCount; animIndex++)
            {
                Animation animation = scene.Animations[animIndex];
                Dictionary<string, ProcessedAnimationChannel> channels = new Dictionary<string, ProcessedAnimationChannel>();
                for (int channelIndex = 0; channelIndex < animation.NodeAnimationChannelCount; channelIndex++)
                {
                    NodeAnimationChannel nac = animation.NodeAnimationChannels[channelIndex];
                    channels[nac.NodeName] = ConvertChannel(nac);
                }

                string baseAnimName = animation.Name;
                if (string.IsNullOrEmpty(baseAnimName))
                {
                    baseAnimName = "anim_" + animIndex;
                }

                string animationName = baseAnimName;


                int counter = 1;
                while (!encounteredNames.Add(animationName))
                {
                    animationName = baseAnimName + "_" + counter.ToString();
                    counter += 1;
                }
                animations.Add(new ProcessedAnimation(animationName, animation.DurationInTicks, animation.TicksPerSecond, channels));
            }

            return new ProcessedModel(parts.ToArray(), nodes, animations.ToArray());
        }

        private int GetAndIncrementRelativeBoneIndex(Dictionary<int, int> assignedBoneWeights, int vertexID)
        {
            int currentCount = 0;
            assignedBoneWeights.TryGetValue(vertexID, out currentCount);
            assignedBoneWeights[vertexID] = currentCount + 1;
            return currentCount;
        }

        private ProcessedAnimationChannel ConvertChannel(NodeAnimationChannel nac)
        {
            string nodeName = nac.NodeName;
            AssetPrimitives.VectorKey[] positions = new AssetPrimitives.VectorKey[nac.PositionKeyCount];
            for (int i = 0; i < nac.PositionKeyCount; i++)
            {
                Assimp.VectorKey assimpKey = nac.PositionKeys[i];
                positions[i] = new AssetPrimitives.VectorKey(assimpKey.Time, assimpKey.Value.ToSystemVector3());
            }

            AssetPrimitives.VectorKey[] scales = new AssetPrimitives.VectorKey[nac.ScalingKeyCount];
            for (int i = 0; i < nac.ScalingKeyCount; i++)
            {
                Assimp.VectorKey assimpKey = nac.ScalingKeys[i];
                scales[i] = new AssetPrimitives.VectorKey(assimpKey.Time, assimpKey.Value.ToSystemVector3());
            }

            AssetPrimitives.QuaternionKey[] rotations = new AssetPrimitives.QuaternionKey[nac.RotationKeyCount];
            for (int i = 0; i < nac.RotationKeyCount; i++)
            {
                Assimp.QuaternionKey assimpKey = nac.RotationKeys[i];
                rotations[i] = new AssetPrimitives.QuaternionKey(assimpKey.Time, assimpKey.Value.ToSystemQuaternion());
            }

            return new ProcessedAnimationChannel(nodeName, positions, scales, rotations);
        }

        private int ConvertNode(Node node, int parentIndex, List<ProcessedNode> processedNodes)
        {
            int currentIndex = processedNodes.Count;
            int[] childIndices = new int[node.ChildCount];
            var nodeTransform = node.Transform.ToSystemMatrixTransposed();
            ProcessedNode pn = new ProcessedNode(node.Name, nodeTransform, parentIndex, childIndices);
            processedNodes.Add(pn);

            for (int i = 0; i < childIndices.Length; i++)
            {
                int childIndex = ConvertNode(node.Children[i], currentIndex, processedNodes);
                childIndices[i] = childIndex;
            }

            return currentIndex;
        }

        private unsafe struct VertexDataBuilder
        {
            private readonly GCHandle _gch;
            private readonly unsafe byte* _dataPtr;
            private readonly int _vertexSize;

            public VertexDataBuilder(byte[] data, int vertexSize)
            {
                _gch = GCHandle.Alloc(data, GCHandleType.Pinned);
                _dataPtr = (byte*)_gch.AddrOfPinnedObject();
                _vertexSize = vertexSize;
            }

            public void WriteVertexElement<T>(int vertex, int elementOffset, ref T data)
            {
                byte* dst = _dataPtr + (_vertexSize * vertex) + elementOffset;
                Unsafe.Copy(dst, ref data);
            }

            public void WriteVertexElement<T>(int vertex, int elementOffset, T data)
            {
                byte* dst = _dataPtr + (_vertexSize * vertex) + elementOffset;
                Unsafe.Copy(dst, ref data);
            }

            public void FreeGCHandle()
            {
                _gch.Free();
            }
        }
    }

    public static class AssimpExtensions
    {
        public static unsafe System.Numerics.Matrix4x4 ToSystemMatrixTransposed(this aiMatrix4x4 mat)
        {
            return System.Numerics.Matrix4x4.Transpose(Unsafe.Read<System.Numerics.Matrix4x4>(&mat));
        }

        public static System.Numerics.Quaternion ToSystemQuaternion(this Assimp.Quaternion quat)
        {
            return new System.Numerics.Quaternion(quat.X, quat.Y, quat.Z, quat.W);
        }

        public static Vector3 ToSystemVector3(this Assimp.Vector3D v3)
        {
            return new Vector3(v3.X, v3.Y, v3.Z);
        }
    }
}
