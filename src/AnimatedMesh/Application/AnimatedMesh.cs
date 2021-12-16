using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;
using AssetPrimitives;
using SampleBase;
using Veldrid;
using Veldrid.SPIRV;

namespace AnimatedMesh
{
    public class AnimatedMesh : SampleApplication
    {
        private DeviceBuffer _projectionBuffer;
        private DeviceBuffer _viewBuffer;
        private DeviceBuffer _worldBuffer;
        private DeviceBuffer _vertexBuffer;
        private DeviceBuffer _indexBuffer;
        private uint _indexCount;
        private DeviceBuffer _bonesBuffer;
        private TextureView _texView;
        private ResourceSet _rs;
        private CommandList _cl;
        private Pipeline _pipeline;

        private ProcessedAnimation _modelAnimation;
        private IDictionary<string, uint> _boneIDsByName;
        private double _previousAnimSeconds = 0;
        private ProcessedModel _model;
        private ProcessedMeshPart _modelMesh;
        private Matrix4x4 _rootNodeInverseTransform;
        private Matrix4x4[] _boneTransformations;
        private float _animationTimeScale = 1f;

        public AnimatedMesh(ApplicationWindow window) : base(window) { }

        protected override void CreateResources(ResourceFactory factory)
        {
            _projectionBuffer = factory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer | BufferUsage.Dynamic));
            _viewBuffer = factory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer | BufferUsage.Dynamic));
            _worldBuffer = factory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer | BufferUsage.Dynamic));
            Matrix4x4 worldMatrix =
                Matrix4x4.CreateTranslation(0, 15000, -5000)
                * Matrix4x4.CreateRotationX(3 * (float)Math.PI / 2)
                * Matrix4x4.CreateScale(0.05f);
            GraphicsDevice.UpdateBuffer(_worldBuffer, 0, ref worldMatrix);

            ResourceLayout layout = factory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("ProjectionBuffer", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                new ResourceLayoutElementDescription("ViewBuffer", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                new ResourceLayoutElementDescription("WorldBuffer", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                new ResourceLayoutElementDescription("BonesBuffer", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                new ResourceLayoutElementDescription("SurfaceTex", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("SurfaceSampler", ResourceKind.Sampler, ShaderStages.Fragment)));

            Texture texture;

            var ktxData = LoadEmbeddedAsset<byte[]>("goblin_bc3_unorm.binary");
            texture = KtxFile.LoadTexture(
                GraphicsDevice,
                factory,
                ktxData,
                PixelFormat.BC3_UNorm);
            _texView = ResourceFactory.CreateTextureView(texture);

            _model = LoadEmbeddedAsset<ProcessedModel>("goblin.binary");
            _modelMesh = _model.MeshParts[0];
            _modelAnimation = _model.Animations[0];
            _boneIDsByName = _modelMesh.BoneIDsByName;

            VertexLayoutDescription vertexLayouts = new VertexLayoutDescription(_modelMesh.VertexElements);

            GraphicsPipelineDescription gpd = new GraphicsPipelineDescription(
                BlendStateDescription.SingleOverrideBlend,
                DepthStencilStateDescription.DepthOnlyLessEqual,
                new RasterizerStateDescription(FaceCullMode.Back, PolygonFillMode.Solid, FrontFace.Clockwise, true, false),
                PrimitiveTopology.TriangleList,
                new ShaderSetDescription(
                    new[] { vertexLayouts },
                    factory.CreateFromSpirv(
                        new ShaderDescription(ShaderStages.Vertex, Encoding.UTF8.GetBytes(VertexCode), "main"),
                        new ShaderDescription(ShaderStages.Fragment, Encoding.UTF8.GetBytes(FragmentCode), "main"))),
                layout,
                GraphicsDevice.SwapchainFramebuffer.OutputDescription);
            _pipeline = factory.CreateGraphicsPipeline(ref gpd);

            _rootNodeInverseTransform = _model.Nodes.RootNodeInverseTransform;
            _boneTransformations = new Matrix4x4[_modelMesh.BoneOffsets.Length];

            _bonesBuffer = ResourceFactory.CreateBuffer(new BufferDescription(
                64 * 64, BufferUsage.UniformBuffer | BufferUsage.Dynamic));

            _rs = factory.CreateResourceSet(new ResourceSetDescription(layout,
                _projectionBuffer, _viewBuffer, _worldBuffer, _bonesBuffer, _texView, GraphicsDevice.Aniso4xSampler));

            _indexCount = _modelMesh.IndexCount;

            _vertexBuffer = ResourceFactory.CreateBuffer(new BufferDescription((uint)_modelMesh.VertexData.Length, BufferUsage.VertexBuffer));
            GraphicsDevice.UpdateBuffer(_vertexBuffer, 0, _modelMesh.VertexData);

            _indexBuffer = ResourceFactory.CreateBuffer(new BufferDescription((uint)_modelMesh.IndexData.Length, BufferUsage.IndexBuffer));
            GraphicsDevice.UpdateBuffer(_indexBuffer, 0, _modelMesh.IndexData);

            _cl = factory.CreateCommandList();
            _camera.Position = new Vector3(110, -87, -532);
            _camera.Yaw = 0.45f;
            _camera.Pitch = -0.55f;
            _camera.MoveSpeed = 1000f;
            _camera.FarDistance = 100000;
        }

        protected override void Draw(float deltaSeconds)
        {
            UpdateAnimation(deltaSeconds);
            UpdateUniforms();
            _cl.Begin();
            _cl.SetFramebuffer(GraphicsDevice.SwapchainFramebuffer);
            _cl.ClearColorTarget(0, RgbaFloat.Black);
            _cl.ClearDepthStencil(1f);
            _cl.SetPipeline(_pipeline);
            _cl.SetGraphicsResourceSet(0, _rs);
            _cl.SetVertexBuffer(0, _vertexBuffer);
            _cl.SetIndexBuffer(_indexBuffer, IndexFormat.UInt32);
            _cl.DrawIndexed(_indexCount);
            _cl.End();
            GraphicsDevice.SubmitCommands(_cl);
            GraphicsDevice.SwapBuffers();
        }

        private void UpdateAnimation(float deltaSeconds)
        {
            double totalSeconds = _modelAnimation.DurationInTicks * _modelAnimation.TicksPerSecond;
            double newSeconds = _previousAnimSeconds + (deltaSeconds * _animationTimeScale);
            newSeconds = newSeconds % totalSeconds;
            _previousAnimSeconds = newSeconds;

            double ticks = newSeconds * _modelAnimation.TicksPerSecond;

            UpdateChannel(ticks, _model.Nodes.Nodes[_model.Nodes.RootNodeIndex], Matrix4x4.Identity);

            GraphicsDevice.UpdateBuffer(_bonesBuffer, 0, _boneTransformations);
        }

        private void UpdateChannel(double time, ProcessedNode node, Matrix4x4 parentTransform)
        {
            Matrix4x4 nodeTransformation = node.Transform;

            if (GetChannel(node, out var channel))
            {
                Matrix4x4 scale = InterpolateScale(time, channel);
                Matrix4x4 rotation = InterpolateRotation(time, channel);
                Matrix4x4 translation = InterpolateTranslation(time, channel);

                nodeTransformation = scale * rotation * translation;
            }

            if (_boneIDsByName.TryGetValue(node.Name, out uint boneID))
            {
                Matrix4x4 m = _modelMesh.BoneOffsets[boneID]
                    * nodeTransformation
                    * parentTransform
                    * _rootNodeInverseTransform;
                _boneTransformations[boneID] = m;
            }

            foreach (var childIdx in node.ChildIndices)
            {
                var childNode = _model.Nodes.Nodes[childIdx];
                UpdateChannel(time, childNode, nodeTransformation * parentTransform);
            }
        }

        private Matrix4x4 InterpolateTranslation(double time, ProcessedAnimationChannel channel)
        {
            Vector3 position;

            if (channel.Positions.Length == 1)
            {
                position = channel.Positions[0].Value;
            }
            else
            {
                uint frameIndex = 0;
                for (uint i = 0; i < channel.Positions.Length - 1; i++)
                {
                    if (time < (float)channel.Positions[(int)(i + 1)].Time)
                    {
                        frameIndex = i;
                        break;
                    }
                }

                VectorKey currentFrame = channel.Positions[(int)frameIndex];
                VectorKey nextFrame = channel.Positions[(int)((frameIndex + 1) % channel.Positions.Length)];

                double delta = (time - (float)currentFrame.Time) / (float)(nextFrame.Time - currentFrame.Time);

                var start = currentFrame.Value;
                var end = nextFrame.Value;
                position = (start + (float)delta * (end - start));
            }

            return Matrix4x4.CreateTranslation(position);
        }

        private Matrix4x4 InterpolateRotation(double time, ProcessedAnimationChannel channel)
        {
            Quaternion rotation;

            if (channel.Rotations.Length == 1)
            {
                rotation = channel.Rotations[0].Value;
            }
            else
            {
                uint frameIndex = 0;
                for (uint i = 0; i < channel.Rotations.Length - 1; i++)
                {
                    if (time < (float)channel.Rotations[(int)(i + 1)].Time)
                    {
                        frameIndex = i;
                        break;
                    }
                }

                QuaternionKey currentFrame = channel.Rotations[(int)frameIndex];
                QuaternionKey nextFrame = channel.Rotations[(int)((frameIndex + 1) % channel.Rotations.Length)];

                double delta = (time - (float)currentFrame.Time) / (float)(nextFrame.Time - currentFrame.Time);

                var start = currentFrame.Value;
                var end = nextFrame.Value;
                rotation = Quaternion.Slerp(start, end, (float)delta);
                rotation = Quaternion.Normalize(rotation);
            }

            return Matrix4x4.CreateFromQuaternion(rotation);
        }

        private Matrix4x4 InterpolateScale(double time, ProcessedAnimationChannel channel)
        {
            Vector3 scale;

            if (channel.Scales.Length == 1)
            {
                scale = channel.Scales[0].Value;
            }
            else
            {
                uint frameIndex = 0;
                for (uint i = 0; i < channel.Scales.Length - 1; i++)
                {
                    if (time < (float)channel.Scales[(int)(i + 1)].Time)
                    {
                        frameIndex = i;
                        break;
                    }
                }

                VectorKey currentFrame = channel.Scales[(int)frameIndex];
                VectorKey nextFrame = channel.Scales[(int)((frameIndex + 1) % channel.Scales.Length)];

                double delta = (time - (float)currentFrame.Time) / (float)(nextFrame.Time - currentFrame.Time);

                Vector3 start = currentFrame.Value;
                Vector3 end = nextFrame.Value;

                scale = (start + (float)delta * (end - start));
            }

            return Matrix4x4.CreateScale(scale);
        }

        private bool GetChannel(ProcessedNode node, out ProcessedAnimationChannel channel)
        {
            foreach (var kvp in _modelAnimation.AnimationChannels)
            {
                var c = kvp.Value;
                if (c.NodeName == node.Name)
                {
                    channel = c;
                    return true;
                }
            }

            channel = null;
            return false;
        }

        protected override void OnKeyDown(KeyEvent keyEvent)
        {
            if (keyEvent.Key == Key.KeypadPlus)
            {
                _animationTimeScale = Math.Min(3, _animationTimeScale + 0.25f);
            }
            if (keyEvent.Key == Key.KeypadMinus)
            {
                _animationTimeScale = Math.Max(0, _animationTimeScale - 0.25f);
            }
        }

        private void UpdateUniforms()
        {
            GraphicsDevice.UpdateBuffer(_projectionBuffer, 0, _camera.ProjectionMatrix);
            GraphicsDevice.UpdateBuffer(_viewBuffer, 0, _camera.ViewMatrix);
        }

        private const string VertexCode = @"
#version 450

layout(set = 0, binding = 0) uniform ProjectionBuffer
{
    mat4 Projection;
};

layout(set = 0, binding = 1) uniform ViewBuffer
{
    mat4 View;
};

layout(set = 0, binding = 2) uniform WorldBuffer
{
    mat4 World;
};

layout(set = 0, binding = 3) uniform BonesBuffer
{
    mat4 BonesTransformations[64];
};

layout(location = 0) in vec3 Position;
layout(location = 1) in vec2 UV;
layout(location = 2) in vec4 BoneWeights;
layout(location = 3) in uvec4 BoneIndices;
layout(location = 0) out vec2 fsin_uv;

void main()
{
    mat4 boneTransformation = BonesTransformations[BoneIndices.x]  * BoneWeights.x;
    boneTransformation += BonesTransformations[BoneIndices.y]  * BoneWeights.y;
    boneTransformation += BonesTransformations[BoneIndices.z]  * BoneWeights.z;
    boneTransformation += BonesTransformations[BoneIndices.w]  * BoneWeights.w;
    gl_Position = Projection * View * World * boneTransformation * vec4(Position, 1);
    fsin_uv = UV;
}";

        private const string FragmentCode = @"
#version 450

layout(set = 0, binding = 4) uniform texture2D SurfaceTex;
layout(set = 0, binding = 5) uniform sampler SurfaceSampler;

layout(location = 0) in vec2 fsin_uv;
layout(location = 0) out vec4 fsout_color;

void main()
{
    fsout_color = texture(sampler2D(SurfaceTex, SurfaceSampler), fsin_uv);
}";
    }
}
