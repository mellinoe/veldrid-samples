using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using Veldrid;

namespace AssetPrimitives
{
    public enum ProcessedModelVertexElementSemantic
    {
        Position,
        Normal,
        TextureCoordinate,
        Color,
        Tangent,
        BiTangent,
        BoneWeights,
        BoneIndices
    }

    public class ProcessedModel
    {
        public ProcessedMeshPart[] MeshParts { get; }
        public ProcessedNodeSet Nodes { get; }
        public ProcessedAnimation[] Animations { get; }

        public ProcessedModel(ProcessedMeshPart[] meshParts, ProcessedNodeSet nodes, ProcessedAnimation[] animations)
        {
            MeshParts = meshParts;
            Nodes = nodes;
            Animations = animations;
        }

        public void MergeMeshesToSingleVertexAndIndexBuffer(out byte[] vertexData, out VertexElementDescription[] vertexElementDescriptions, out byte[] indexData, out uint indexCount, out IndexFormat indexFormat)
        {
            var dataLengths = MeshParts.Aggregate(
                (vertexDataLength: 0u, indexDataLength: 0u, indexCount: 0u),
                (accum, mesh) => (
                    vertexDataLength: accum.vertexDataLength + (uint)mesh.VertexData.Length,
                    indexDataLength: accum.indexDataLength + (uint)mesh.IndexData.Length,
                    indexCount: accum.indexCount + mesh.IndexCount
                )
            );

            vertexData = new byte[dataLengths.vertexDataLength];
            indexData = new byte[dataLengths.indexDataLength];
            indexCount = dataLengths.indexCount;

            vertexElementDescriptions = MeshParts[0].VertexElements;
            indexFormat = MeshParts[0].IndexFormat;

            var vertexSize = (uint)vertexElementDescriptions.Sum(element => FormatSizeHelpers.GetSizeInBytes(element.Format));

            using (var vertexOutStream = new MemoryStream(vertexData))
            using (var indexOutStream = new MemoryStream(indexData))
            using (var vertexWriter = new BinaryWriter(vertexOutStream))
            using (var indexWriter = new BinaryWriter(indexOutStream))
            {
                uint startIndexOffset = 0;
                for (int i = 0; i < MeshParts.Length; i++)
                {
                    var mesh = MeshParts[i];
                    Debug.Assert(Enumerable.SequenceEqual(mesh.VertexElements, vertexElementDescriptions), "Check mesh vertex data compatibility.  There seems to be meshes with incompatible vertex element lists.");
                    Debug.Assert(mesh.IndexFormat.Equals(indexFormat), "Check mesh index data compatibility.  There seems to be meshes with incompatible index formats.");

                    vertexOutStream.Write(mesh.VertexData, 0, mesh.VertexData.Length);

                    using (var indexInStream = new MemoryStream(mesh.IndexData))
                    using (var indexReader = new BinaryReader(indexInStream))
                    {
                        for (int index = 0; index < mesh.IndexCount; index++)
                        {
                            switch (indexFormat)
                            {
                                case IndexFormat.UInt16:
                                    indexWriter.Write(indexReader.ReadUInt16() + startIndexOffset);
                                    break;
                                case IndexFormat.UInt32:
                                    indexWriter.Write(indexReader.ReadUInt32() + startIndexOffset);
                                    break;
                                default:
                                    throw new NotImplementedException($"{nameof(MergeMeshesToSingleVertexAndIndexBuffer)} not implemented for {nameof(indexFormat)} {indexFormat}");
                            }
                        }
                    }

                    startIndexOffset += (uint)mesh.VertexData.Length / vertexSize;
                }
            }
        }
    }

    public class ProcessedMeshPart
    {
        public byte[] VertexData { get; set; }
        public VertexElementDescription[] VertexElements { get; set; }
        public byte[] IndexData { get; set; }
        public IndexFormat IndexFormat { get; set; }
        public uint IndexCount { get; set; }
        public IDictionary<string, uint> BoneIDsByName { get; set; }
        public Matrix4x4[] BoneOffsets { get; set; }

        public ProcessedMeshPart(
            byte[] vertexData,
            VertexElementDescription[] vertexElements,
            byte[] indexData,
            IndexFormat indexFormat,
            uint indexCount,
            IDictionary<string, uint> boneIDsByName,
            Matrix4x4[] boneOffsets)
        {
            VertexData = vertexData;
            VertexElements = vertexElements;
            IndexData = indexData;
            IndexFormat = indexFormat;
            IndexCount = indexCount;
            BoneIDsByName = boneIDsByName;
            BoneOffsets = boneOffsets;
        }

        public ModelResources CreateDeviceResources(
            GraphicsDevice gd,
            ResourceFactory factory)
        {
            DeviceBuffer vertexBuffer = factory.CreateBuffer(new BufferDescription(
                (uint)VertexData.Length, BufferUsage.VertexBuffer));
            gd.UpdateBuffer(vertexBuffer, 0, VertexData);

            DeviceBuffer indexBuffer = factory.CreateBuffer(new BufferDescription(
                (uint)IndexData.Length, BufferUsage.IndexBuffer));
            gd.UpdateBuffer(indexBuffer, 0, IndexData);

            return new ModelResources(vertexBuffer, indexBuffer, IndexFormat, IndexCount);
        }
    }

    [Serializable]
    public class ProcessedAnimation
    {
        public ProcessedAnimation(
            string name,
            double durationInTicks,
            double ticksPerSecond,
            Dictionary<string, ProcessedAnimationChannel> animationChannels)
        {
            Name = name;
            DurationInTicks = durationInTicks;
            TicksPerSecond = ticksPerSecond;
            AnimationChannels = animationChannels;
        }

        public string Name { get; set; }
        public double DurationInTicks { get; set; }
        public double TicksPerSecond { get; set; }
        public Dictionary<string, ProcessedAnimationChannel> AnimationChannels { get; set; }

        public double DurationInSeconds => DurationInTicks * TicksPerSecond;
    }

    public class ProcessedAnimationChannel
    {
        public ProcessedAnimationChannel(string nodeName, VectorKey[] positions, VectorKey[] scales, QuaternionKey[] rotations)
        {
            NodeName = nodeName;
            Positions = positions;
            Scales = scales;
            Rotations = rotations;
        }

        public string NodeName { get; set; }
        public VectorKey[] Positions { get; set; }
        public VectorKey[] Scales { get; set; }
        public QuaternionKey[] Rotations { get; set; }
    }

    public struct VectorKey
    {
        public readonly double Time;
        public readonly Vector3 Value;

        public VectorKey(double time, Vector3 value)
        {
            Time = time;
            Value = value;
        }
    }

    public struct QuaternionKey
    {
        public readonly double Time;
        public readonly Quaternion Value;

        public QuaternionKey(double time, Quaternion value)
        {
            Time = time;
            Value = value;
        }
    }

    public class ProcessedNodeSet
    {
        public ProcessedNodeSet(ProcessedNode[] nodes, int rootNodeIndex, Matrix4x4 rootNodeInverseTransform)
        {
            Nodes = nodes;
            RootNodeIndex = rootNodeIndex;
            RootNodeInverseTransform = rootNodeInverseTransform;
        }

        public ProcessedNode[] Nodes { get; set; }
        public int RootNodeIndex { get; set; }
        public Matrix4x4 RootNodeInverseTransform { get; set; }
    }

    public class ProcessedNode
    {
        public ProcessedNode(string name, Matrix4x4 transform, int parentIndex, int[] childIndices)
        {
            Name = name;
            Transform = transform;
            ParentIndex = parentIndex;
            ChildIndices = childIndices;
        }

        public string Name { get; set; }
        public Matrix4x4 Transform { get; set; }
        public int ParentIndex { get; set; }
        public int[] ChildIndices { get; set; }
    }

    public struct ModelResources
    {
        public readonly DeviceBuffer VertexBuffer;
        public readonly DeviceBuffer IndexBuffer;
        public readonly IndexFormat IndexFormat;
        public readonly uint IndexCount;

        public ModelResources(DeviceBuffer vertexBuffer, DeviceBuffer indexBuffer, IndexFormat indexFormat, uint indexCount)
        {
            VertexBuffer = vertexBuffer;
            IndexBuffer = indexBuffer;
            IndexFormat = indexFormat;
            IndexCount = indexCount;
        }
    }

    public class ProcessedModelSerializer : BinaryAssetSerializer<ProcessedModel>
    {
        public override ProcessedModel ReadT(BinaryReader reader)
        {
            var parts = reader.ReadObjectArray(ReadMeshPart);
            var nodes = ReadNodeSet(reader);
            var animations = reader.ReadObjectArray(ReadAnimation);

            return new ProcessedModel(parts, nodes, animations);
        }

        public override void WriteT(BinaryWriter writer, ProcessedModel value)
        {
            writer.WriteObjectArray(value.MeshParts, WriteMeshPart);
            WriteNodeSet(writer, value.Nodes);
            writer.WriteObjectArray(value.Animations, WriteAnimation);
        }

        private void WriteMeshPart(BinaryWriter writer, ProcessedMeshPart part)
        {
            writer.WriteByteArray(part.VertexData);
            writer.WriteObjectArray(part.VertexElements, WriteVertexElementDesc);
            writer.WriteByteArray(part.IndexData);
            writer.WriteEnum(part.IndexFormat);
            writer.Write(part.IndexCount);
            writer.WriteDictionary(part.BoneIDsByName, (w, s) => w.Write(s), (w, v) => w.Write(v));
            writer.WriteBlittableArray(part.BoneOffsets);
        }

        private ProcessedMeshPart ReadMeshPart(BinaryReader reader)
        {
            byte[] vertexData = reader.ReadByteArray();
            VertexElementDescription[] vertexDescs = reader.ReadObjectArray(ReadVertexElementDesc);
            byte[] indexData = reader.ReadByteArray();
            IndexFormat format = reader.ReadEnum<IndexFormat>();
            uint indexCount = reader.ReadUInt32();
            var dict = reader.ReadDictionary(r => r.ReadString(), r => r.ReadUInt32());
            Matrix4x4[] boneOffsets = reader.ReadBlittableArray<Matrix4x4>();

            return new ProcessedMeshPart(
                vertexData,
                vertexDescs,
                indexData,
                format,
                indexCount,
                dict,
                boneOffsets);
        }

        private void WriteNodeSet(BinaryWriter writer, ProcessedNodeSet nodeSet)
        {
            writer.WriteObjectArray(nodeSet.Nodes, WriteNode);
            writer.Write(nodeSet.RootNodeIndex);
            writer.WriteBlittableArray(new[] { nodeSet.RootNodeInverseTransform });
        }

        private ProcessedNodeSet ReadNodeSet(BinaryReader reader)
        {
            var nodes = reader.ReadObjectArray(ReadNode);
            var rootNodeIndex = reader.ReadInt32();
            var rootNodeInverseTransform = reader.ReadBlittableArray<Matrix4x4>();
            return new ProcessedNodeSet(nodes, rootNodeIndex, rootNodeInverseTransform[0]);
        }

        private void WriteNode(BinaryWriter writer, ProcessedNode node)
        {
            writer.Write(node.Name);
            writer.WriteBlittableArray(new[] { node.Transform });
            writer.Write(node.ParentIndex);
            writer.WriteBlittableArray(node.ChildIndices);
        }

        private ProcessedNode ReadNode(BinaryReader reader)
        {
            var name = reader.ReadString();
            var transform = reader.ReadBlittableArray<Matrix4x4>();
            var parentIndex = reader.ReadInt32();
            var childIndices = reader.ReadBlittableArray<int>();
            return new ProcessedNode(name, transform[0], parentIndex, childIndices);
        }

        private void WriteAnimation(BinaryWriter writer, ProcessedAnimation animation)
        {
            writer.Write(animation.Name);
            writer.Write(animation.DurationInTicks);
            writer.Write(animation.TicksPerSecond);
            writer.WriteDictionary(animation.AnimationChannels, (w, s) => w.Write(s), WriteAnimationChannel);
        }

        private ProcessedAnimation ReadAnimation(BinaryReader reader)
        {
            var name = reader.ReadString();
            var durationInTicks = reader.ReadDouble();
            var ticksPerSecond = reader.ReadDouble();
            var animationChannels = reader.ReadDictionary(r => r.ReadString(), ReadAnimationChannel);
            return new ProcessedAnimation(name, durationInTicks, ticksPerSecond, animationChannels.ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
        }

        private void WriteAnimationChannel(BinaryWriter writer, ProcessedAnimationChannel channel)
        {
            writer.Write(channel.NodeName);
            writer.WriteBlittableArray(channel.Positions);
            writer.WriteBlittableArray(channel.Scales);
            writer.WriteBlittableArray(channel.Rotations);
        }

        private ProcessedAnimationChannel ReadAnimationChannel(BinaryReader reader)
        {
            var name = reader.ReadString();
            var positions = reader.ReadBlittableArray<VectorKey>();
            var scales = reader.ReadBlittableArray<VectorKey>();
            var rotations = reader.ReadBlittableArray<QuaternionKey>();
            return new ProcessedAnimationChannel(name, positions, scales, rotations);
        }

        private void WriteVertexElementDesc(BinaryWriter writer, VertexElementDescription desc)
        {
            writer.Write(desc.Name);
            writer.WriteEnum(desc.Semantic);
            writer.WriteEnum(desc.Format);
        }

        public VertexElementDescription ReadVertexElementDesc(BinaryReader reader)
        {
            string name = reader.ReadString();
            VertexElementSemantic semantic = reader.ReadEnum<VertexElementSemantic>();
            VertexElementFormat format = reader.ReadEnum<VertexElementFormat>();
            return new VertexElementDescription(name, format, semantic);
        }
    }
}
