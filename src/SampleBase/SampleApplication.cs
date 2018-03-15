using System.IO;
using Veldrid;

namespace SampleBase
{
    public abstract class SampleApplication
    {
        public ApplicationWindow Window { get; }

        public GraphicsDevice GraphicsDevice { get; private set; }

        public SampleApplication(ApplicationWindow window)
        {
            Window = window;
            Window.Resized += HandleWindowResize;
            Window.GraphicsDeviceCreated += OnGraphicsDeviceCreated;
            Window.Rendering += Draw;
        }

        private void OnGraphicsDeviceCreated(GraphicsDevice gd, ResourceFactory factory)
        {
            GraphicsDevice = gd;
            CreateResources(factory);
        }

        protected virtual string GetTitle() => GetType().Name;

        protected abstract void CreateResources(ResourceFactory factory);

        protected abstract void Draw(float deltaSeconds);

        protected virtual void HandleWindowResize() { }

        public Stream OpenEmbeddedAssetStream(string name) => GetType().Assembly.GetManifestResourceStream(name);

        public Shader LoadShader(ResourceFactory factory, string set, ShaderStages stage, string entryPoint)
        {
            string name = $"{set}-{stage.ToString().ToLower()}.{GetExtension(factory.BackendType)}";
            using (Stream shaderStream = OpenEmbeddedAssetStream(name))
            {
                byte[] bytes = new byte[shaderStream.Length];
                using (MemoryStream ms = new MemoryStream(bytes))
                {
                    shaderStream.CopyTo(ms);
                    return factory.CreateShader(new ShaderDescription(stage, bytes, entryPoint));
                }
            }
        }

        private static string GetExtension(GraphicsBackend backendType)
        {
            return (backendType == GraphicsBackend.Direct3D11)
                ? "hlsl.bytes"
                : (backendType == GraphicsBackend.Vulkan)
                    ? "450.glsl.spv"
                    : (backendType == GraphicsBackend.Metal)
                        ? "metallib"
                        : (backendType == GraphicsBackend.OpenGL)
                            ? "330.glsl"
                            : "300.glsles";
        }
    }
}
