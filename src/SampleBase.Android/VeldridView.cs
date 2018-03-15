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
    public unsafe class VeldridView : SurfaceView, ISurfaceHolderCallback
    {
        private GraphicsDeviceOptions _deviceOptions;

        public GraphicsDevice GraphicsDevice { get; private set; }

        public event Action Rendering;
        public event Action DeviceCreated;
        public event Action DeviceDisposed;
        public event Action Resized;

        public VeldridView(Context context) : base(context)
        {
            bool debug = false;
#if DEBUG
            debug = true;
#endif
            _deviceOptions = new GraphicsDeviceOptions(debug);
            Holder.AddCallback(this);
        }

        public VeldridView(Context context, GraphicsDeviceOptions deviceOptions) : base(context)
        {
            _deviceOptions = deviceOptions;
            Holder.AddCallback(this);
        }

        public void SurfaceCreated(ISurfaceHolder holder)
        {
            GraphicsDevice = AndroidStartup.CreateOpenGLESGraphicsDevice(
                _deviceOptions,
                holder.Surface.Handle,
                JNIEnv.Handle,
                (uint)Width,
                (uint)Height);
            DeviceCreated?.Invoke();
        }

        public void RunContinuousRenderLoop()
        {
            Task.Factory.StartNew(() => RenderLoop(), TaskCreationOptions.LongRunning);
        }

        public void SurfaceDestroyed(ISurfaceHolder holder)
        {
            GraphicsDevice.Dispose();
            DeviceDisposed?.Invoke();
        }

        public void SurfaceChanged(ISurfaceHolder holder, [GeneratedEnum] Format format, int width, int height)
        {
        }

        private void RenderLoop()
        {
            while (true)
            {
                try
                {
                    Rendering?.Invoke();
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Encountered an error while rendering: " + e);
                    throw;
                }
            }
        }

        protected override void OnSizeChanged(int w, int h, int oldw, int oldh)
        {
            if (GraphicsDevice != null)
            {
                GraphicsDevice.ResizeMainWindow((uint)w, (uint)h);
            }
            Resized?.Invoke();
        }
    }
}