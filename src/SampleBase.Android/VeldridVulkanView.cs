using System;
using Android.Content;
using Android.Runtime;
using Veldrid;

using static Veldrid.Android.AndroidRuntime;

namespace SampleBase.Android
{
    public class VeldridVulkanView : VeldridSurfaceView
    {
        private SwapchainSource _swapchainSource;

        public VeldridVulkanView(Context context) : base(context) { }
        public VeldridVulkanView(Context context, GraphicsDeviceOptions deviceOptions) : base(context, deviceOptions) { }

        protected override (GraphicsDevice, Swapchain) CreateGraphicsDevice(
            IntPtr surfaceHandle,
            uint width,
            uint height)
        {
            GraphicsDevice gd = GraphicsDevice.CreateVulkan(DeviceOptions);

            IntPtr aNativeWindow = ANativeWindow_fromSurface(JNIEnv.Handle, surfaceHandle);
            _swapchainSource = SwapchainSource.CreateANativeWindow(aNativeWindow);
            Swapchain sc = CreateSwapchain(gd);

            return (gd, sc);
        }

        private Swapchain CreateSwapchain(GraphicsDevice gd)
        {
            SwapchainDescription swapchainDesc = new SwapchainDescription(
                _swapchainSource,
                (uint)Width, (uint)Height,
                DeviceOptions.SwapchainDepthFormat,
                DeviceOptions.SyncToVerticalBlank);
            Swapchain sc = gd.ResourceFactory.CreateSwapchain(ref swapchainDesc);
            return sc;
        }

        protected override void HandleSizeChanged()
        {
            MainSwapchain.Resize((uint)Width, (uint)Height);
        }

        protected override void HandleSurfaceDestroyed()
        {
            MainSwapchain.Dispose();
            MainSwapchain = null;
            FireSwapchainChangedEvent();
        }

        protected override void HandleSurfaceCreated()
        {
            if (GraphicsDevice == null)
            {
                (GraphicsDevice, Swapchain) gdsc = CreateGraphicsDevice(Holder.Surface.Handle, (uint)Width, (uint)Height);
                GraphicsDevice = gdsc.Item1;
                MainSwapchain = gdsc.Item2;
                FireDeviceCreatedEvent();
            }
            else
            {
                MainSwapchain = CreateSwapchain(GraphicsDevice);
                FireSwapchainChangedEvent();
            }
        }
    }
}