using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Runtime;
using Android.Views;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Veldrid;
using Veldrid.Android;

namespace SampleBase.Android
{
    public class VeldridSurfaceView : SurfaceView, ISurfaceHolderCallback
    {
        private readonly GraphicsBackend _backend;
        protected GraphicsDeviceOptions DeviceOptions { get; }
        private bool _surfaceDestroyed;
        private bool _paused;
        private Action _endOfFrameAction;
        private bool _enabled;
        private bool _needsResize;

        public GraphicsDevice GraphicsDevice { get; protected set; }
        public Swapchain MainSwapchain { get; protected set; }

        public event Action Rendering;
        public event Action DeviceCreated;
        public event Action DeviceDisposed;
        public event Action Resized;
        public event Action SwapchainChanged;

        public VeldridSurfaceView(Context context, GraphicsBackend backend)
            : this(context, backend, new GraphicsDeviceOptions())
        {
        }

        public VeldridSurfaceView(Context context, GraphicsBackend backend, GraphicsDeviceOptions deviceOptions) : base(context)
        {
            if (!(backend == GraphicsBackend.Vulkan || backend == GraphicsBackend.OpenGLES))
            {
                throw new NotSupportedException($"{backend} is not supported on Android.");
            }

            _backend = backend;
            DeviceOptions = deviceOptions;
            Holder.AddCallback(this);
        }

        public void Disable()
        {
            _enabled = false;
        }

        public void RunAtEndOfFrame(Action a) { _endOfFrameAction = a; }

        public void SurfaceCreated(ISurfaceHolder holder)
        {
            bool deviceCreated = false;
            if (_backend == GraphicsBackend.Vulkan)
            {
                if (GraphicsDevice == null)
                {
                    GraphicsDevice = GraphicsDevice.CreateVulkan(DeviceOptions);
                    deviceCreated = true;
                }

                Debug.Assert(MainSwapchain == null);
                IntPtr aNativeWindow = AndroidRuntime.ANativeWindow_fromSurface(JNIEnv.Handle, holder.Surface.Handle);
                SwapchainSource ss = SwapchainSource.CreateANativeWindow(aNativeWindow);
                SwapchainDescription sd = new SwapchainDescription(
                    ss,
                    (uint)Width,
                    (uint)Height,
                    DeviceOptions.SwapchainDepthFormat,
                    DeviceOptions.SyncToVerticalBlank);
                MainSwapchain = GraphicsDevice.ResourceFactory.CreateSwapchain(sd);
            }
            else
            {
                Debug.Assert(GraphicsDevice == null && MainSwapchain == null);
                GraphicsDevice = AndroidStartup.CreateOpenGLESGraphicsDevice(
                    DeviceOptions,
                    holder.Surface.Handle,
                    JNIEnv.Handle,
                    (uint)Width,
                    (uint)Height);
                MainSwapchain = GraphicsDevice.MainSwapchain;
                deviceCreated = true;
            }

            if (deviceCreated)
            {
                DeviceCreated?.Invoke();
            }
            SwapchainChanged?.Invoke();
        }

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
            _needsResize = true;
        }

        private void RenderLoop()
        {
            _enabled = true;
            while (_enabled)
            {
                try
                {
                    if (_paused) { continue; }

                    if (_needsResize)
                    {
                        _needsResize = false;
                        MainSwapchain.Resize((uint)Width, (uint)Height);
                        Resized?.Invoke();
                    }

                    if (GraphicsDevice != null)
                    {
                        Rendering?.Invoke();
                    }

                    if (_surfaceDestroyed)
                    {
                        HandleSurfaceDestroyed();
                    }

                    _endOfFrameAction?.Invoke();
                    _endOfFrameAction = null;
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Encountered an error while rendering: " + e);
                    throw;
                }
            }
        }

        private void HandleSurfaceDestroyed()
        {
            if (_backend == GraphicsBackend.Vulkan)
            {
                MainSwapchain.Dispose();
                MainSwapchain = null;
                SwapchainChanged?.Invoke();
            }
            else
            {
                GraphicsDevice.Dispose();
                GraphicsDevice = null;
                MainSwapchain = null;
                DeviceDisposed?.Invoke();
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
    }
}