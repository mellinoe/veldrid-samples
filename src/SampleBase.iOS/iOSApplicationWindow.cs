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

		public UIViewApplicationWindow(IntPtr handle) : base(handle)
        {
			_backend = GraphicsBackend.Metal;
			_options = new GraphicsDeviceOptions();
        }

        public uint Width => (uint)View.Frame.Width;
        public uint Height => (uint)View.Frame.Width;

        public event Action<float> Rendering;
        public event Action<GraphicsDevice, ResourceFactory, Swapchain> GraphicsDeviceCreated;
        public event Action<Swapchain> SwapchainChanged;
        public event Action Resized;

        public void Run()
        {
            _timer = CADisplayLink.Create(Render);
            _timer.FrameInterval = 1;
            _timer.AddToRunLoop(NSRunLoop.Main, NSRunLoop.NSDefaultRunLoopMode);
        }

        private void Render()
        {
            float elapsed = (float)(_timer.TargetTimestamp - _timer.Timestamp);
            Rendering?.Invoke(elapsed);
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            if (_backend == GraphicsBackend.Metal)
            {
                _gd = GraphicsDevice.CreateMetal(new GraphicsDeviceOptions(true));
            }
            else if (_backend == GraphicsBackend.Vulkan)
            {
                throw new NotImplementedException();
            }
            //else if (_backend == GraphicsBackend.OpenGLES)
            //{
            //    throw new NotImplementedException();
            //}

            SwapchainSource ss = SwapchainSource.CreateUIView(this.View.Handle);
            _sc = _gd.ResourceFactory.CreateSwapchain(new SwapchainDescription(
                ss,
                (uint)View.Frame.Width,
                (uint)View.Frame.Height,
                PixelFormat.R32_Float,
                false));

            GraphicsDeviceCreated?.Invoke(_gd, _gd.ResourceFactory, _sc);
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