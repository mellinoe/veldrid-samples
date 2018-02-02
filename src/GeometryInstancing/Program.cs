using System.IO;
using System.Numerics;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;
using Veldrid.NeoDemo;

namespace GeometryInstancing
{
    class Program
    {
        private static GraphicsDevice _graphicsDevice;
        private static CommandList _commandList;
        private static DeviceBuffer _vertexBuffer;
        private static DeviceBuffer _indexBuffer;
        private static DeviceBuffer _xOffsetBuffer;
        private static DeviceBuffer _cameraProjViewBuffer;
        private static Shader _vertexShader;
        private static Shader _fragmentShader;
        private static Pipeline _pipeline;
        private static Camera _camera;
        private static ResourceSet _resourceSet;
        private static ResourceLayout _resourceLayout;

        static void Main(string[] args)
        {
            WindowCreateInfo windowCI = new WindowCreateInfo()
            {
                X = 100,
                Y = 100,
                WindowWidth = 960,
                WindowHeight = 540,
                WindowTitle = "Veldrid Tutorial"
            };
            Sdl2Window window = VeldridStartup.CreateWindow(ref windowCI);
         
            _camera = new Camera(960,540);

            _graphicsDevice = VeldridStartup.CreateGraphicsDevice(window,GraphicsBackend.OpenGL);
            //_graphicsDevice = VeldridStartup.CreateGraphicsDevice(window); // Defaults to metal on mac

            CreateResources();

            while (window.Exists)
            {
                InputSnapshot inputSnapshot = window.PumpEvents();

                if (window.Exists)
                {
                    InputTracker.UpdateFrameInput(inputSnapshot);

                    float deltaSeconds = 1/60f;
                    _camera.Update(deltaSeconds);

                    Draw();
                }
            }

            DisposeResources();
        }

        private static void CreateResources()
        {
            ResourceFactory _factory = _graphicsDevice.ResourceFactory;

            // Setup Uniform Layout, (Opengl) Veldrid expects a single struct per Uniform
            // One Matrix4x4 = 64 Bytes for floats => 128 for 2
            _cameraProjViewBuffer = _factory.CreateBuffer(new BufferDescription(128,BufferUsage.UniformBuffer));

            ResourceLayoutElementDescription resourceLayoutElementDescription = new ResourceLayoutElementDescription("projView",ResourceKind.UniformBuffer,ShaderStages.Vertex);
            ResourceLayoutElementDescription[] resourceLayoutElementDescriptions = {resourceLayoutElementDescription};
            ResourceLayoutDescription resourceLayoutDescription = new ResourceLayoutDescription(resourceLayoutElementDescriptions);
            BindableResource[] bindableResources = new BindableResource[]{_cameraProjViewBuffer};

            _resourceLayout = _factory.CreateResourceLayout(resourceLayoutDescription);
            ResourceSetDescription resourceSetDescription = new ResourceSetDescription(_resourceLayout,bindableResources);
            
            _resourceSet = _factory.CreateResourceSet(resourceSetDescription);

            VertexPositionColour[] quadVerticies = {
                new VertexPositionColour(new Vector2(-1.0f,1.0f),RgbaFloat.Red),
                new VertexPositionColour(new Vector2(1.0f,1.0f),RgbaFloat.Green),
                new VertexPositionColour(new Vector2(-1.0f,-1.0f),RgbaFloat.Blue),
                new VertexPositionColour(new Vector2(1.0f,-1.0f),RgbaFloat.Yellow)
            };

            ushort[] quadIndicies = { 0, 1, 2, 3 };

            float[] _xOffset = {-2,2};

            // declare (VBO) buffers
            _vertexBuffer 
                = _factory.CreateBuffer(new BufferDescription(4 * VertexPositionColour.SizeInBytes, BufferUsage.VertexBuffer));
            _indexBuffer 
                = _factory.CreateBuffer(new BufferDescription(4*sizeof(ushort),BufferUsage.IndexBuffer));
            _xOffsetBuffer
                = _factory.CreateBuffer(new BufferDescription(2*sizeof(float),BufferUsage.VertexBuffer));

            // fill buffers with data
            _graphicsDevice.UpdateBuffer(_vertexBuffer,0,quadVerticies);
            _graphicsDevice.UpdateBuffer(_indexBuffer,0,quadIndicies);
            _graphicsDevice.UpdateBuffer(_xOffsetBuffer,0,_xOffset);
            _graphicsDevice.UpdateBuffer(_cameraProjViewBuffer,0,_camera.ViewMatrix);
            _graphicsDevice.UpdateBuffer(_cameraProjViewBuffer,64,_camera.ProjectionMatrix);

            VertexLayoutDescription vertexLayout 
                = new VertexLayoutDescription(
                    new VertexElementDescription("Position",VertexElementSemantic.Position,VertexElementFormat.Float2),
                    new VertexElementDescription("Colour",VertexElementSemantic.Color,VertexElementFormat.Float4)
                );
            
            VertexElementDescription vertexElementPerInstance
                = new VertexElementDescription("xOff",VertexElementSemantic.Position,VertexElementFormat.Float1);

            VertexLayoutDescription vertexLayoutPerInstance 
                = new VertexLayoutDescription(
                    stride: 4,
                    instanceStepRate: 1,
                    elements: new VertexElementDescription[] {vertexElementPerInstance}
                );

            _vertexShader = LoadShader(ShaderStages.Vertex);
            _fragmentShader = LoadShader(ShaderStages.Fragment);

            GraphicsPipelineDescription pipelineDescription = new GraphicsPipelineDescription(){
                BlendState = BlendStateDescription.SingleOverrideBlend,
                DepthStencilState = new DepthStencilStateDescription(
                    depthTestEnabled: true,
                    depthWriteEnabled: true,
                    comparisonKind: ComparisonKind.LessEqual),
                RasterizerState = new RasterizerStateDescription(
                    cullMode: FaceCullMode.Back,
                    fillMode: PolygonFillMode.Solid,
                    frontFace: FrontFace.Clockwise,
                    depthClipEnabled: true,
                    scissorTestEnabled: false
                ),
                PrimitiveTopology = PrimitiveTopology.TriangleStrip,
                ResourceLayouts = new ResourceLayout[] {_resourceLayout},
                ShaderSet = new ShaderSetDescription(
                    // The ordering of layouts directly impacts shader layout schemes
                    vertexLayouts: new VertexLayoutDescription[] {vertexLayout,vertexLayoutPerInstance},
                    shaders: new Shader[] {_vertexShader,_fragmentShader}
                ),
                Outputs = _graphicsDevice.SwapchainFramebuffer.OutputDescription
            };

            _pipeline = _factory.CreateGraphicsPipeline(pipelineDescription);

            _commandList = _factory.CreateCommandList();

        }

        private static Shader LoadShader(ShaderStages stage)
        {
            string extension = null;
            switch (_graphicsDevice.BackendType)
            {
                case GraphicsBackend.Direct3D11:
                    extension = "hlsl.bytes";
                    // not implemented
                    throw new System.InvalidOperationException();
                    //break;
                case GraphicsBackend.Vulkan:
                    extension = "spv";
                    // not implemented
                    throw new System.InvalidOperationException();
                    //break;
                case GraphicsBackend.OpenGL:
                    extension = "glsl";
                    break;
                case GraphicsBackend.Metal:
                    extension = "metallib";
                    // not implemented
                    throw new System.InvalidOperationException();
                    //break;
                default: throw new System.InvalidOperationException();
            }

            string entryPoint = stage == ShaderStages.Vertex ? "VS" : "FS";
            string path = Path.Combine(System.AppContext.BaseDirectory, "Shaders", $"{stage.ToString()}.{extension}");
            byte[] shaderBytes = File.ReadAllBytes(path);
            return _graphicsDevice.ResourceFactory.CreateShader(new ShaderDescription(stage, shaderBytes, entryPoint));
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
            _commandList.SetVertexBuffer(0,_vertexBuffer);
            _commandList.SetIndexBuffer(_indexBuffer,IndexFormat.UInt16);
            _commandList.SetVertexBuffer(1,_xOffsetBuffer);
            _commandList.SetPipeline(_pipeline);
            // Set uniforms
            _commandList.SetGraphicsResourceSet(0,_resourceSet); // Always after SetPipeline
            _commandList.UpdateBuffer(_cameraProjViewBuffer,0,_camera.ViewMatrix);
            _commandList.UpdateBuffer(_cameraProjViewBuffer,64,_camera.ProjectionMatrix);
            // Issue a Draw command for two instances with 4 indices.
            _commandList.DrawIndexed(
                indexCount: 4,
                instanceCount: 2,
                indexStart: 0,
                vertexOffset: 0,
                instanceStart: 0);

            // End() must be called before commands can be submitted for execution.
            _commandList.End();
            _graphicsDevice.SubmitCommands(_commandList);

            // Once commands have been submitted, the rendered image can be presented to the application window.
            _graphicsDevice.SwapBuffers();
        }

        private static void DisposeResources()
        {
            _pipeline.Dispose();
            _vertexShader.Dispose();
            _fragmentShader.Dispose();
            _commandList.Dispose();
            _vertexBuffer.Dispose();
            _indexBuffer.Dispose();
            _graphicsDevice.Dispose();
            _resourceSet.Dispose();
            _resourceLayout.Dispose();
            _xOffsetBuffer.Dispose();
            _cameraProjViewBuffer.Dispose();
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

    public struct VertexPositionColour
    {
        public Vector2 Position; // Position in NDC
        public RgbaFloat Colour;
        public VertexPositionColour(Vector2 position, RgbaFloat colour)
        {
            Position = position;
            Colour = colour;
        }
        // 8 Bytes for Position + 16 Bytes for Colour
        public const uint SizeInBytes = 24;
        
    }
}
