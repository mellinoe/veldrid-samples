using System;
using Veldrid;

namespace SampleBase
{
    public interface ApplicationWindow
    {
        event Action<float> Rendering;
        event Action<GraphicsDevice, ResourceFactory, Swapchain> GraphicsDeviceCreated;
        event Action GraphicsDeviceDestroyed;
        event Action<Swapchain> SwapchainChanged;
        event Action Resized;

        uint Width { get; }
        uint Height { get; }

        void Run();
    }
}
