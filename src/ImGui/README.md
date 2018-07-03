# ImGui

This is a sample project demonstrating the usage of Veldrid.ImGui, a utility library that allows the output of ImGui.NET to be easily rendered with Veldrid.

There are a few things that need to be taken care of to ensure proper usage:

1) __Creating an ImGuiRenderer instance__: The constructor takes a few pieces of information:
  * Your application's GraphicsDevice.
  * The OutputDescription of the Framebuffer that your UI will be rendered onto. In many cases, this will simply be the OutputDescription of your application's main Swapchain (GraphicsDevice.MainSwapchain.Framebuffer.OutputDescription). If you want to render your UI to a separate Framebuffer, you should provide its OutputDescription instead.
  * The initial width and height of the output Texture. This might be the dimensions of your application window, or the dimensions of your UI Framebuffer.
    * NOTE: Whenever your window or UI Framebuffer's size changes, you need to call ImGuiRenderer.WindowResized(), passing in the new dimensions.
2) __Calling ImGuiRenderer.Update__. Every frame, an ImGuiRenderer needs a new InputSnapshot, which you get from your window's message pump. This tells ImGui where the mouse is, which buttons are pressed, which keys were entered, etc. You must call this function once per frame.
3) __Calling ImGuiRenderer.Render__. Every frame, you must call Render. This function takes your GraphicsDevice, as well as an active CommandList to record drawing commands into.

In between steps 2) and 3), you should be submitting the actual ImGui.NET function calls that define your UI.
