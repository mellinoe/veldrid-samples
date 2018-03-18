﻿using System;
using System.Diagnostics;
using Android.Content;
using Veldrid;

namespace SampleBase.Android
{
    public class AndroidApplicationWindow : ApplicationWindow
    {
        // This is supposed to be a DisposeCollectorResourceFactory but it crashes mono
        private ResourceFactory _disposeFactory;
        private readonly Stopwatch _sw;
        private double _previousSeconds;
        private VeldridSurfaceView _view;

        public event Action<GraphicsDevice, ResourceFactory, Swapchain> GraphicsDeviceCreated;
        public event Action GraphicsDeviceDestroyed;
        public event Action<Swapchain> SwapchainChanged;

        public uint Width => (uint)_view.Width;
        public uint Height => (uint)_view.Height;

        public event Action<float> Rendering;
        public event Action Resized;

        public AndroidApplicationWindow(Context context, VeldridSurfaceView view)
        {
            SetView(view);

            _sw = Stopwatch.StartNew();
        }

        public void SetView(VeldridSurfaceView view)
        {
            _view = view;
            _view.Rendering += OnViewRendering;
            _view.DeviceCreated += OnViewCreatedDevice;
            _view.SwapchainChanged += OnViewSwapchainChanged;
            _view.Resized += OnViewResized;
            _view.DeviceDisposed += OnViewDeviceDisposed;
        }

        private void OnViewDeviceDisposed() => GraphicsDeviceDestroyed?.Invoke();

        private void OnViewSwapchainChanged() => SwapchainChanged?.Invoke(_view.MainSwapchain);

        private void OnViewResized() => Resized?.Invoke();

        private void OnViewCreatedDevice()
        {
            _disposeFactory = _view.GraphicsDevice.ResourceFactory;
            GraphicsDeviceCreated?.Invoke(_view.GraphicsDevice, _disposeFactory, _view.MainSwapchain);
        }

        private void OnViewRendering()
        {
            double newSeconds = _sw.Elapsed.TotalSeconds;
            double deltaSeconds = newSeconds - _previousSeconds;
            _previousSeconds = newSeconds;
            Rendering?.Invoke((float)deltaSeconds);
        }

        public void Run()
        {
            _view.RunContinuousRenderLoop();
        }
    }
}