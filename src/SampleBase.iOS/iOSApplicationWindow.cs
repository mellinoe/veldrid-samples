using System;
using UIKit;
using Veldrid;

namespace SampleBase.iOS
{
    public class UIViewApplicationWindow : ApplicationWindow
    {
        private readonly UIView _view;
        private readonly GraphicsDeviceOptions _options;
        private GraphicsDevice _gd;

        public UIViewApplicationWindow(UIView view, GraphicsDeviceOptions options)
        {
            _view = view;
            _options = options;
        }

        public uint Width => (uint)_view.Frame.Width;
        public uint Height => (uint)_view.Frame.Height;

        public event Action<float> Rendering;
        public event Action<GraphicsDevice, ResourceFactory, Swapchain> GraphicsDeviceCreated;
        public event Action<Swapchain> SwapchainChanged;
        public event Action Resized;

        public void Run()
        {
            SwapchainSource scSource = SwapchainSource.CreateUIView(_view.Handle);
            SwapchainDescription scDesc = new SwapchainDescription(
                scSource,
                Width,
                Height,
                _options.SwapchainDepthFormat,
                _options.SyncToVerticalBlank);
            _gd = GraphicsDevice.CreateMetal(_options, scDesc);
            GraphicsDeviceCreated?.Invoke(_gd, _gd.ResourceFactory, _gd.MainSwapchain);
        }
    }
}