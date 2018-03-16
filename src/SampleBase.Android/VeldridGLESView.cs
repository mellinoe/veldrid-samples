using Android.Content;
using Android.Runtime;
using System;
using Veldrid;
using Veldrid.Android;

namespace SampleBase.Android
{
    public unsafe class VeldridGLESView : VeldridSurfaceView
    {
        public VeldridGLESView(Context context) : base(context)
        {
        }

        public VeldridGLESView(Context context, GraphicsDeviceOptions deviceOptions) : base(context, deviceOptions)
        {
        }

        protected override (GraphicsDevice, Swapchain) CreateGraphicsDevice(
            IntPtr surfaceHandle,
            uint width,
            uint height)
        {
            GraphicsDevice gd = AndroidStartup.CreateOpenGLESGraphicsDevice(
                DeviceOptions,
                surfaceHandle,
                JNIEnv.Handle,
                (uint)Width,
                (uint)Height);

            return (gd, gd.MainSwapchain);
        }

        protected override void HandleSurfaceCreated()
        {
            (GraphicsDevice GD, Swapchain SC) gdsc = CreateGraphicsDevice(
                Holder.Surface.Handle,
                (uint)Width,
                (uint)Height);
            GraphicsDevice = gdsc.GD;
            MainSwapchain = gdsc.SC;

            FireDeviceCreatedEvent();
            FireSwapchainChangedEvent();
        }

        protected override void HandleSizeChanged()
        {
            GraphicsDevice.ResizeMainWindow((uint)Width, (uint)Height);
            FireResizedEvent();
        }

        protected override void HandleSurfaceDestroyed()
        {
            GraphicsDevice.Dispose();
            GraphicsDevice = null;
            MainSwapchain = null;
            FireGraphicsDeviceDisposedEvent();
            FireSwapchainChangedEvent();
        }
    }
}
