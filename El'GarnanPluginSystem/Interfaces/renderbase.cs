using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using El_Garnan_Plugin_Loader.Models;

namespace El_Garnan_Plugin_Loader.Interfaces
{
    public class ImGuiRenderer : IDisposable
{
    private ImGuiController _controller;
    private GraphicsDevice _gd;
    private CommandList _cl;
    private readonly List<IGamePlugin> _plugins;
    
    public ImGuiRenderer(IEnumerable<IGamePlugin> plugins)
    {
        _plugins = plugins.ToList();
        InitializeGraphics();
    }

    private void InitializeGraphics()
    {
        var window = VeldridStartup.CreateWindow(new WindowCreateInfo(
            x: 100, y: 100, width: 960, height: 540,
            WindowState.Normal, "Plugin Notifications"));
            
        _gd = VeldridStartup.CreateGraphicsDevice(window);
        _cl = _gd.ResourceFactory.CreateCommandList();
        _controller = new ImGuiController(_gd, _gd.MainSwapchain.Framebuffer.OutputDescription, 
            window.Width, window.Height);
    }

    public void Render()
    {
        _controller.Update(1f/60f, InputSnapshot.Empty);
        
        foreach (var plugin in _plugins.Where(p => p.SupportsImGui))
        {
            plugin.RenderImGui();
        }

        _cl.Begin();
        _cl.SetFramebuffer(_gd.MainSwapchain.Framebuffer);
        _cl.ClearColorTarget(0, RgbaFloat.Black);
        _controller.Render(_gd, _cl);
        _cl.End();
        _gd.SubmitCommands(_cl);
        _gd.SwapBuffers(_gd.MainSwapchain);
    }

    public void Dispose()
    {
        _controller?.Dispose();
        _cl?.Dispose();
        _gd?.Dispose();
    }
}

}