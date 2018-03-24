using System.Numerics;
using Veldrid;
using SampleBase;
using System.Runtime.CompilerServices;
using System.IO;
using System;
using Common;
using System.Runtime.InteropServices;

// This example has been adapted from Sascha Willem's "instancing" example from https://github.com/SaschaWillems/Vulkan.

namespace Instancing
{
    class InstancingApplication : SampleApplication
    {
        private CommandList _commandList;

        // Shared resources
        private ResourceSet _sharedResourceSet;
        private DeviceBuffer _cameraProjViewBuffer;
        private DeviceBuffer _lightInfoBuffer;

        // Resources for instanced rocks
        private Pipeline _instancePipeline;
        private uint _instanceCount = 8000;
        private DeviceBuffer _instanceVB;
        private ResourceSet _instanceTextureSet;
        private Model _rockModel;

        // Resources for central planet
        private Pipeline _planetPipeline;
        private ResourceSet _planetTextureSet;
        private Model _planetModel;

        // Resources for the background starfield
        private Pipeline _starfieldPipeline;
        private DeviceBuffer _viewInfoBuffer;
        private ResourceSet _viewInfoSet;

        // Dynamic data
        private Vector3 _lightDir;
        private bool _lightFromCamera = false; // Press F1 to switch where the directional light originates
        private DeviceBuffer _rotationInfoBuffer; // Contains the local and global rotation values.
        private float _localRotation = 0f; // Causes individual rocks to rotate around their centers
        private float _globalRotation = 0f; // Causes rocks to rotate around the global origin (where the planet is)

        protected override void CreateResources(ResourceFactory factory)
        {
            _camera.Position = new Vector3(-36f, 20f, 100f);
            _camera.Pitch = -0.3f;
            _camera.Yaw = 0.1f;

            _cameraProjViewBuffer = factory.CreateBuffer(
                new BufferDescription((uint)(Unsafe.SizeOf<Matrix4x4>() * 2), BufferUsage.UniformBuffer | BufferUsage.Dynamic));
            _lightInfoBuffer = factory.CreateBuffer(new BufferDescription(32, BufferUsage.UniformBuffer | BufferUsage.Dynamic));
            _rotationInfoBuffer = factory.CreateBuffer(new BufferDescription(16, BufferUsage.UniformBuffer | BufferUsage.Dynamic));
            _lightDir = Vector3.Normalize(new Vector3(0.3f, -0.75f, -0.3f));

            VertexLayoutDescription sharedVertexLayout = new VertexLayoutDescription(
                new VertexElementDescription("Position", VertexElementSemantic.Position, VertexElementFormat.Float3),
                new VertexElementDescription("Normal", VertexElementSemantic.Normal, VertexElementFormat.Float3),
                new VertexElementDescription("TexCoord", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2));

            Texture rockTexture = KtxFile.LoadTexture(
                _gd,
                _factory,
                Path.Combine(AppContext.BaseDirectory, "assets", "textures", "texturearray_rocks_bc3_unorm.ktx"),
                PixelFormat.BC3_UNorm);
            TextureView rockTextureView = _factory.CreateTextureView(rockTexture);

            ResourceLayoutElementDescription[] resourceLayoutElementDescriptions =
            {
                new ResourceLayoutElementDescription("ProjView", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                new ResourceLayoutElementDescription("RotationInfo", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                new ResourceLayoutElementDescription("LightInfo", ResourceKind.UniformBuffer, ShaderStages.Fragment),
            };
            ResourceLayoutDescription resourceLayoutDescription = new ResourceLayoutDescription(resourceLayoutElementDescriptions);
            ResourceLayout sharedLayout = factory.CreateResourceLayout(resourceLayoutDescription);

            ResourceLayoutElementDescription[] textureLayoutDescriptions =
            {
                new ResourceLayoutElementDescription("Tex", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("Samp", ResourceKind.Sampler, ShaderStages.Fragment)
            };
            ResourceLayout textureLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(textureLayoutDescriptions));

            BindableResource[] bindableResources = new BindableResource[] { _cameraProjViewBuffer, _rotationInfoBuffer, _lightInfoBuffer };
            ResourceSetDescription resourceSetDescription = new ResourceSetDescription(sharedLayout, bindableResources);
            _sharedResourceSet = factory.CreateResourceSet(resourceSetDescription);

            BindableResource[] instanceBindableResources = { rockTextureView, _gd.LinearSampler };
            _instanceTextureSet = factory.CreateResourceSet(new ResourceSetDescription(textureLayout, instanceBindableResources));

            _rockModel = new Model(
                _gd,
                _factory,
                Path.Combine(AppContext.BaseDirectory, "assets", "models", "rock01.dae"),
                sharedVertexLayout,
                null);

            VertexLayoutDescription vertexLayoutPerInstance = new VertexLayoutDescription(
                new VertexElementDescription("InstancePosition", VertexElementSemantic.Position, VertexElementFormat.Float3),
                new VertexElementDescription("InstanceRotation", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
                new VertexElementDescription("InstanceScale", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
                new VertexElementDescription("InstanceTexArrayIndex", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Int1));
            vertexLayoutPerInstance.InstanceStepRate = 1;
            _instanceVB = _factory.CreateBuffer(new BufferDescription(InstanceInfo.Size * _instanceCount, BufferUsage.VertexBuffer));
            InstanceInfo[] infos = new InstanceInfo[_instanceCount];
            Random r = new Random();
            float orbitDistance = 50f;
            for (uint i = 0; i < _instanceCount / 2; i++)
            {
                float angle = (float)(r.NextDouble() * Math.PI * 2);
                infos[i] = new InstanceInfo(
                    new Vector3(
                        (MathF.Cos(angle) * orbitDistance) + (float)(-10 + r.NextDouble() * 20),
                        (float)(-1.5 + r.NextDouble() * 3),
                        (MathF.Sin(angle) * orbitDistance) + (float)(-10 + r.NextDouble() * 20)),
                    new Vector3(
                        (float)(r.NextDouble() * Math.PI * 2),
                        (float)(r.NextDouble() * Math.PI * 2),
                        (float)(r.NextDouble() * Math.PI * 2)),
                    new Vector3((float)(0.65 + r.NextDouble() * 0.35)),
                    r.Next(0, (int)rockTexture.ArrayLayers));
            }

            orbitDistance = 100f;
            for (uint i = _instanceCount / 2; i < _instanceCount; i++)
            {
                float angle = (float)(r.NextDouble() * Math.PI * 2);
                infos[i] = new InstanceInfo(
                    new Vector3(
                        (MathF.Cos(angle) * orbitDistance) + (float)(-10 + r.NextDouble() * 20),
                        (float)(-1.5 + r.NextDouble() * 3),
                        (MathF.Sin(angle) * orbitDistance) + (float)(-10 + r.NextDouble() * 20)),
                    new Vector3(
                        (float)(r.NextDouble() * Math.PI * 2),
                        (float)(r.NextDouble() * Math.PI * 2),
                        (float)(r.NextDouble() * Math.PI * 2)),
                    new Vector3((float)(0.65 + r.NextDouble() * 0.35)),
                    r.Next(0, (int)rockTexture.ArrayLayers));
            }

            _gd.UpdateBuffer(_instanceVB, 0, infos);

            Shader instanceVS = LoadShader(_factory, "Instance", ShaderStages.Vertex, "VS");
            Shader instanceFS = LoadShader(_factory, "Instance", ShaderStages.Fragment, "FS");

            GraphicsPipelineDescription pipelineDescription = new GraphicsPipelineDescription()
            {
                BlendState = BlendStateDescription.SingleOverrideBlend,
                DepthStencilState = new DepthStencilStateDescription(
                    depthTestEnabled: true,
                    depthWriteEnabled: true,
                    comparisonKind: ComparisonKind.LessEqual),
                RasterizerState = new RasterizerStateDescription(
                    cullMode: FaceCullMode.Back,
                    fillMode: PolygonFillMode.Solid,
                    frontFace: FrontFace.CounterClockwise,
                    depthClipEnabled: true,
                    scissorTestEnabled: false
                ),
                PrimitiveTopology = PrimitiveTopology.TriangleList,
                ResourceLayouts = new ResourceLayout[] { sharedLayout, textureLayout },
                ShaderSet = new ShaderSetDescription(
                    // The ordering of layouts directly impacts shader layout schemes
                    vertexLayouts: new VertexLayoutDescription[] { sharedVertexLayout, vertexLayoutPerInstance },
                    shaders: new Shader[] { instanceVS, instanceFS }
                ),
                Outputs = _gd.SwapchainFramebuffer.OutputDescription
            };

            _instancePipeline = factory.CreateGraphicsPipeline(pipelineDescription);

            // Create planet Pipeline
            // Almost everything is the same as the rock Pipeline,
            // except no instance vertex buffer is needed, and different shaders are used.
            pipelineDescription.ShaderSet = new ShaderSetDescription(
                new[] { sharedVertexLayout },
                new[]
                {
                    LoadShader(_factory, "Planet", ShaderStages.Vertex, "VS"),
                    LoadShader(_factory, "Planet", ShaderStages.Fragment, "FS"),
                });
            _planetPipeline = _factory.CreateGraphicsPipeline(pipelineDescription);

            _planetModel = new Model(
                _gd,
                _factory,
                Path.Combine(AppContext.BaseDirectory, "assets", "models", "sphere.obj"),
                sharedVertexLayout,
                null);
            Texture planetTexture = KtxFile.LoadTexture(
                _gd,
                _factory,
                Path.Combine(AppContext.BaseDirectory, "assets", "textures", "lavaplanet_bc3_unorm.ktx"),
                PixelFormat.BC3_UNorm);
            TextureView planetTextureView = _factory.CreateTextureView(planetTexture);
            _planetTextureSet = _factory.CreateResourceSet(new ResourceSetDescription(textureLayout, planetTextureView, _gd.Aniso4xSampler));

            // Starfield resources
            ResourceLayout invCameraInfoLayout = _factory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("InvCameraInfo", ResourceKind.UniformBuffer, ShaderStages.Fragment)));
            _viewInfoBuffer = _factory.CreateBuffer(new BufferDescription((uint)Unsafe.SizeOf<InvCameraInfo>(), BufferUsage.UniformBuffer | BufferUsage.Dynamic));
            _viewInfoSet = _factory.CreateResourceSet(new ResourceSetDescription(invCameraInfoLayout, _viewInfoBuffer));

            ShaderSetDescription starfieldShaders = new ShaderSetDescription(
                Array.Empty<VertexLayoutDescription>(),
                new[]
                {
                    LoadShader(_factory, "Starfield", ShaderStages.Vertex, "VS"),
                    LoadShader(_factory, "Starfield", ShaderStages.Fragment, "FS"),
                });

            _starfieldPipeline = _factory.CreateGraphicsPipeline(new GraphicsPipelineDescription(
                BlendStateDescription.SingleOverrideBlend,
                DepthStencilStateDescription.Disabled,
                RasterizerStateDescription.CullNone,
                PrimitiveTopology.TriangleList,
                starfieldShaders,
                new[] { invCameraInfoLayout },
                _gd.SwapchainFramebuffer.OutputDescription));

            _commandList = factory.CreateCommandList();
        }

        protected override void Draw(float delta)
        {
            if (InputTracker.GetKeyDown(Key.F1))
            {
                _lightFromCamera = !_lightFromCamera;
            }

            // Update per-frame resources.
            MappedResourceView<Matrix4x4> writeMap = _gd.Map<Matrix4x4>(_cameraProjViewBuffer, MapMode.Write);
            writeMap[0] = _camera.ViewMatrix;
            writeMap[1] = _camera.ProjectionMatrix;
            _gd.Unmap(_cameraProjViewBuffer);

            if (_lightFromCamera)
            {
                _gd.UpdateBuffer(_lightInfoBuffer, 0, new LightInfo(_camera.Forward, _camera.Position));
            }
            else
            {
                _gd.UpdateBuffer(_lightInfoBuffer, 0, new LightInfo(_lightDir, _camera.Position));
            }

            _localRotation += delta * (MathF.PI * 2 / 9);
            _globalRotation += -delta * (MathF.PI * 2 / 240);
            _gd.UpdateBuffer(_rotationInfoBuffer, 0, new Vector4(_localRotation, _globalRotation, 0, 0));

            Matrix4x4.Invert(_camera.ProjectionMatrix, out Matrix4x4 inverseProjection);
            Matrix4x4.Invert(_camera.ViewMatrix, out Matrix4x4 inverseView);
            _gd.UpdateBuffer(_viewInfoBuffer, 0, new InvCameraInfo(
                inverseProjection,
                inverseView));

            // Begin() must be called before commands can be issued.
            _commandList.Begin();

            // We want to render directly to the output window.
            _commandList.SetFramebuffer(_gd.SwapchainFramebuffer);
            _commandList.ClearColorTarget(0, RgbaFloat.Black);
            _commandList.ClearDepthStencil(1f);

            // First, draw the background starfield.
            _commandList.SetPipeline(_starfieldPipeline);
            _commandList.SetGraphicsResourceSet(0, _viewInfoSet);
            _commandList.Draw(4);

            // Next, draw our orbiting rocks with instanced drawing.
            _commandList.SetPipeline(_instancePipeline);
            // Set uniforms
            _commandList.SetGraphicsResourceSet(0, _sharedResourceSet); // Always after SetPipeline
            _commandList.SetGraphicsResourceSet(1, _instanceTextureSet);

            // Set all relevant state to draw the orbiting rocks.
            _commandList.SetVertexBuffer(0, _rockModel.VertexBuffer);
            _commandList.SetIndexBuffer(_rockModel.IndexBuffer, _rockModel.IndexFormat);
            _commandList.SetVertexBuffer(1, _instanceVB);

            // Issue a Draw command for two instances with 4 indices.
            _commandList.DrawIndexed(
                indexCount: _rockModel.IndexCount,
                instanceCount: _instanceCount,
                indexStart: 0,
                vertexOffset: 0,
                instanceStart: 0);

            // Next, we draw our central planet.
            _commandList.SetPipeline(_planetPipeline);
            _commandList.SetGraphicsResourceSet(1, _planetTextureSet);
            _commandList.SetVertexBuffer(0, _planetModel.VertexBuffer);
            _commandList.SetIndexBuffer(_planetModel.IndexBuffer, _planetModel.IndexFormat);

            // The planet is drawn with regular indexed drawing -- not instanced.
            _commandList.DrawIndexed(_planetModel.IndexCount);

            // End() must be called before commands can be submitted for execution.
            _commandList.End();
            _gd.SubmitCommands(_commandList);

            // Once commands have been submitted, the rendered image can be presented to the application window.
            _gd.SwapBuffers();
        }
    }

    public struct InstanceInfo
    {
        public static uint Size { get; } = (uint)Unsafe.SizeOf<InstanceInfo>();

        public Vector3 Position;
        public Vector3 Rotation;
        public Vector3 Scale;
        public int TexArrayIndex;

        public InstanceInfo(Vector3 position, Vector3 rotation, Vector3 scale, int texArrayIndex)
        {
            Position = position;
            Rotation = rotation;
            Scale = scale;
            TexArrayIndex = texArrayIndex;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct LightInfo
    {
        public Vector3 LightDirection;
        private float padding0;
        public Vector3 CameraPosition;
        private float padding1;

        public LightInfo(Vector3 lightDirection, Vector3 cameraPosition)
        {
            LightDirection = lightDirection;
            CameraPosition = cameraPosition;
            padding0 = 0;
            padding1 = 0;
        }
    }

    public struct InvCameraInfo
    {
        public Matrix4x4 InverseProjection;
        public Matrix4x4 InverseView;

        public InvCameraInfo(Matrix4x4 inverseProjection, Matrix4x4 inverseView)
        {
            InverseProjection = inverseProjection;
            InverseView = inverseView;
        }
    }
}
