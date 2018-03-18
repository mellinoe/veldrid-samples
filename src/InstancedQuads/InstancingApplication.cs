using System.Numerics;
using Veldrid;
using SampleBase;

namespace InstancedQuads
{
    class InstancingApplication : SampleApplication
    {
        private CommandList _commandList;
        private DeviceBuffer _vertexBuffer;
        private DeviceBuffer _indexBuffer;
        private DeviceBuffer _xOffsetBuffer;
        private DeviceBuffer _cameraProjViewBuffer;
        private Shader _vertexShader;
        private Shader _fragmentShader;
        private Pipeline _pipeline;
        private ResourceSet _resourceSet;
        private ResourceLayout _resourceLayout;

        protected override void CreateResources(ResourceFactory factory)
        {
            _camera.Position = new Vector3(0, 0, 10);
            // Setup Uniform Layout, (Opengl) Veldrid expects a single struct per Uniform
            // One Matrix4x4 = 64 Bytes for floats => 128 for 2
            _cameraProjViewBuffer = factory.CreateBuffer(new BufferDescription(128, BufferUsage.UniformBuffer));

            ResourceLayoutElementDescription resourceLayoutElementDescription = new ResourceLayoutElementDescription("projView", ResourceKind.UniformBuffer, ShaderStages.Vertex);
            ResourceLayoutElementDescription[] resourceLayoutElementDescriptions = { resourceLayoutElementDescription };
            ResourceLayoutDescription resourceLayoutDescription = new ResourceLayoutDescription(resourceLayoutElementDescriptions);
            BindableResource[] bindableResources = new BindableResource[] { _cameraProjViewBuffer };

            _resourceLayout = factory.CreateResourceLayout(resourceLayoutDescription);
            ResourceSetDescription resourceSetDescription = new ResourceSetDescription(_resourceLayout, bindableResources);

            _resourceSet = factory.CreateResourceSet(resourceSetDescription);

            VertexPositionColour[] quadVerticies = {
                new VertexPositionColour(new Vector2(-1.0f,1.0f),RgbaFloat.Red),
                new VertexPositionColour(new Vector2(1.0f,1.0f),RgbaFloat.Green),
                new VertexPositionColour(new Vector2(-1.0f,-1.0f),RgbaFloat.Blue),
                new VertexPositionColour(new Vector2(1.0f,-1.0f),RgbaFloat.Yellow)
            };

            ushort[] quadIndicies = { 0, 1, 2, 3 };

            float[] _xOffset = { -2, 2 };

            // declare (VBO) buffers
            _vertexBuffer
                = factory.CreateBuffer(new BufferDescription(4 * VertexPositionColour.SizeInBytes, BufferUsage.VertexBuffer));
            _indexBuffer
                = factory.CreateBuffer(new BufferDescription(4 * sizeof(ushort), BufferUsage.IndexBuffer));
            _xOffsetBuffer
                = factory.CreateBuffer(new BufferDescription(2 * sizeof(float), BufferUsage.VertexBuffer));

            // fill buffers with data
            _gd.UpdateBuffer(_vertexBuffer, 0, quadVerticies);
            _gd.UpdateBuffer(_indexBuffer, 0, quadIndicies);
            _gd.UpdateBuffer(_xOffsetBuffer, 0, _xOffset);
            _gd.UpdateBuffer(_cameraProjViewBuffer, 0, _camera.ViewMatrix);
            _gd.UpdateBuffer(_cameraProjViewBuffer, 64, _camera.ProjectionMatrix);

            VertexLayoutDescription vertexLayout
                = new VertexLayoutDescription(
                    new VertexElementDescription("Position", VertexElementSemantic.Position, VertexElementFormat.Float2),
                    new VertexElementDescription("Color", VertexElementSemantic.Color, VertexElementFormat.Float4)
                );

            VertexElementDescription vertexElementPerInstance
                = new VertexElementDescription("xOff", VertexElementSemantic.Position, VertexElementFormat.Float1);

            VertexLayoutDescription vertexLayoutPerInstance
                = new VertexLayoutDescription(
                    stride: 4,
                    instanceStepRate: 1,
                    elements: new VertexElementDescription[] { vertexElementPerInstance }
                );

            _vertexShader = Program.LoadShader(_gd, ShaderStages.Vertex);
            _fragmentShader = Program.LoadShader(_gd, ShaderStages.Fragment);

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
                    frontFace: FrontFace.Clockwise,
                    depthClipEnabled: true,
                    scissorTestEnabled: false
                ),
                PrimitiveTopology = PrimitiveTopology.TriangleStrip,
                ResourceLayouts = new ResourceLayout[] { _resourceLayout },
                ShaderSet = new ShaderSetDescription(
                    // The ordering of layouts directly impacts shader layout schemes
                    vertexLayouts: new VertexLayoutDescription[] { vertexLayout, vertexLayoutPerInstance },
                    shaders: new Shader[] { _vertexShader, _fragmentShader }
                ),
                Outputs = _gd.SwapchainFramebuffer.OutputDescription
            };

            _pipeline = factory.CreateGraphicsPipeline(pipelineDescription);

            _commandList = factory.CreateCommandList();

        }

        protected override void Draw(float delta)
        {
            // Begin() must be called before commands can be issued.
            _commandList.Begin();

            // We want to render directly to the output window.
            _commandList.SetFramebuffer(_gd.SwapchainFramebuffer);
            _commandList.SetFullViewports();
            _commandList.ClearColorTarget(0, RgbaFloat.Black);
            _commandList.ClearDepthStencil(1f);
            _commandList.SetPipeline(_pipeline);
            // Set uniforms
            _commandList.SetGraphicsResourceSet(0, _resourceSet); // Always after SetPipeline

            _commandList.UpdateBuffer(_cameraProjViewBuffer, 0, _camera.ViewMatrix);
            _commandList.UpdateBuffer(_cameraProjViewBuffer, 64, _camera.ProjectionMatrix);

            // Set all relevant state to draw our quad.
            _commandList.SetVertexBuffer(0, _vertexBuffer);
            _commandList.SetIndexBuffer(_indexBuffer, IndexFormat.UInt16);
            _commandList.SetVertexBuffer(1, _xOffsetBuffer);

            // Issue a Draw command for two instances with 4 indices.
            _commandList.DrawIndexed(
                indexCount: 4,
                instanceCount: 2,
                indexStart: 0,
                vertexOffset: 0,
                instanceStart: 0);

            // End() must be called before commands can be submitted for execution.
            _commandList.End();
            _gd.SubmitCommands(_commandList);

            // Once commands have been submitted, the rendered image can be presented to the application window.
            _gd.SwapBuffers();
        }
    }
}
