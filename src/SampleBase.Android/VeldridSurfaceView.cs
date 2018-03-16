using Android.Content;
using Android.Graphics;
using Android.Runtime;
using Android.Views;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Veldrid;

namespace SampleBase.Android
{
    public abstract class VeldridSurfaceView : SurfaceView, ISurfaceHolderCallback
    {
        protected GraphicsDeviceOptions DeviceOptions { get; }
        private bool _surfaceDestroyed;
        private bool _sizeChanged;
        private bool _paused;

        public GraphicsDevice GraphicsDevice { get; protected set; }
        public Swapchain MainSwapchain { get; protected set; }

        public event Action Rendering;
        public event Action DeviceCreated;
        public event Action DeviceDisposed;
        public event Action Resized;
        public event Action SwapchainChanged;

        public VeldridSurfaceView(Context context) : base(context)
        {
            bool debug = false;
#if DEBUG
            debug = true;
#endif
            DeviceOptions = new GraphicsDeviceOptions(debug);
            Holder.AddCallback(this);
        }

        public VeldridSurfaceView(Context context, GraphicsDeviceOptions deviceOptions) : base(context)
        {
            DeviceOptions = deviceOptions;
            Holder.AddCallback(this);
        }

        public void SurfaceCreated(ISurfaceHolder holder)
        {
            HandleSurfaceCreated();
        }

        protected abstract (GraphicsDevice, Swapchain) CreateGraphicsDevice(
            IntPtr surfaceHandle,
            uint width,
            uint height);

        public void RunContinuousRenderLoop()
        {
            Task.Factory.StartNew(() => RenderLoop(), TaskCreationOptions.LongRunning);
        }

        public void SurfaceDestroyed(ISurfaceHolder holder)
        {
            _surfaceDestroyed = true;
        }

        public void SurfaceChanged(ISurfaceHolder holder, [GeneratedEnum] Format format, int width, int height)
        {
            _sizeChanged = true;
        }

        private void RenderLoop()
        {
            while (true)
            {
                try
                {
                    if (_paused)
                    {
                        continue;
                    }

                    if (_surfaceDestroyed)
                    {
                        _surfaceDestroyed = false;
                        _sizeChanged = false;
                        HandleSurfaceDestroyed();
                    }
                    else if (_sizeChanged)
                    {
                        _sizeChanged = false;
                        HandleSizeChanged();
                    }
                    else if (GraphicsDevice != null)
                    {
                        Rendering?.Invoke();
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Encountered an error while rendering: " + e);
                    throw;
                }
            }
        }

        protected abstract void HandleSizeChanged();

        protected abstract void HandleSurfaceDestroyed();
        protected abstract void HandleSurfaceCreated();

        protected override void OnSizeChanged(int w, int h, int oldw, int oldh)
        {
            if (GraphicsDevice != null)
            {
                _sizeChanged = true;
            }
        }

        public void OnPause()
        {
            _paused = true;
        }

        public void OnResume()
        {
            _paused = false;
        }

        protected void FireDeviceCreatedEvent() => DeviceCreated?.Invoke();
        protected void FireResizedEvent() => Resized?.Invoke();
        protected void FireGraphicsDeviceDisposedEvent() => DeviceDisposed?.Invoke();
        protected void FireSwapchainChangedEvent() => SwapchainChanged?.Invoke();
    }
}