using System;
using System.Diagnostics;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using SampleBase;
using Veldrid;
using Veldrid.Utilities;

namespace TexturedCube.UWP
{
    public sealed partial class VeldridControl : SwapChainPanel, ApplicationWindow
    {
        private GraphicsDevice _graphicsDevice;
        private readonly Stopwatch _stopwatch = new Stopwatch();
        private DisposeCollectorResourceFactory _resources;

        public VeldridControl()
        {
            InitializeComponent();

            CompositionScaleChanged += OnPanelScaleChanged;
            SizeChanged += OnPanelSizeChanged;

            CompositionTarget.Rendering += RenderPanel;

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        public event Action<float> Rendering;
        public event Action<GraphicsDevice, ResourceFactory, Swapchain> GraphicsDeviceCreated;
        public event Action GraphicsDeviceDestroyed;
        public event Action Resized;
        public event Action<KeyEvent> KeyPressed;

        public SamplePlatformType PlatformType => SamplePlatformType.Desktop;
        uint ApplicationWindow.Width => (uint) (RenderSize.Width * CompositionScaleX);
        uint ApplicationWindow.Height => (uint) (RenderSize.Height * CompositionScaleY);

        public void Run()
        {
            throw new NotImplementedException();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            DestroyDevice();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            CreateDevice();
        }


        private void DestroyDevice()
        {
            if (_graphicsDevice != null)
            {
                GraphicsDeviceDestroyed?.Invoke();
                _graphicsDevice.WaitForIdle();
                _resources.DisposeCollector.DisposeAll();
                _graphicsDevice.Dispose();
                _graphicsDevice = null;
            }
        }

        private void CreateDevice()
        {
            var options = new GraphicsDeviceOptions(true, PixelFormat.R32_Float, true, ResourceBindingModel.Improved);
            var logicalDpi = 96.0f * CompositionScaleX;
            var renderWidth = RenderSize.Width;
            var renderHeight = RenderSize.Height;
            _graphicsDevice = GraphicsDevice.CreateD3D11(options, this, renderWidth, renderHeight, logicalDpi);
            _resources = new DisposeCollectorResourceFactory(_graphicsDevice.ResourceFactory);
            GraphicsDeviceCreated?.Invoke(_graphicsDevice, _resources, _graphicsDevice.MainSwapchain);
        }

        private void RenderPanel(object sender, object e)
        {
            var seconds = (float) _stopwatch.Elapsed.TotalSeconds;
            _stopwatch.Restart();
            Rendering?.Invoke(seconds);
        }

        private void OnPanelSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_graphicsDevice != null) Resized?.Invoke();
        }

        private void OnPanelScaleChanged(SwapChainPanel sender, object args)
        {
            if (_graphicsDevice != null) Resized?.Invoke();
        }
    }
}