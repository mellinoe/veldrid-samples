using System;
using System.Diagnostics;
using System.IO;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;
using Veldrid.Utilities;

namespace SampleBase
{
    public class VeldridStartupWindow : ApplicationWindow
    {
        private readonly Sdl2Window _window;
        private GraphicsDevice _gd;
        private DisposeCollectorResourceFactory _factory;
        private bool _windowResized = true;

        public event Action<float> Rendering;
        public event Action<GraphicsDevice, ResourceFactory, Swapchain> GraphicsDeviceCreated;
        public event Action<Swapchain> SwapchainChanged;
        public event Action Resized;

        public uint Width => (uint)_window.Width;
        public uint Height => (uint)_window.Height;

        public VeldridStartupWindow(string title)
        {
            WindowCreateInfo wci = new WindowCreateInfo
            {
                X = 100,
                Y = 100,
                WindowWidth = 960,
                WindowHeight = 540,
                WindowTitle = title,
            };
            _window = VeldridStartup.CreateWindow(ref wci);
            _window.Resized += () =>
            {
                _windowResized = true;
            };
            _window.MouseMove += OnMouseMove;
            _window.KeyDown += OnKeyDown;
        }

        public void Run()
        {
            GraphicsDeviceOptions options = new GraphicsDeviceOptions(false, PixelFormat.R16_UNorm, true);
#if DEBUG
            options.Debug = true;
#endif
            _gd = VeldridStartup.CreateGraphicsDevice(_window, options);
            _factory = new DisposeCollectorResourceFactory(_gd.ResourceFactory);
            GraphicsDeviceCreated?.Invoke(_gd, _factory, _gd.MainSwapchain);

            Stopwatch sw = Stopwatch.StartNew();
            double previousElapsed = sw.Elapsed.TotalSeconds;

            while (_window.Exists)
            {
                double newElapsed = sw.Elapsed.TotalSeconds;
                float deltaSeconds = (float)(newElapsed - previousElapsed);

                InputSnapshot inputSnapshot = _window.PumpEvents();
                InputTracker.UpdateFrameInput(inputSnapshot);

                if (_window.Exists)
                {
                    previousElapsed = newElapsed;
                    if (_windowResized)
                    {
                        _gd.ResizeMainWindow((uint)_window.Width, (uint)_window.Height);
                        Resized?.Invoke();
                    }

                    Rendering?.Invoke(deltaSeconds);
                }
            }

            _gd.WaitForIdle();
            _factory.DisposeCollector.DisposeAll();
            _gd.Dispose();
        }

        protected virtual void OnMouseMove(MouseMoveEventArgs mouseMoveEvent)
        {
        }

        protected virtual void OnKeyDown(KeyEvent keyEvent)
        {
        }
    }
}
