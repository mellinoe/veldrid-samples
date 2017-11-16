using System;
using System.IO;
using System.Numerics;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;

namespace GettingStarted
{
    class Program
    {
        private static GraphicsDevice _graphicsDevice;
        private static CommandList _commandList;
        private static VertexBuffer _vertexBuffer;
        private static IndexBuffer _indexBuffer;
        private static Shader _vertexShader;
        private static Shader _fragmentShader;
        private static Pipeline _pipeline;

        static void Main(string[] args)
        {
            WindowCreateInfo windowCI = new WindowCreateInfo()
            {
                X = 100,
                Y = 100,
                WindowWidth = 960,
                WindowHeight = 540,
                WindowTitle = "Veldrid Getting Started Tutorial"
            };
            GraphicsDeviceCreateInfo gdCI = new GraphicsDeviceCreateInfo();

            VeldridStartup.CreateWindowAndGraphicsDevice(ref windowCI, ref gdCI, out Sdl2Window window, out _graphicsDevice);
            bool resized = false;
            window.Resized += () => resized = true;

            CreateResources();

            while (window.Exists)
            {
                window.PumpEvents();

                if (window.Exists)
                {
                    if (resized)
                    {
                        _graphicsDevice.ResizeMainWindow((uint)window.Width, (uint)window.Height);
                    }
                    Draw();
                }
                else
                {
                }
            }

            DisposeResources();
        }

        private static void CreateResources()
        {
            ResourceFactory factory = _graphicsDevice.ResourceFactory;
            _commandList = factory.CreateCommandList();

            _vertexBuffer = factory.CreateVertexBuffer(new BufferDescription(4 * VertexPositionColor.SizeInBytes));
            _indexBuffer = factory.CreateIndexBuffer(new IndexBufferDescription(4 * sizeof(ushort), IndexFormat.UInt16));

            // Begin command list for resource updates.
            _commandList.Begin();

            VertexPositionColor[] vertexData =
            {
                new VertexPositionColor(new Vector2(-.75f, .75f), RgbaFloat.Red),
                new VertexPositionColor(new Vector2(.75f, .75f), RgbaFloat.Green),
                new VertexPositionColor(new Vector2(-.75f, -.75f), RgbaFloat.Blue),
                new VertexPositionColor(new Vector2(.75f, -.75f), RgbaFloat.Yellow)
            };
            _commandList.UpdateBuffer(_vertexBuffer, 0, vertexData);

            ushort[] indexData = { 0, 1, 2, 3 };
            _commandList.UpdateBuffer(_indexBuffer, 0, indexData);

            // End command list and execute it.
            _commandList.End();
            _graphicsDevice.ExecuteCommands(_commandList);

            VertexLayoutDescription vertexLayout = new VertexLayoutDescription(
                new VertexElementDescription("Position", VertexElementSemantic.Position, VertexElementFormat.Float2),
                new VertexElementDescription("Color", VertexElementSemantic.Color, VertexElementFormat.Float4));

            _vertexShader = LoadShader(ShaderStages.Vertex);
            _vertexShader.Dispose();
            _graphicsDevice.WaitForIdle();
            _vertexShader = LoadShader(ShaderStages.Vertex);
            _fragmentShader = LoadShader(ShaderStages.Fragment);

            ShaderStageDescription[] shaderStages =
            {
                new ShaderStageDescription(ShaderStages.Vertex, _vertexShader, "VS"),
                new ShaderStageDescription(ShaderStages.Fragment, _fragmentShader, "FS")
            };

            ShaderSetDescription shaderSet = new ShaderSetDescription(
                new VertexLayoutDescription[] { vertexLayout }, // We use a single vertex buffer for all attributes.
                shaderStages);

            // Create pipeline
            PipelineDescription pipelineDescription = new PipelineDescription();
            pipelineDescription.BlendState = BlendStateDescription.SingleOverrideBlend;
            pipelineDescription.DepthStencilState = DepthStencilStateDescription.Disabled;
            pipelineDescription.RasterizerState = RasterizerStateDescription.Default;
            pipelineDescription.PrimitiveTopology = PrimitiveTopology.TriangleStrip;
            pipelineDescription.ResourceLayouts = Array.Empty<ResourceLayout>();
            pipelineDescription.ShaderSet = shaderSet;
            pipelineDescription.Outputs = _graphicsDevice.SwapchainFramebuffer.OutputDescription;

            _pipeline = factory.CreatePipeline(ref pipelineDescription);
        }

        private static Shader LoadShader(ShaderStages stage)
        {
            string extension = null;
            switch (_graphicsDevice.BackendType)
            {
                case GraphicsBackend.Direct3D11:
                    extension = "hlsl.bytes";
                    break;
                case GraphicsBackend.Vulkan:
                    extension = "spv";
                    break;
                case GraphicsBackend.OpenGL:
                    extension = "glsl";
                    break;
                default: throw new InvalidOperationException();
            }

            string path = Path.Combine(AppContext.BaseDirectory, "Shaders", $"{stage.ToString()}.{extension}");
            byte[] shaderBytes = File.ReadAllBytes(path);
            return _graphicsDevice.ResourceFactory.CreateShader(new ShaderDescription(stage, shaderBytes));
        }

        private static void Draw()
        {
            // Begin() must be called before commands can be issued.
            _commandList.Begin();

            // We want to render directly to the output window.
            _commandList.SetFramebuffer(_graphicsDevice.SwapchainFramebuffer);
            _commandList.SetFullViewports();
            _commandList.ClearColorTarget(0, RgbaFloat.Black);

            // Set all relevant state to draw our quad.
            _commandList.SetVertexBuffer(0, _vertexBuffer);
            _commandList.SetIndexBuffer(_indexBuffer);
            _commandList.SetPipeline(_pipeline);
            // Issue a Draw command for a single instance with 4 indices.
            _commandList.Draw(4, 1, 0, 0, 0);

            // End() must be called before commands can be submitted for execution.
            _commandList.End();
            _graphicsDevice.ExecuteCommands(_commandList);

            // Once commands have been submitted, the rendered image can be presented to the application window.
            _graphicsDevice.SwapBuffers();
        }

        private static void DisposeResources()
        {
            _graphicsDevice.WaitForIdle();
            _pipeline.Dispose();
            _vertexShader.Dispose();
            _fragmentShader.Dispose();
            _commandList.Dispose();
            _vertexBuffer.Dispose();
            _indexBuffer.Dispose();
            _graphicsDevice.Dispose();
        }
    }

    struct VertexPositionColor
    {
        public const uint SizeInBytes = 24;

        public Vector2 Position;
        public RgbaFloat Color;

        public VertexPositionColor(Vector2 position, RgbaFloat color)
        {
            Position = position;
            Color = color;
        }
    }
}
