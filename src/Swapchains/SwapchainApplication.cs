using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using ImGuiNET;
using SampleBase;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;

namespace Swapchains
{
    public class SwapchainApplication : SampleApplication
    {
        private readonly List<ExtraWindow> _extraWindows = new List<ExtraWindow>();
        private CommandList _cl;
        private ImGuiRenderer _imguiRenderer;
        private int _id;
        private float _deltaTime;
        private Random _random = new Random();

        public SwapchainApplication(ApplicationWindow window) : base(window)
        {
        }

        protected override void CreateResources(ResourceFactory factory)
        {
            _cl = factory.CreateCommandList();
            _imguiRenderer = new ImGuiRenderer(
                GraphicsDevice,
                GraphicsDevice.MainSwapchain.Framebuffer.OutputDescription,
                (int)Window.Width, (int)Window.Height);
        }

        protected override void ProcessInputs(InputSnapshot snapshot) => _imguiRenderer.Update(_deltaTime, snapshot);

        protected override void Draw(float deltaSeconds)
        {
            _deltaTime = deltaSeconds;
            DrawGui();

            _cl.Begin();
            ProcessOtherWindows();
            _cl.SetFramebuffer(GraphicsDevice.MainSwapchain.Framebuffer);
            _cl.ClearColorTarget(0, new RgbaFloat(0f, 0f, 0.2f, 1f));
            _imguiRenderer.Render(GraphicsDevice, _cl);
            _cl.End();
            GraphicsDevice.SubmitCommands(_cl);
            GraphicsDevice.SwapBuffers(GraphicsDevice.MainSwapchain);

            foreach (ExtraWindow window in _extraWindows) { GraphicsDevice.SwapBuffers(window.Swapchain); }
        }

        private void DrawGui()
        {
            if (ImGui.Button("Create new window"))
            {
                RgbaFloat randomColor = new RgbaFloat(
                    (float)_random.NextDouble(),
                    (float)_random.NextDouble(),
                    (float)_random.NextDouble(),
                    1);
                _extraWindows.Add(new ExtraWindow(GraphicsDevice, ResourceFactory, $"Window{_id++}", randomColor));
            }

            foreach (ExtraWindow window in _extraWindows)
            {
                DrawWindowControls(window);
            }
        }

        private void DrawWindowControls(ExtraWindow window)
        {
            Vector3 color3 = new Vector3(window.Color.R, window.Color.G, window.Color.B);
            if (ImGui.ColorEdit3($"Clear Color##{window.Name}", ref color3))
            {
                window.Color = new RgbaFloat(color3.X, color3.Y, color3.Z, 1);
            }
        }

        private void ProcessOtherWindows()
        {
            for (int i = 0; i < _extraWindows.Count; i++)
            {
                ExtraWindow window = _extraWindows[i];
                if (!window.Exists)
                {
                    _extraWindows.RemoveAt(i);
                    i -= 1;
                }

                window.ProcessEvents();
                _cl.SetFramebuffer(window.Swapchain.Framebuffer);
                _cl.ClearColorTarget(0, window.Color);
            }
        }
    }

    public class ExtraWindow
    {
        private readonly Sdl2Window _window;
        public Swapchain Swapchain { get; }
        public string Name { get; }
        public RgbaFloat Color { get; set; }
        public bool Exists => _window.Exists;

        public ExtraWindow(GraphicsDevice gd, ResourceFactory rf, string name, RgbaFloat color)
        {
            _window = VeldridStartup.CreateWindow(new WindowCreateInfo(400, 400, 300, 175, WindowState.Normal, name));
            SwapchainSource ss = VeldridStartup.GetSwapchainSource(_window);
            Swapchain = rf.CreateSwapchain(new SwapchainDescription(
                ss, (uint)_window.Width, (uint)_window.Height, null, false));
            Name = name;
            Color = color;
        }

        public void ProcessEvents()
        {
            InputSnapshot snapshot = _window.PumpEvents();
        }
    }
}
