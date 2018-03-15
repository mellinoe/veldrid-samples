using System;
using System.Diagnostics;
using Android.Content;
using Veldrid;
using Veldrid.Utilities;

namespace SampleBase.Android
{
    public class AndroidApplicationWindow : VeldridView, ApplicationWindow
    {
        // This is supposed to be a DisposeCollectorResourceFactory but it crashes mono
        private ResourceFactory _disposeFactory;
        private readonly Stopwatch _sw;
        private double _previousSeconds;

        public event Action<GraphicsDevice, ResourceFactory> GraphicsDeviceCreated;

        uint ApplicationWindow.Width => (uint)Width;
        uint ApplicationWindow.Height => (uint)Height;

        private Action<float> _appRendering;

        event Action<float> ApplicationWindow.Rendering
        {
            add => _appRendering += value;
            remove => _appRendering -= value;
        }

        public AndroidApplicationWindow(Context context, GraphicsDeviceOptions options) : base(context, options)
        {
            Rendering += OnViewRendering;
            DeviceCreated += OnViewCreatedDevice;
            _sw = Stopwatch.StartNew();
        }

        private void OnViewCreatedDevice()
        {
            _disposeFactory = GraphicsDevice.ResourceFactory;
            GraphicsDeviceCreated?.Invoke(GraphicsDevice, _disposeFactory);
        }

        private void OnViewRendering()
        {
            double newSeconds = _sw.Elapsed.TotalSeconds;
            double deltaSeconds = newSeconds - _previousSeconds;
            _previousSeconds = newSeconds;
            _appRendering?.Invoke((float)deltaSeconds);
        }

        public void Run()
        {
            RunContinuousRenderLoop();
        }
    }
}