using Veldrid;
using Veldrid.StartupUtilities;
using ImGuiNET;
using Veldrid.Sdl2;

namespace ImGuiSample
{
    internal static class Program
    {
        static void Main(string[] args)
        {
            VeldridStartup.CreateWindowAndGraphicsDevice(
                new WindowCreateInfo(50, 50, 960, 540, WindowState.Normal, "ImGui Test"),
                out Sdl2Window window,
                out GraphicsDevice gd);

            CommandList cl = gd.ResourceFactory.CreateCommandList();

            ImGuiRenderer imguiRenderer = new ImGuiRenderer(
                gd,
                gd.MainSwapchain.Framebuffer.OutputDescription,
                window.Width,
                window.Height);

            window.Resized += () =>
            {
                imguiRenderer.WindowResized(window.Width, window.Height);
                gd.MainSwapchain.Resize((uint)window.Width, (uint)window.Height);
            };

            while (window.Exists)
            {
                InputSnapshot snapshot = window.PumpEvents();
                imguiRenderer.Update(1f / 60f, snapshot);

                // Draw whatever you want here.
                if (ImGui.BeginWindow("Test Window"))
                {
                    ImGui.Text("Hello");
                    if (ImGui.Button("Quit"))
                    {
                        window.Close();
                    }
                }
                ImGui.Button("What");
                ImGui.EndWindow();

                cl.Begin();
                cl.SetFramebuffer(gd.MainSwapchain.Framebuffer);
                cl.ClearColorTarget(0, new RgbaFloat(0, 0, 0.2f, 1f));
                imguiRenderer.Render(gd, cl);
                cl.End();
                gd.SubmitCommands(cl);
                gd.SwapBuffers(gd.MainSwapchain);
            }
        }
    }
}
