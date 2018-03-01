using System.IO;
using System.Numerics;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;

namespace InstancedQuads
{
    class Program
    {

        static void Main(string[] args)
        {
            new InstancingApplication().Run();
        }


        public static Shader LoadShader(GraphicsDevice gd, ShaderStages stage)
        {
            string extension = null;
            switch (gd.BackendType)
            {
                case GraphicsBackend.Direct3D11:
                    extension = "hlsl.bytes";
                    // not implemented
                    throw new System.NotImplementedException();
                    //break;
                case GraphicsBackend.Vulkan:
                    extension = "spv";
                    // not implemented
                    throw new System.NotImplementedException();
                    //break;
                case GraphicsBackend.OpenGL:
                    extension = "glsl";
                    break;
                case GraphicsBackend.Metal:
                    extension = "metallib";
                    break;
                default: throw new System.InvalidOperationException();
            }

            string entryPoint = stage == ShaderStages.Vertex ? "VS" : "FS";
            string path = Path.Combine(System.AppContext.BaseDirectory, "Shaders", $"{stage.ToString()}.{extension}");
            byte[] shaderBytes = File.ReadAllBytes(path);
            return gd.ResourceFactory.CreateShader(new ShaderDescription(stage, shaderBytes, entryPoint));
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
