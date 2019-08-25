using CoreAnimation;
using Foundation;
using System;
using UIKit;
using Veldrid;

namespace SampleBase.iOS
{
    public class UIViewApplicationWindow : UIViewController, ApplicationWindow
    {
        private readonly GraphicsDeviceOptions _options;
        private readonly GraphicsBackend _backend;
        private GraphicsDevice _gd;
        private CADisplayLink _timer;
        private Swapchain _sc;
        private bool _viewLoaded;

        public UIViewApplicationWindow(IntPtr handle) : base(handle)
        {
            _backend = GraphicsBackend.Metal;
            _options = new GraphicsDeviceOptions(false, null, false, ResourceBindingModel.Improved);
        }

        public uint Width => (uint)View.Frame.Width;
        public uint Height => (uint)View.Frame.Height;

        public SamplePlatformType PlatformType => SamplePlatformType.Mobile;

        public event Action<float> Rendering;
        public event Action<GraphicsDevice, ResourceFactory, Swapchain> GraphicsDeviceCreated;
        public event Action GraphicsDeviceDestroyed;
        public event Action Resized;
        public event Action<KeyEvent> KeyPressed;

        public void Run(SampleOptions options)
        {
            _timer = CADisplayLink.Create(Render);
            _timer.FrameInterval = 1;
            _timer.AddToRunLoop(NSRunLoop.Main, NSRunLoop.NSDefaultRunLoopMode);
        }

        private void Render()
        {
            if (_viewLoaded)
            {
                float elapsed = (float)(_timer.TargetTimestamp - _timer.Timestamp);
                Rendering?.Invoke(elapsed);
            }
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            SwapchainSource ss = SwapchainSource.CreateUIView(this.View.Handle);
            SwapchainDescription scd = new SwapchainDescription(
                ss,
                (uint)View.Frame.Width,
                (uint)View.Frame.Height,
                PixelFormat.R32_Float,
                false);
            if (_backend == GraphicsBackend.Metal)
            {
                _gd = GraphicsDevice.CreateMetal(_options);
                _sc = _gd.ResourceFactory.CreateSwapchain(ref scd);
            }
            else if (_backend == GraphicsBackend.OpenGLES)
            {
                _gd = GraphicsDevice.CreateOpenGLES(_options, scd);
                _sc = _gd.MainSwapchain;
            }
            else if (_backend == GraphicsBackend.Vulkan)
            {
                throw new NotImplementedException();
            }

            GraphicsDeviceCreated?.Invoke(_gd, _gd.ResourceFactory, _sc);
            _viewLoaded = true;
        }

        // Called whenever view changes orientation or layout is changed
        public override void ViewDidLayoutSubviews()
        {
            base.ViewDidLayoutSubviews();
            _sc.Resize((uint)View.Frame.Width, (uint)View.Frame.Height);
            Resized?.Invoke();
        }
    }
}