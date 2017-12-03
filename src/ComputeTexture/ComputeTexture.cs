using System;
using System.IO;
using System.Numerics;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;
using Veldrid.Utilities;

namespace ComputeTexture
{
    internal class ComputeTexture
    {
        private readonly Sdl2Window _window;
        private readonly GraphicsDevice _gd;
        private bool _windowResized;

        private Veldrid.Buffer _screenSizeBuffer;
        private Veldrid.Buffer _shiftBuffer;
        private Veldrid.Buffer _vertexBuffer;
        private Veldrid.Buffer _indexBuffer;
        private Shader _computeShader;
        private ResourceLayout _computeLayout;
        private Pipeline _computePipeline;
        private ResourceSet _computeResourceSet;
        private Shader _vertexShader;
        private Shader _fragmentShader;
        private Pipeline _graphicsPipeline;
        private ResourceSet _graphicsResourceSet;
        private CommandList _cl;

        private Texture _computeTargetTexture;
        private TextureView _computeTargetTextureView;
        private ResourceLayout _graphicsLayout;

        public ComputeTexture()
        {
            WindowCreateInfo wci = new WindowCreateInfo
            {
                X = 100,
                Y = 100,
                WindowWidth = 960,
                WindowHeight = 540,
                WindowTitle = "Compute Texture"
            };
            _window = VeldridStartup.CreateWindow(ref wci);
            _window.Resized += () => _windowResized = true;

            GraphicsDeviceOptions options = new GraphicsDeviceOptions();
#if DEBUG
            options.Debug = true;
#endif
            _gd = VeldridStartup.CreateGraphicsDevice(_window, options);
        }

        public void Run()
        {
            DisposeCollectorResourceFactory factory = new DisposeCollectorResourceFactory(_gd.ResourceFactory);
            CreateResources(factory);

            while (_window.Exists)
            {
                _window.PumpEvents();

                if (_window.Exists)
                {
                    Draw();
                }
            }

            factory.DisposeCollector.DisposeAll();
        }

        private void CreateResources(ResourceFactory factory)
        {
            _screenSizeBuffer = factory.CreateBuffer(new BufferDescription(16, BufferUsage.UniformBuffer));
            _shiftBuffer = factory.CreateBuffer(new BufferDescription(16, BufferUsage.UniformBuffer));
            _vertexBuffer = factory.CreateBuffer(new BufferDescription(16 * 4, BufferUsage.VertexBuffer));
            _indexBuffer = factory.CreateBuffer(new BufferDescription(2 * 6, BufferUsage.IndexBuffer));

            _computeShader = factory.CreateShader(new ShaderDescription(
                ShaderStages.Compute,
                File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Shaders", $"Compute.{GetExtension(factory.BackendType)}"))));

            _computeLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("Tex", ResourceKind.TextureReadWrite, ShaderStages.Compute),
                new ResourceLayoutElementDescription("ScreenSizeBuffer", ResourceKind.UniformBuffer, ShaderStages.Compute),
                new ResourceLayoutElementDescription("ShiftBuffer", ResourceKind.UniformBuffer, ShaderStages.Compute)));

            ComputePipelineDescription computePipelineDesc = new ComputePipelineDescription(
                new ShaderStageDescription(ShaderStages.Compute, _computeShader, "CS"),
                new[] { _computeLayout });
            _computePipeline = factory.CreateComputePipeline(ref computePipelineDesc);

            _vertexShader = factory.CreateShader(new ShaderDescription(
                ShaderStages.Vertex,
                File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Shaders", $"Vertex.{GetExtension(factory.BackendType)}"))));
            _fragmentShader = factory.CreateShader(new ShaderDescription(
                ShaderStages.Fragment,
                File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Shaders", $"Fragment.{GetExtension(factory.BackendType)}"))));

            ShaderSetDescription shaderSet = new ShaderSetDescription(
                new VertexLayoutDescription[]
                {
                    new VertexLayoutDescription(
                        new VertexElementDescription("Position", VertexElementSemantic.Position, VertexElementFormat.Float2),
                        new VertexElementDescription("TexCoord", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2))
                },
                new[]
                {
                    new ShaderStageDescription(ShaderStages.Vertex, _vertexShader, "VS"),
                    new ShaderStageDescription(ShaderStages.Fragment, _fragmentShader, "FS")
                });

            _graphicsLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("Tex", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("Tex11", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("Tex22", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("SS", ResourceKind.Sampler, ShaderStages.Fragment)));

            GraphicsPipelineDescription fullScreenQuadDesc = new GraphicsPipelineDescription(
                BlendStateDescription.SingleOverrideBlend,
                DepthStencilStateDescription.Disabled,
                new RasterizerStateDescription(FaceCullMode.Back, PolygonFillMode.Solid, FrontFace.Clockwise, false, false),
                PrimitiveTopology.TriangleList,
                shaderSet,
                new[] { _graphicsLayout },
                _gd.SwapchainFramebuffer.OutputDescription);

            _graphicsPipeline = factory.CreateGraphicsPipeline(ref fullScreenQuadDesc);

            _cl = factory.CreateCommandList();

            CreateWindowSizedResources();
            InitResources(factory);
        }

        private void CreateWindowSizedResources()
        {
            _computeTargetTexture?.Dispose();
            _computeTargetTextureView?.Dispose();
            _computeResourceSet?.Dispose();
            _graphicsResourceSet?.Dispose();

            ResourceFactory factory = _gd.ResourceFactory;

            _computeTargetTexture = factory.CreateTexture(new TextureDescription(
                (uint)_window.Width,
                (uint)_window.Height,
                1,
                1,
                1,
                PixelFormat.R32_G32_B32_A32_Float,
                TextureUsage.Sampled | TextureUsage.Storage));

            _computeTargetTextureView = factory.CreateTextureView(_computeTargetTexture);

            _computeResourceSet = factory.CreateResourceSet(new ResourceSetDescription(
                _computeLayout,
                _computeTargetTextureView,
                _screenSizeBuffer,
                _shiftBuffer));

            _graphicsResourceSet = factory.CreateResourceSet(new ResourceSetDescription(
                _graphicsLayout,
                _computeTargetTextureView,
                _computeTargetTextureView,
                _computeTargetTextureView,
                _gd.PointSampler));
        }

        private object GetExtension(GraphicsBackend backendType)
        {
            return backendType == GraphicsBackend.Direct3D11 ? "hlsl.bytes"
                : backendType == GraphicsBackend.Vulkan ? "spv"
                    : "430.glsl";
        }

        private void InitResources(ResourceFactory factory)
        {
            _cl.Begin();
            _cl.UpdateBuffer(_screenSizeBuffer, 0, new Vector4(_window.Width, _window.Height, 0, 0));

            Vector4[] quadVerts =
            {
                new Vector4(-1, 1, 0, 0),
                new Vector4(1, 1, 1, 0),
                new Vector4(1, -1, 1, 1),
                new Vector4(-1, -1, 0, 1),
            };

            ushort[] indices = { 0, 1, 2, 0, 2, 3 };

            _cl.UpdateBuffer(_vertexBuffer, 0, quadVerts);
            _cl.UpdateBuffer(_indexBuffer, 0, indices);

            _cl.End();
            _gd.ExecuteCommands(_cl);
            _gd.WaitForIdle();
        }

        private void Draw()
        {
            _cl.Begin();
            if (_windowResized)
            {
                _windowResized = false;
                _gd.ResizeMainWindow((uint)_window.Width, (uint)_window.Height);
                _cl.UpdateBuffer(_screenSizeBuffer, 0, new Vector4(_window.Width, _window.Height, 0, 0));
                CreateWindowSizedResources();
            }

            int ticks = Environment.TickCount;
            Vector4 shifts = new Vector4(
                _window.Width * MathF.Cos(ticks / 500f), // Red shift
                _window.Height * MathF.Sin(ticks / 1250f), // Green shift
                MathF.Sin(ticks / 1000f), // Blue shift
                0); // Padding
            _cl.UpdateBuffer(_shiftBuffer, 0, ref shifts);

            _cl.SetPipeline(_computePipeline);
            _cl.SetComputeResourceSet(0, _computeResourceSet);
            _cl.Dispatch((uint)_window.Width, (uint)_window.Height, 1);

            _cl.SetFramebuffer(_gd.SwapchainFramebuffer);
            _cl.SetFullViewports();
            _cl.SetFullScissorRects();
            _cl.ClearColorTarget(0, RgbaFloat.Black);
            _cl.SetPipeline(_graphicsPipeline);
            _cl.SetVertexBuffer(0, _vertexBuffer);
            _cl.SetIndexBuffer(_indexBuffer, IndexFormat.UInt16);
            _cl.SetGraphicsResourceSet(0, _graphicsResourceSet);
            _cl.DrawIndexed(6, 1, 0, 0, 0);

            _cl.End();
            _gd.ExecuteCommands(_cl);
            _gd.SwapBuffers();
        }
    }

    struct ParticleInfo
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public Vector4 Color;
        public ParticleInfo(Vector2 position, Vector2 velocity, Vector4 color)
        {
            Position = position;
            Velocity = velocity;
            Color = color;
        }
    }
}
