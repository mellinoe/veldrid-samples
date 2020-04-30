using System;
using System.Diagnostics;
using System.Windows.Forms;
using SampleBase;
using Veldrid;
using Veldrid.Utilities;
using Veldrid.Vk;

namespace TexturedCube
{
    public class VeldridControl : Control, ApplicationWindow
    {
        private readonly Stopwatch _stopwatch = new Stopwatch();
        private GraphicsDevice _device;
        private DisposeCollectorResourceFactory _resources;
        private bool _isAnimated;

        public VeldridControl()
        {
            this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            this.SetStyle(ControlStyles.DoubleBuffer, false);
            this.SetStyle(ControlStyles.Opaque, true);
            this.SetStyle(ControlStyles.ResizeRedraw, true);
            this.SetStyle(ControlStyles.UserPaint, true);
        }

        public event Action<float> Rendering;
        public event Action<GraphicsDevice, ResourceFactory, Swapchain> GraphicsDeviceCreated;
        public event Action GraphicsDeviceDestroyed;
        public event Action Resized;
        public event Action<KeyEvent> KeyPressed;

        public bool IsAnimated
        {
            get => _isAnimated;
            set
            {
                if (_isAnimated != value)
                {
                    _isAnimated = value;
                    if (_isAnimated)
                    {
                        Application.Idle += OnIdle;
                    }
                    else
                    {
                        Application.Idle -= OnIdle;
                    }
                }
            }
        }

        public SamplePlatformType PlatformType => SamplePlatformType.Desktop;
        uint ApplicationWindow.Width => (uint) Width;
        uint ApplicationWindow.Height => (uint) Height;

        public void Run()
        {
            throw new NotImplementedException();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            var width = (uint) Math.Max(Width, 1);
            var height = (uint) Math.Max(Height, 1);
            _device = GraphicsDevice.CreateD3D11(new GraphicsDeviceOptions(true, PixelFormat.R32_Float, false), Handle, width, height);
            //_device = GraphicsDevice.CreateVulkan(new GraphicsDeviceOptions(false, PixelFormat.R32_Float, false) {PreferStandardClipSpaceYDirection = true}, VkSurfaceSource.CreateWin32(Handle, Handle), width, height);
            _resources = new DisposeCollectorResourceFactory(_device.ResourceFactory);

            GraphicsDeviceCreated?.Invoke(_device, _resources, _device.MainSwapchain);
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            if (_device != null)
            {
                GraphicsDeviceDestroyed?.Invoke();
                _device.WaitForIdle();
                _resources.DisposeCollector.DisposeAll();
                _device.Dispose();
                _device = null;
            }

            base.OnHandleDestroyed(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Render();
        }

        private void Render()
        {
            var elapsedTotalSeconds = (float) _stopwatch.Elapsed.TotalSeconds;
            if (!_stopwatch.IsRunning)
                _stopwatch.Start();
            else
                _stopwatch.Restart();
            if (_device != null)
            {
                Rendering?.Invoke(elapsedTotalSeconds);
            }
        }

        protected override void OnResize(EventArgs e)
        {
            _device?.ResizeMainWindow((uint) Width, (uint) Height);
            Resized?.Invoke();
            Invalidate();
            base.OnResize(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (KeyPressed == null)
                return;

            switch (e.KeyCode)
            {
                //case System.Windows.Forms.Keys.LButton: KeyPressed(new KeyEvent(Veldrid.Key.LButton, true, GetKeyModifiers(e))); break;
                //case System.Windows.Forms.Keys.RButton: KeyPressed(new KeyEvent(Veldrid.Key.RButton, true, GetKeyModifiers(e))); break;
                //case System.Windows.Forms.Keys.Cancel: KeyPressed(new KeyEvent(Veldrid.Key.Cancel, true, GetKeyModifiers(e))); break;
                //case System.Windows.Forms.Keys.MButton: KeyPressed(new KeyEvent(Veldrid.Key.MButton, true, GetKeyModifiers(e))); break;
                //case System.Windows.Forms.Keys.XButton1: KeyPressed(new KeyEvent(Veldrid.Key.XButton1, true, GetKeyModifiers(e))); break;
                //case System.Windows.Forms.Keys.XButton2: KeyPressed(new KeyEvent(Veldrid.Key.XButton2, true, GetKeyModifiers(e))); break;
                case Keys.Back:
                    KeyPressed(new KeyEvent(Key.Back, true, GetKeyModifiers(e)));
                    break;
                case Keys.Tab:
                    KeyPressed(new KeyEvent(Key.Tab, true, GetKeyModifiers(e)));
                    break;
                //case System.Windows.Forms.Keys.LineFeed: KeyPressed(new KeyEvent(Veldrid.Key.LineFeed, true, GetKeyModifiers(e))); break;
                case Keys.Clear:
                    KeyPressed(new KeyEvent(Key.Clear, true, GetKeyModifiers(e)));
                    break;
                case Keys.Enter:
                    KeyPressed(new KeyEvent(Key.Enter, true, GetKeyModifiers(e)));
                    break;
                case Keys.ShiftKey:
                    KeyPressed(new KeyEvent(Key.ShiftLeft, true, GetKeyModifiers(e)));
                    break;
                case Keys.ControlKey:
                    KeyPressed(new KeyEvent(Key.ControlLeft, true, GetKeyModifiers(e)));
                    break;
                case Keys.Menu:
                    KeyPressed(new KeyEvent(Key.Menu, true, GetKeyModifiers(e)));
                    break;
                case Keys.Pause:
                    KeyPressed(new KeyEvent(Key.Pause, true, GetKeyModifiers(e)));
                    break;
                case Keys.CapsLock:
                    KeyPressed(new KeyEvent(Key.CapsLock, true, GetKeyModifiers(e)));
                    break;
                case Keys.Escape:
                    KeyPressed(new KeyEvent(Key.Escape, true, GetKeyModifiers(e)));
                    break;
                case Keys.Space:
                    KeyPressed(new KeyEvent(Key.Space, true, GetKeyModifiers(e)));
                    break;
                case Keys.PageUp:
                    KeyPressed(new KeyEvent(Key.PageUp, true, GetKeyModifiers(e)));
                    break;
                case Keys.PageDown:
                    KeyPressed(new KeyEvent(Key.PageDown, true, GetKeyModifiers(e)));
                    break;
                case Keys.End:
                    KeyPressed(new KeyEvent(Key.End, true, GetKeyModifiers(e)));
                    break;
                case Keys.Home:
                    KeyPressed(new KeyEvent(Key.Home, true, GetKeyModifiers(e)));
                    break;
                case Keys.Left:
                    KeyPressed(new KeyEvent(Key.Left, true, GetKeyModifiers(e)));
                    break;
                case Keys.Up:
                    KeyPressed(new KeyEvent(Key.Up, true, GetKeyModifiers(e)));
                    break;
                case Keys.Right:
                    KeyPressed(new KeyEvent(Key.Right, true, GetKeyModifiers(e)));
                    break;
                case Keys.Down:
                    KeyPressed(new KeyEvent(Key.Down, true, GetKeyModifiers(e)));
                    break;
                //case System.Windows.Forms.Keys.Select: KeyPressed(new KeyEvent(Veldrid.Key.Select, true, GetKeyModifiers(e))); break;
                //case System.Windows.Forms.Keys.Print: KeyPressed(new KeyEvent(Veldrid.Key.Print, true, GetKeyModifiers(e))); break;
                //case System.Windows.Forms.Keys.Execute: KeyPressed(new KeyEvent(Veldrid.Key.Execute, true, GetKeyModifiers(e))); break;
                case Keys.PrintScreen:
                    KeyPressed(new KeyEvent(Key.PrintScreen, true, GetKeyModifiers(e)));
                    break;
                case Keys.Insert:
                    KeyPressed(new KeyEvent(Key.Insert, true, GetKeyModifiers(e)));
                    break;
                case Keys.Delete:
                    KeyPressed(new KeyEvent(Key.Delete, true, GetKeyModifiers(e)));
                    break;
                //case System.Windows.Forms.Keys.Help: KeyPressed(new KeyEvent(Veldrid.Key.Help, true, GetKeyModifiers(e))); break;
                case Keys.D0:
                    KeyPressed(new KeyEvent(Key.Number0, true, GetKeyModifiers(e)));
                    break;
                case Keys.D1:
                    KeyPressed(new KeyEvent(Key.Number1, true, GetKeyModifiers(e)));
                    break;
                case Keys.D2:
                    KeyPressed(new KeyEvent(Key.Number2, true, GetKeyModifiers(e)));
                    break;
                case Keys.D3:
                    KeyPressed(new KeyEvent(Key.Number3, true, GetKeyModifiers(e)));
                    break;
                case Keys.D4:
                    KeyPressed(new KeyEvent(Key.Number4, true, GetKeyModifiers(e)));
                    break;
                case Keys.D5:
                    KeyPressed(new KeyEvent(Key.Number5, true, GetKeyModifiers(e)));
                    break;
                case Keys.D6:
                    KeyPressed(new KeyEvent(Key.Number6, true, GetKeyModifiers(e)));
                    break;
                case Keys.D7:
                    KeyPressed(new KeyEvent(Key.Number7, true, GetKeyModifiers(e)));
                    break;
                case Keys.D8:
                    KeyPressed(new KeyEvent(Key.Number8, true, GetKeyModifiers(e)));
                    break;
                case Keys.D9:
                    KeyPressed(new KeyEvent(Key.Number9, true, GetKeyModifiers(e)));
                    break;
                case Keys.A:
                    KeyPressed(new KeyEvent(Key.A, true, GetKeyModifiers(e)));
                    break;
                case Keys.B:
                    KeyPressed(new KeyEvent(Key.B, true, GetKeyModifiers(e)));
                    break;
                case Keys.C:
                    KeyPressed(new KeyEvent(Key.C, true, GetKeyModifiers(e)));
                    break;
                case Keys.D:
                    KeyPressed(new KeyEvent(Key.D, true, GetKeyModifiers(e)));
                    break;
                case Keys.E:
                    KeyPressed(new KeyEvent(Key.E, true, GetKeyModifiers(e)));
                    break;
                case Keys.F:
                    KeyPressed(new KeyEvent(Key.F, true, GetKeyModifiers(e)));
                    break;
                case Keys.G:
                    KeyPressed(new KeyEvent(Key.G, true, GetKeyModifiers(e)));
                    break;
                case Keys.H:
                    KeyPressed(new KeyEvent(Key.H, true, GetKeyModifiers(e)));
                    break;
                case Keys.I:
                    KeyPressed(new KeyEvent(Key.I, true, GetKeyModifiers(e)));
                    break;
                case Keys.J:
                    KeyPressed(new KeyEvent(Key.J, true, GetKeyModifiers(e)));
                    break;
                case Keys.K:
                    KeyPressed(new KeyEvent(Key.K, true, GetKeyModifiers(e)));
                    break;
                case Keys.L:
                    KeyPressed(new KeyEvent(Key.L, true, GetKeyModifiers(e)));
                    break;
                case Keys.M:
                    KeyPressed(new KeyEvent(Key.M, true, GetKeyModifiers(e)));
                    break;
                case Keys.N:
                    KeyPressed(new KeyEvent(Key.N, true, GetKeyModifiers(e)));
                    break;
                case Keys.O:
                    KeyPressed(new KeyEvent(Key.O, true, GetKeyModifiers(e)));
                    break;
                case Keys.P:
                    KeyPressed(new KeyEvent(Key.P, true, GetKeyModifiers(e)));
                    break;
                case Keys.Q:
                    KeyPressed(new KeyEvent(Key.Q, true, GetKeyModifiers(e)));
                    break;
                case Keys.R:
                    KeyPressed(new KeyEvent(Key.R, true, GetKeyModifiers(e)));
                    break;
                case Keys.S:
                    KeyPressed(new KeyEvent(Key.S, true, GetKeyModifiers(e)));
                    break;
                case Keys.T:
                    KeyPressed(new KeyEvent(Key.T, true, GetKeyModifiers(e)));
                    break;
                case Keys.U:
                    KeyPressed(new KeyEvent(Key.U, true, GetKeyModifiers(e)));
                    break;
                case Keys.V:
                    KeyPressed(new KeyEvent(Key.V, true, GetKeyModifiers(e)));
                    break;
                case Keys.W:
                    KeyPressed(new KeyEvent(Key.W, true, GetKeyModifiers(e)));
                    break;
                case Keys.X:
                    KeyPressed(new KeyEvent(Key.X, true, GetKeyModifiers(e)));
                    break;
                case Keys.Y:
                    KeyPressed(new KeyEvent(Key.Y, true, GetKeyModifiers(e)));
                    break;
                case Keys.Z:
                    KeyPressed(new KeyEvent(Key.Z, true, GetKeyModifiers(e)));
                    break;
                case Keys.LWin:
                    KeyPressed(new KeyEvent(Key.LWin, true, GetKeyModifiers(e)));
                    break;
                case Keys.RWin:
                    KeyPressed(new KeyEvent(Key.RWin, true, GetKeyModifiers(e)));
                    break;
                //case System.Windows.Forms.Keys.Apps: KeyPressed(new KeyEvent(Veldrid.Key.Apps, true, GetKeyModifiers(e))); break;
                case Keys.Sleep:
                    KeyPressed(new KeyEvent(Key.Sleep, true, GetKeyModifiers(e)));
                    break;
                case Keys.NumPad0:
                    KeyPressed(new KeyEvent(Key.Keypad0, true, GetKeyModifiers(e)));
                    break;
                case Keys.NumPad1:
                    KeyPressed(new KeyEvent(Key.Keypad1, true, GetKeyModifiers(e)));
                    break;
                case Keys.NumPad2:
                    KeyPressed(new KeyEvent(Key.Keypad2, true, GetKeyModifiers(e)));
                    break;
                case Keys.NumPad3:
                    KeyPressed(new KeyEvent(Key.Keypad3, true, GetKeyModifiers(e)));
                    break;
                case Keys.NumPad4:
                    KeyPressed(new KeyEvent(Key.Keypad4, true, GetKeyModifiers(e)));
                    break;
                case Keys.NumPad5:
                    KeyPressed(new KeyEvent(Key.Keypad5, true, GetKeyModifiers(e)));
                    break;
                case Keys.NumPad6:
                    KeyPressed(new KeyEvent(Key.Keypad6, true, GetKeyModifiers(e)));
                    break;
                case Keys.NumPad7:
                    KeyPressed(new KeyEvent(Key.Keypad7, true, GetKeyModifiers(e)));
                    break;
                case Keys.NumPad8:
                    KeyPressed(new KeyEvent(Key.Keypad8, true, GetKeyModifiers(e)));
                    break;
                case Keys.NumPad9:
                    KeyPressed(new KeyEvent(Key.Keypad9, true, GetKeyModifiers(e)));
                    break;
                case Keys.Multiply:
                    KeyPressed(new KeyEvent(Key.KeypadMultiply, true, GetKeyModifiers(e)));
                    break;
                case Keys.Add:
                    KeyPressed(new KeyEvent(Key.KeypadAdd, true, GetKeyModifiers(e)));
                    break;
                //case System.Windows.Forms.Keys.Separator: KeyPressed(new KeyEvent(Veldrid.Key.Separator, true, GetKeyModifiers(e))); break;
                case Keys.Subtract:
                    KeyPressed(new KeyEvent(Key.KeypadSubtract, true, GetKeyModifiers(e)));
                    break;
                case Keys.Decimal:
                    KeyPressed(new KeyEvent(Key.KeypadDecimal, true, GetKeyModifiers(e)));
                    break;
                case Keys.Divide:
                    KeyPressed(new KeyEvent(Key.KeypadDivide, true, GetKeyModifiers(e)));
                    break;
                case Keys.F1:
                    KeyPressed(new KeyEvent(Key.F1, true, GetKeyModifiers(e)));
                    break;
                case Keys.F2:
                    KeyPressed(new KeyEvent(Key.F2, true, GetKeyModifiers(e)));
                    break;
                case Keys.F3:
                    KeyPressed(new KeyEvent(Key.F3, true, GetKeyModifiers(e)));
                    break;
                case Keys.F4:
                    KeyPressed(new KeyEvent(Key.F4, true, GetKeyModifiers(e)));
                    break;
                case Keys.F5:
                    KeyPressed(new KeyEvent(Key.F5, true, GetKeyModifiers(e)));
                    break;
                case Keys.F6:
                    KeyPressed(new KeyEvent(Key.F6, true, GetKeyModifiers(e)));
                    break;
                case Keys.F7:
                    KeyPressed(new KeyEvent(Key.F7, true, GetKeyModifiers(e)));
                    break;
                case Keys.F8:
                    KeyPressed(new KeyEvent(Key.F8, true, GetKeyModifiers(e)));
                    break;
                case Keys.F9:
                    KeyPressed(new KeyEvent(Key.F9, true, GetKeyModifiers(e)));
                    break;
                case Keys.F10:
                    KeyPressed(new KeyEvent(Key.F10, true, GetKeyModifiers(e)));
                    break;
                case Keys.F11:
                    KeyPressed(new KeyEvent(Key.F11, true, GetKeyModifiers(e)));
                    break;
                case Keys.F12:
                    KeyPressed(new KeyEvent(Key.F12, true, GetKeyModifiers(e)));
                    break;
                case Keys.F13:
                    KeyPressed(new KeyEvent(Key.F13, true, GetKeyModifiers(e)));
                    break;
                case Keys.F14:
                    KeyPressed(new KeyEvent(Key.F14, true, GetKeyModifiers(e)));
                    break;
                case Keys.F15:
                    KeyPressed(new KeyEvent(Key.F15, true, GetKeyModifiers(e)));
                    break;
                case Keys.F16:
                    KeyPressed(new KeyEvent(Key.F16, true, GetKeyModifiers(e)));
                    break;
                case Keys.F17:
                    KeyPressed(new KeyEvent(Key.F17, true, GetKeyModifiers(e)));
                    break;
                case Keys.F18:
                    KeyPressed(new KeyEvent(Key.F18, true, GetKeyModifiers(e)));
                    break;
                case Keys.F19:
                    KeyPressed(new KeyEvent(Key.F19, true, GetKeyModifiers(e)));
                    break;
                case Keys.F20:
                    KeyPressed(new KeyEvent(Key.F20, true, GetKeyModifiers(e)));
                    break;
                case Keys.F21:
                    KeyPressed(new KeyEvent(Key.F21, true, GetKeyModifiers(e)));
                    break;
                case Keys.F22:
                    KeyPressed(new KeyEvent(Key.F22, true, GetKeyModifiers(e)));
                    break;
                case Keys.F23:
                    KeyPressed(new KeyEvent(Key.F23, true, GetKeyModifiers(e)));
                    break;
                case Keys.F24:
                    KeyPressed(new KeyEvent(Key.F24, true, GetKeyModifiers(e)));
                    break;
                case Keys.NumLock:
                    KeyPressed(new KeyEvent(Key.NumLock, true, GetKeyModifiers(e)));
                    break;
                case Keys.Scroll:
                    KeyPressed(new KeyEvent(Key.ScrollLock, true, GetKeyModifiers(e)));
                    break;
                case Keys.LShiftKey:
                    KeyPressed(new KeyEvent(Key.ShiftLeft, true, GetKeyModifiers(e)));
                    break;
                case Keys.RShiftKey:
                    KeyPressed(new KeyEvent(Key.ShiftRight, true, GetKeyModifiers(e)));
                    break;
                case Keys.LControlKey:
                    KeyPressed(new KeyEvent(Key.ControlLeft, true, GetKeyModifiers(e)));
                    break;
                case Keys.RControlKey:
                    KeyPressed(new KeyEvent(Key.ControlRight, true, GetKeyModifiers(e)));
                    break;
                //case System.Windows.Forms.Keys.LMenu: KeyPressed(new KeyEvent(Veldrid.Key.LMenu, true, GetKeyModifiers(e))); break;
                //case System.Windows.Forms.Keys.RMenu: KeyPressed(new KeyEvent(Veldrid.Key.RMenu, true, GetKeyModifiers(e))); break;
                //case System.Windows.Forms.Keys.BrowserBack: KeyPressed(new KeyEvent(Veldrid.Key.BrowserBack, true, GetKeyModifiers(e))); break;
                //case System.Windows.Forms.Keys.BrowserForward: KeyPressed(new KeyEvent(Veldrid.Key.BrowserForward, true, GetKeyModifiers(e))); break;
                //case System.Windows.Forms.Keys.BrowserRefresh: KeyPressed(new KeyEvent(Veldrid.Key.BrowserRefresh, true, GetKeyModifiers(e))); break;
                //case System.Windows.Forms.Keys.BrowserStop: KeyPressed(new KeyEvent(Veldrid.Key.BrowserStop, true, GetKeyModifiers(e))); break;
                //case System.Windows.Forms.Keys.BrowserSearch: KeyPressed(new KeyEvent(Veldrid.Key.BrowserSearch, true, GetKeyModifiers(e))); break;
                //case System.Windows.Forms.Keys.BrowserFavorites: KeyPressed(new KeyEvent(Veldrid.Key.BrowserFavorites, true, GetKeyModifiers(e))); break;
                //case System.Windows.Forms.Keys.BrowserHome: KeyPressed(new KeyEvent(Veldrid.Key.BrowserHome, true, GetKeyModifiers(e))); break;
                //case System.Windows.Forms.Keys.VolumeMute: KeyPressed(new KeyEvent(Veldrid.Key.VolumeMute, true, GetKeyModifiers(e))); break;
                //case System.Windows.Forms.Keys.VolumeDown: KeyPressed(new KeyEvent(Veldrid.Key.VolumeDown, true, GetKeyModifiers(e))); break;
                //case System.Windows.Forms.Keys.VolumeUp: KeyPressed(new KeyEvent(Veldrid.Key.VolumeUp, true, GetKeyModifiers(e))); break;
                //case System.Windows.Forms.Keys.MediaNextTrack: KeyPressed(new KeyEvent(Veldrid.Key.MediaNextTrack, true, GetKeyModifiers(e))); break;
                //case System.Windows.Forms.Keys.MediaPreviousTrack: KeyPressed(new KeyEvent(Veldrid.Key.MediaPreviousTrack, true, GetKeyModifiers(e))); break;
                //case System.Windows.Forms.Keys.MediaStop: KeyPressed(new KeyEvent(Veldrid.Key.MediaStop, true, GetKeyModifiers(e))); break;
                //case System.Windows.Forms.Keys.MediaPlayPause: KeyPressed(new KeyEvent(Veldrid.Key.MediaPlayPause, true, GetKeyModifiers(e))); break;
                //case System.Windows.Forms.Keys.LaunchMail: KeyPressed(new KeyEvent(Veldrid.Key.LaunchMail, true, GetKeyModifiers(e))); break;
                //case System.Windows.Forms.Keys.SelectMedia: KeyPressed(new KeyEvent(Veldrid.Key.SelectMedia, true, GetKeyModifiers(e))); break;
                //case System.Windows.Forms.Keys.LaunchApplication1: KeyPressed(new KeyEvent(Veldrid.Key.LaunchApplication1, true, GetKeyModifiers(e))); break;
                //case System.Windows.Forms.Keys.LaunchApplication2: KeyPressed(new KeyEvent(Veldrid.Key.LaunchApplication2, true, GetKeyModifiers(e))); break;
                //case System.Windows.Forms.Keys.Oemplus: KeyPressed(new KeyEvent(Veldrid.Key.Oemplus, true, GetKeyModifiers(e))); break;
                //case System.Windows.Forms.Keys.Oemcomma: KeyPressed(new KeyEvent(Veldrid.Key.Oemcomma, true, GetKeyModifiers(e))); break;
                //case System.Windows.Forms.Keys.OemMinus: KeyPressed(new KeyEvent(Veldrid.Key.OemMinus, true, GetKeyModifiers(e))); break;
                //case System.Windows.Forms.Keys.OemPeriod: KeyPressed(new KeyEvent(Veldrid.Key.OemPeriod, true, GetKeyModifiers(e))); break;
                //case System.Windows.Forms.Keys.ProcessKey: KeyPressed(new KeyEvent(Veldrid.Key.ProcessKey, true, GetKeyModifiers(e))); break;
                //case System.Windows.Forms.Keys.Packet: KeyPressed(new KeyEvent(Veldrid.Key.Packet, true, GetKeyModifiers(e))); break;
                //case System.Windows.Forms.Keys.Attn: KeyPressed(new KeyEvent(Veldrid.Key.Attn, true, GetKeyModifiers(e))); break;
                //case System.Windows.Forms.Keys.Crsel: KeyPressed(new KeyEvent(Veldrid.Key.Crsel, true, GetKeyModifiers(e))); break;
                //case System.Windows.Forms.Keys.Exsel: KeyPressed(new KeyEvent(Veldrid.Key.Exsel, true, GetKeyModifiers(e))); break;
                //case System.Windows.Forms.Keys.EraseEof: KeyPressed(new KeyEvent(Veldrid.Key.EraseEof, true, GetKeyModifiers(e))); break;
                //case System.Windows.Forms.Keys.Play: KeyPressed(new KeyEvent(Veldrid.Key.Play, true, GetKeyModifiers(e))); break;
                //case System.Windows.Forms.Keys.Zoom: KeyPressed(new KeyEvent(Veldrid.Key.Zoom, true, GetKeyModifiers(e))); break;
                //case System.Windows.Forms.Keys.NoName: KeyPressed(new KeyEvent(Veldrid.Key.NoName, true, GetKeyModifiers(e))); break;
                //case System.Windows.Forms.Keys.Pa1: KeyPressed(new KeyEvent(Veldrid.Key.Pa1, true, GetKeyModifiers(e))); break;
                //case System.Windows.Forms.Keys.OemClear: KeyPressed(new KeyEvent(Veldrid.Key.OemClear, true, GetKeyModifiers(e))); break;
                //case System.Windows.Forms.Keys.KeyCode: KeyPressed(new KeyEvent(Veldrid.Key.KeyCode, true, GetKeyModifiers(e))); break;
                //case System.Windows.Forms.Keys.Shift: KeyPressed(new KeyEvent(Veldrid.Key.Shift, true, GetKeyModifiers(e))); break;
                //case System.Windows.Forms.Keys.Control: KeyPressed(new KeyEvent(Veldrid.Key.Control, true, GetKeyModifiers(e))); break;
                //case System.Windows.Forms.Keys.Alt: KeyPressed(new KeyEvent(Veldrid.Key.Alt, true, GetKeyModifiers(e))); break;
                //case System.Windows.Forms.Keys.Modifiers: KeyPressed(new KeyEvent(Veldrid.Key.Modifiers, true, GetKeyModifiers(e))); break;
            }

            base.OnKeyDown(e);
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            if (KeyPressed == null)
                return;

            switch (e.KeyCode)
            {
                //case System.Windows.Forms.Keys.LButton: KeyPressed(new KeyEvent(Veldrid.Key.LButton, false, GetKeyModifiers(e))); break;
                //case System.Windows.Forms.Keys.RButton: KeyPressed(new KeyEvent(Veldrid.Key.RButton, false, GetKeyModifiers(e))); break;
                //case System.Windows.Forms.Keys.Cancel: KeyPressed(new KeyEvent(Veldrid.Key.Cancel, false, GetKeyModifiers(e))); break;
                //case System.Windows.Forms.Keys.MButton: KeyPressed(new KeyEvent(Veldrid.Key.MButton, false, GetKeyModifiers(e))); break;
                //case System.Windows.Forms.Keys.XButton1: KeyPressed(new KeyEvent(Veldrid.Key.XButton1, false, GetKeyModifiers(e))); break;
                //case System.Windows.Forms.Keys.XButton2: KeyPressed(new KeyEvent(Veldrid.Key.XButton2, false, GetKeyModifiers(e))); break;
                case Keys.Back:
                    KeyPressed(new KeyEvent(Key.Back, false, GetKeyModifiers(e)));
                    break;
                case Keys.Tab:
                    KeyPressed(new KeyEvent(Key.Tab, false, GetKeyModifiers(e)));
                    break;
                //case System.Windows.Forms.Keys.LineFeed: KeyPressed(new KeyEvent(Veldrid.Key.LineFeed, false, GetKeyModifiers(e))); break;
                case Keys.Clear:
                    KeyPressed(new KeyEvent(Key.Clear, false, GetKeyModifiers(e)));
                    break;
                case Keys.Enter:
                    KeyPressed(new KeyEvent(Key.Enter, false, GetKeyModifiers(e)));
                    break;
                case Keys.ShiftKey:
                    KeyPressed(new KeyEvent(Key.ShiftLeft, false, GetKeyModifiers(e)));
                    break;
                case Keys.ControlKey:
                    KeyPressed(new KeyEvent(Key.ControlLeft, false, GetKeyModifiers(e)));
                    break;
                case Keys.Menu:
                    KeyPressed(new KeyEvent(Key.Menu, false, GetKeyModifiers(e)));
                    break;
                case Keys.Pause:
                    KeyPressed(new KeyEvent(Key.Pause, false, GetKeyModifiers(e)));
                    break;
                case Keys.CapsLock:
                    KeyPressed(new KeyEvent(Key.CapsLock, false, GetKeyModifiers(e)));
                    break;
                case Keys.Escape:
                    KeyPressed(new KeyEvent(Key.Escape, false, GetKeyModifiers(e)));
                    break;
                case Keys.Space:
                    KeyPressed(new KeyEvent(Key.Space, false, GetKeyModifiers(e)));
                    break;
                case Keys.PageUp:
                    KeyPressed(new KeyEvent(Key.PageUp, false, GetKeyModifiers(e)));
                    break;
                case Keys.PageDown:
                    KeyPressed(new KeyEvent(Key.PageDown, false, GetKeyModifiers(e)));
                    break;
                case Keys.End:
                    KeyPressed(new KeyEvent(Key.End, false, GetKeyModifiers(e)));
                    break;
                case Keys.Home:
                    KeyPressed(new KeyEvent(Key.Home, false, GetKeyModifiers(e)));
                    break;
                case Keys.Left:
                    KeyPressed(new KeyEvent(Key.Left, false, GetKeyModifiers(e)));
                    break;
                case Keys.Up:
                    KeyPressed(new KeyEvent(Key.Up, false, GetKeyModifiers(e)));
                    break;
                case Keys.Right:
                    KeyPressed(new KeyEvent(Key.Right, false, GetKeyModifiers(e)));
                    break;
                case Keys.Down:
                    KeyPressed(new KeyEvent(Key.Down, false, GetKeyModifiers(e)));
                    break;
                //case System.Windows.Forms.Keys.Select: KeyPressed(new KeyEvent(Veldrid.Key.Select, false, GetKeyModifiers(e))); break;
                //case System.Windows.Forms.Keys.Print: KeyPressed(new KeyEvent(Veldrid.Key.Print, false, GetKeyModifiers(e))); break;
                //case System.Windows.Forms.Keys.Execute: KeyPressed(new KeyEvent(Veldrid.Key.Execute, false, GetKeyModifiers(e))); break;
                case Keys.PrintScreen:
                    KeyPressed(new KeyEvent(Key.PrintScreen, false, GetKeyModifiers(e)));
                    break;
                case Keys.Insert:
                    KeyPressed(new KeyEvent(Key.Insert, false, GetKeyModifiers(e)));
                    break;
                case Keys.Delete:
                    KeyPressed(new KeyEvent(Key.Delete, false, GetKeyModifiers(e)));
                    break;
                //case System.Windows.Forms.Keys.Help: KeyPressed(new KeyEvent(Veldrid.Key.Help, false, GetKeyModifiers(e))); break;
                case Keys.D0:
                    KeyPressed(new KeyEvent(Key.Number0, false, GetKeyModifiers(e)));
                    break;
                case Keys.D1:
                    KeyPressed(new KeyEvent(Key.Number1, false, GetKeyModifiers(e)));
                    break;
                case Keys.D2:
                    KeyPressed(new KeyEvent(Key.Number2, false, GetKeyModifiers(e)));
                    break;
                case Keys.D3:
                    KeyPressed(new KeyEvent(Key.Number3, false, GetKeyModifiers(e)));
                    break;
                case Keys.D4:
                    KeyPressed(new KeyEvent(Key.Number4, false, GetKeyModifiers(e)));
                    break;
                case Keys.D5:
                    KeyPressed(new KeyEvent(Key.Number5, false, GetKeyModifiers(e)));
                    break;
                case Keys.D6:
                    KeyPressed(new KeyEvent(Key.Number6, false, GetKeyModifiers(e)));
                    break;
                case Keys.D7:
                    KeyPressed(new KeyEvent(Key.Number7, false, GetKeyModifiers(e)));
                    break;
                case Keys.D8:
                    KeyPressed(new KeyEvent(Key.Number8, false, GetKeyModifiers(e)));
                    break;
                case Keys.D9:
                    KeyPressed(new KeyEvent(Key.Number9, false, GetKeyModifiers(e)));
                    break;
                case Keys.A:
                    KeyPressed(new KeyEvent(Key.A, false, GetKeyModifiers(e)));
                    break;
                case Keys.B:
                    KeyPressed(new KeyEvent(Key.B, false, GetKeyModifiers(e)));
                    break;
                case Keys.C:
                    KeyPressed(new KeyEvent(Key.C, false, GetKeyModifiers(e)));
                    break;
                case Keys.D:
                    KeyPressed(new KeyEvent(Key.D, false, GetKeyModifiers(e)));
                    break;
                case Keys.E:
                    KeyPressed(new KeyEvent(Key.E, false, GetKeyModifiers(e)));
                    break;
                case Keys.F:
                    KeyPressed(new KeyEvent(Key.F, false, GetKeyModifiers(e)));
                    break;
                case Keys.G:
                    KeyPressed(new KeyEvent(Key.G, false, GetKeyModifiers(e)));
                    break;
                case Keys.H:
                    KeyPressed(new KeyEvent(Key.H, false, GetKeyModifiers(e)));
                    break;
                case Keys.I:
                    KeyPressed(new KeyEvent(Key.I, false, GetKeyModifiers(e)));
                    break;
                case Keys.J:
                    KeyPressed(new KeyEvent(Key.J, false, GetKeyModifiers(e)));
                    break;
                case Keys.K:
                    KeyPressed(new KeyEvent(Key.K, false, GetKeyModifiers(e)));
                    break;
                case Keys.L:
                    KeyPressed(new KeyEvent(Key.L, false, GetKeyModifiers(e)));
                    break;
                case Keys.M:
                    KeyPressed(new KeyEvent(Key.M, false, GetKeyModifiers(e)));
                    break;
                case Keys.N:
                    KeyPressed(new KeyEvent(Key.N, false, GetKeyModifiers(e)));
                    break;
                case Keys.O:
                    KeyPressed(new KeyEvent(Key.O, false, GetKeyModifiers(e)));
                    break;
                case Keys.P:
                    KeyPressed(new KeyEvent(Key.P, false, GetKeyModifiers(e)));
                    break;
                case Keys.Q:
                    KeyPressed(new KeyEvent(Key.Q, false, GetKeyModifiers(e)));
                    break;
                case Keys.R:
                    KeyPressed(new KeyEvent(Key.R, false, GetKeyModifiers(e)));
                    break;
                case Keys.S:
                    KeyPressed(new KeyEvent(Key.S, false, GetKeyModifiers(e)));
                    break;
                case Keys.T:
                    KeyPressed(new KeyEvent(Key.T, false, GetKeyModifiers(e)));
                    break;
                case Keys.U:
                    KeyPressed(new KeyEvent(Key.U, false, GetKeyModifiers(e)));
                    break;
                case Keys.V:
                    KeyPressed(new KeyEvent(Key.V, false, GetKeyModifiers(e)));
                    break;
                case Keys.W:
                    KeyPressed(new KeyEvent(Key.W, false, GetKeyModifiers(e)));
                    break;
                case Keys.X:
                    KeyPressed(new KeyEvent(Key.X, false, GetKeyModifiers(e)));
                    break;
                case Keys.Y:
                    KeyPressed(new KeyEvent(Key.Y, false, GetKeyModifiers(e)));
                    break;
                case Keys.Z:
                    KeyPressed(new KeyEvent(Key.Z, false, GetKeyModifiers(e)));
                    break;
                case Keys.LWin:
                    KeyPressed(new KeyEvent(Key.LWin, false, GetKeyModifiers(e)));
                    break;
                case Keys.RWin:
                    KeyPressed(new KeyEvent(Key.RWin, false, GetKeyModifiers(e)));
                    break;
                //case System.Windows.Forms.Keys.Apps: KeyPressed(new KeyEvent(Veldrid.Key.Apps, false, GetKeyModifiers(e))); break;
                case Keys.Sleep:
                    KeyPressed(new KeyEvent(Key.Sleep, false, GetKeyModifiers(e)));
                    break;
                case Keys.NumPad0:
                    KeyPressed(new KeyEvent(Key.Keypad0, false, GetKeyModifiers(e)));
                    break;
                case Keys.NumPad1:
                    KeyPressed(new KeyEvent(Key.Keypad1, false, GetKeyModifiers(e)));
                    break;
                case Keys.NumPad2:
                    KeyPressed(new KeyEvent(Key.Keypad2, false, GetKeyModifiers(e)));
                    break;
                case Keys.NumPad3:
                    KeyPressed(new KeyEvent(Key.Keypad3, false, GetKeyModifiers(e)));
                    break;
                case Keys.NumPad4:
                    KeyPressed(new KeyEvent(Key.Keypad4, false, GetKeyModifiers(e)));
                    break;
                case Keys.NumPad5:
                    KeyPressed(new KeyEvent(Key.Keypad5, false, GetKeyModifiers(e)));
                    break;
                case Keys.NumPad6:
                    KeyPressed(new KeyEvent(Key.Keypad6, false, GetKeyModifiers(e)));
                    break;
                case Keys.NumPad7:
                    KeyPressed(new KeyEvent(Key.Keypad7, false, GetKeyModifiers(e)));
                    break;
                case Keys.NumPad8:
                    KeyPressed(new KeyEvent(Key.Keypad8, false, GetKeyModifiers(e)));
                    break;
                case Keys.NumPad9:
                    KeyPressed(new KeyEvent(Key.Keypad9, false, GetKeyModifiers(e)));
                    break;
                case Keys.Multiply:
                    KeyPressed(new KeyEvent(Key.KeypadMultiply, false, GetKeyModifiers(e)));
                    break;
                case Keys.Add:
                    KeyPressed(new KeyEvent(Key.KeypadAdd, false, GetKeyModifiers(e)));
                    break;
                //case System.Windows.Forms.Keys.Separator: KeyPressed(new KeyEvent(Veldrid.Key.Separator, false, GetKeyModifiers(e))); break;
                case Keys.Subtract:
                    KeyPressed(new KeyEvent(Key.KeypadSubtract, false, GetKeyModifiers(e)));
                    break;
                case Keys.Decimal:
                    KeyPressed(new KeyEvent(Key.KeypadDecimal, false, GetKeyModifiers(e)));
                    break;
                case Keys.Divide:
                    KeyPressed(new KeyEvent(Key.KeypadDivide, false, GetKeyModifiers(e)));
                    break;
                case Keys.F1:
                    KeyPressed(new KeyEvent(Key.F1, false, GetKeyModifiers(e)));
                    break;
                case Keys.F2:
                    KeyPressed(new KeyEvent(Key.F2, false, GetKeyModifiers(e)));
                    break;
                case Keys.F3:
                    KeyPressed(new KeyEvent(Key.F3, false, GetKeyModifiers(e)));
                    break;
                case Keys.F4:
                    KeyPressed(new KeyEvent(Key.F4, false, GetKeyModifiers(e)));
                    break;
                case Keys.F5:
                    KeyPressed(new KeyEvent(Key.F5, false, GetKeyModifiers(e)));
                    break;
                case Keys.F6:
                    KeyPressed(new KeyEvent(Key.F6, false, GetKeyModifiers(e)));
                    break;
                case Keys.F7:
                    KeyPressed(new KeyEvent(Key.F7, false, GetKeyModifiers(e)));
                    break;
                case Keys.F8:
                    KeyPressed(new KeyEvent(Key.F8, false, GetKeyModifiers(e)));
                    break;
                case Keys.F9:
                    KeyPressed(new KeyEvent(Key.F9, false, GetKeyModifiers(e)));
                    break;
                case Keys.F10:
                    KeyPressed(new KeyEvent(Key.F10, false, GetKeyModifiers(e)));
                    break;
                case Keys.F11:
                    KeyPressed(new KeyEvent(Key.F11, false, GetKeyModifiers(e)));
                    break;
                case Keys.F12:
                    KeyPressed(new KeyEvent(Key.F12, false, GetKeyModifiers(e)));
                    break;
                case Keys.F13:
                    KeyPressed(new KeyEvent(Key.F13, false, GetKeyModifiers(e)));
                    break;
                case Keys.F14:
                    KeyPressed(new KeyEvent(Key.F14, false, GetKeyModifiers(e)));
                    break;
                case Keys.F15:
                    KeyPressed(new KeyEvent(Key.F15, false, GetKeyModifiers(e)));
                    break;
                case Keys.F16:
                    KeyPressed(new KeyEvent(Key.F16, false, GetKeyModifiers(e)));
                    break;
                case Keys.F17:
                    KeyPressed(new KeyEvent(Key.F17, false, GetKeyModifiers(e)));
                    break;
                case Keys.F18:
                    KeyPressed(new KeyEvent(Key.F18, false, GetKeyModifiers(e)));
                    break;
                case Keys.F19:
                    KeyPressed(new KeyEvent(Key.F19, false, GetKeyModifiers(e)));
                    break;
                case Keys.F20:
                    KeyPressed(new KeyEvent(Key.F20, false, GetKeyModifiers(e)));
                    break;
                case Keys.F21:
                    KeyPressed(new KeyEvent(Key.F21, false, GetKeyModifiers(e)));
                    break;
                case Keys.F22:
                    KeyPressed(new KeyEvent(Key.F22, false, GetKeyModifiers(e)));
                    break;
                case Keys.F23:
                    KeyPressed(new KeyEvent(Key.F23, false, GetKeyModifiers(e)));
                    break;
                case Keys.F24:
                    KeyPressed(new KeyEvent(Key.F24, false, GetKeyModifiers(e)));
                    break;
                case Keys.NumLock:
                    KeyPressed(new KeyEvent(Key.NumLock, false, GetKeyModifiers(e)));
                    break;
                case Keys.Scroll:
                    KeyPressed(new KeyEvent(Key.ScrollLock, false, GetKeyModifiers(e)));
                    break;
                case Keys.LShiftKey:
                    KeyPressed(new KeyEvent(Key.ShiftLeft, false, GetKeyModifiers(e)));
                    break;
                case Keys.RShiftKey:
                    KeyPressed(new KeyEvent(Key.ShiftRight, false, GetKeyModifiers(e)));
                    break;
                case Keys.LControlKey:
                    KeyPressed(new KeyEvent(Key.ControlLeft, false, GetKeyModifiers(e)));
                    break;
                case Keys.RControlKey:
                    KeyPressed(new KeyEvent(Key.ControlRight, false, GetKeyModifiers(e)));
                    break;
                //case System.Windows.Forms.Keys.LMenu: KeyPressed(new KeyEvent(Veldrid.Key.LMenu, false, GetKeyModifiers(e))); break;
                //case System.Windows.Forms.Keys.RMenu: KeyPressed(new KeyEvent(Veldrid.Key.RMenu, false, GetKeyModifiers(e))); break;
                //case System.Windows.Forms.Keys.BrowserBack: KeyPressed(new KeyEvent(Veldrid.Key.BrowserBack, false, GetKeyModifiers(e))); break;
                //case System.Windows.Forms.Keys.BrowserForward: KeyPressed(new KeyEvent(Veldrid.Key.BrowserForward, false, GetKeyModifiers(e))); break;
                //case System.Windows.Forms.Keys.BrowserRefresh: KeyPressed(new KeyEvent(Veldrid.Key.BrowserRefresh, false, GetKeyModifiers(e))); break;
                //case System.Windows.Forms.Keys.BrowserStop: KeyPressed(new KeyEvent(Veldrid.Key.BrowserStop, false, GetKeyModifiers(e))); break;
                //case System.Windows.Forms.Keys.BrowserSearch: KeyPressed(new KeyEvent(Veldrid.Key.BrowserSearch, false, GetKeyModifiers(e))); break;
                //case System.Windows.Forms.Keys.BrowserFavorites: KeyPressed(new KeyEvent(Veldrid.Key.BrowserFavorites, false, GetKeyModifiers(e))); break;
                //case System.Windows.Forms.Keys.BrowserHome: KeyPressed(new KeyEvent(Veldrid.Key.BrowserHome, false, GetKeyModifiers(e))); break;
                //case System.Windows.Forms.Keys.VolumeMute: KeyPressed(new KeyEvent(Veldrid.Key.VolumeMute, false, GetKeyModifiers(e))); break;
                //case System.Windows.Forms.Keys.VolumeDown: KeyPressed(new KeyEvent(Veldrid.Key.VolumeDown, false, GetKeyModifiers(e))); break;
                //case System.Windows.Forms.Keys.VolumeUp: KeyPressed(new KeyEvent(Veldrid.Key.VolumeUp, false, GetKeyModifiers(e))); break;
                //case System.Windows.Forms.Keys.MediaNextTrack: KeyPressed(new KeyEvent(Veldrid.Key.MediaNextTrack, false, GetKeyModifiers(e))); break;
                //case System.Windows.Forms.Keys.MediaPreviousTrack: KeyPressed(new KeyEvent(Veldrid.Key.MediaPreviousTrack, false, GetKeyModifiers(e))); break;
                //case System.Windows.Forms.Keys.MediaStop: KeyPressed(new KeyEvent(Veldrid.Key.MediaStop, false, GetKeyModifiers(e))); break;
                //case System.Windows.Forms.Keys.MediaPlayPause: KeyPressed(new KeyEvent(Veldrid.Key.MediaPlayPause, false, GetKeyModifiers(e))); break;
                //case System.Windows.Forms.Keys.LaunchMail: KeyPressed(new KeyEvent(Veldrid.Key.LaunchMail, false, GetKeyModifiers(e))); break;
                //case System.Windows.Forms.Keys.SelectMedia: KeyPressed(new KeyEvent(Veldrid.Key.SelectMedia, false, GetKeyModifiers(e))); break;
                //case System.Windows.Forms.Keys.LaunchApplication1: KeyPressed(new KeyEvent(Veldrid.Key.LaunchApplication1, false, GetKeyModifiers(e))); break;
                //case System.Windows.Forms.Keys.LaunchApplication2: KeyPressed(new KeyEvent(Veldrid.Key.LaunchApplication2, false, GetKeyModifiers(e))); break;
                //case System.Windows.Forms.Keys.Oemplus: KeyPressed(new KeyEvent(Veldrid.Key.Oemplus, false, GetKeyModifiers(e))); break;
                //case System.Windows.Forms.Keys.Oemcomma: KeyPressed(new KeyEvent(Veldrid.Key.Oemcomma, false, GetKeyModifiers(e))); break;
                //case System.Windows.Forms.Keys.OemMinus: KeyPressed(new KeyEvent(Veldrid.Key.OemMinus, false, GetKeyModifiers(e))); break;
                //case System.Windows.Forms.Keys.OemPeriod: KeyPressed(new KeyEvent(Veldrid.Key.OemPeriod, false, GetKeyModifiers(e))); break;
                //case System.Windows.Forms.Keys.ProcessKey: KeyPressed(new KeyEvent(Veldrid.Key.ProcessKey, false, GetKeyModifiers(e))); break;
                //case System.Windows.Forms.Keys.Packet: KeyPressed(new KeyEvent(Veldrid.Key.Packet, false, GetKeyModifiers(e))); break;
                //case System.Windows.Forms.Keys.Attn: KeyPressed(new KeyEvent(Veldrid.Key.Attn, false, GetKeyModifiers(e))); break;
                //case System.Windows.Forms.Keys.Crsel: KeyPressed(new KeyEvent(Veldrid.Key.Crsel, false, GetKeyModifiers(e))); break;
                //case System.Windows.Forms.Keys.Exsel: KeyPressed(new KeyEvent(Veldrid.Key.Exsel, false, GetKeyModifiers(e))); break;
                //case System.Windows.Forms.Keys.EraseEof: KeyPressed(new KeyEvent(Veldrid.Key.EraseEof, false, GetKeyModifiers(e))); break;
                //case System.Windows.Forms.Keys.Play: KeyPressed(new KeyEvent(Veldrid.Key.Play, false, GetKeyModifiers(e))); break;
                //case System.Windows.Forms.Keys.Zoom: KeyPressed(new KeyEvent(Veldrid.Key.Zoom, false, GetKeyModifiers(e))); break;
                //case System.Windows.Forms.Keys.NoName: KeyPressed(new KeyEvent(Veldrid.Key.NoName, false, GetKeyModifiers(e))); break;
                //case System.Windows.Forms.Keys.Pa1: KeyPressed(new KeyEvent(Veldrid.Key.Pa1, false, GetKeyModifiers(e))); break;
                //case System.Windows.Forms.Keys.OemClear: KeyPressed(new KeyEvent(Veldrid.Key.OemClear, false, GetKeyModifiers(e))); break;
                //case System.Windows.Forms.Keys.KeyCode: KeyPressed(new KeyEvent(Veldrid.Key.KeyCode, false, GetKeyModifiers(e))); break;
                //case System.Windows.Forms.Keys.Shift: KeyPressed(new KeyEvent(Veldrid.Key.Shift, false, GetKeyModifiers(e))); break;
                //case System.Windows.Forms.Keys.Control: KeyPressed(new KeyEvent(Veldrid.Key.Control, false, GetKeyModifiers(e))); break;
                //case System.Windows.Forms.Keys.Alt: KeyPressed(new KeyEvent(Veldrid.Key.Alt, false, GetKeyModifiers(e))); break;
                //case System.Windows.Forms.Keys.Modifiers: KeyPressed(new KeyEvent(Veldrid.Key.Modifiers, false, GetKeyModifiers(e))); break;
            }

            base.OnKeyUp(e);
        }

        private void OnIdle(object sender, EventArgs e)
        {
            Invalidate();
        }

        private ModifierKeys GetKeyModifiers(KeyEventArgs keyEventArgs)
        {
            var modifiers = Veldrid.ModifierKeys.None;
            if (keyEventArgs.Control)
                modifiers |= Veldrid.ModifierKeys.Control;
            if (keyEventArgs.Shift)
                modifiers |= Veldrid.ModifierKeys.Shift;
            if (keyEventArgs.Alt)
                modifiers |= Veldrid.ModifierKeys.Alt;
            return modifiers;
        }
    }
}