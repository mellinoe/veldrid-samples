using System;
using Veldrid;

namespace SampleBase
{
    public interface ApplicationWindow
    {
        SamplePlatformType PlatformType { get; }

        event Action<float> Rendering;
        event Action<GraphicsDevice, ResourceFactory, Swapchain> GraphicsDeviceCreated;
        event Action GraphicsDeviceDestroyed;
        event Action Resized;
        event Action<KeyEvent> KeyPressed;
        event Action<InputSnapshot> SnapshotReceived;

        uint Width { get; }
        uint Height { get; }

        void Run();
    }
}
