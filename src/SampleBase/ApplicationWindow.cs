using System;
using Veldrid;

namespace SampleBase
{
    public interface ApplicationWindow
    {
        event Action<float> Rendering;
        event Action<GraphicsDevice, ResourceFactory> GraphicsDeviceCreated;
        event Action Resized;

        uint Width { get; }
        uint Height { get; }

        void Run();
    }
}
