namespace Client.UI.Widgets;

using System;
using Raylib_cs;
using global::Client.UI.Rendering;

public abstract class Widget
{
    public Rectangle Bounds { get; set; }
    public IRenderer Renderer { get; private set; }

    protected Widget(Rectangle bounds, IRenderer renderer)
    {
        ArgumentNullException.ThrowIfNull(renderer);
        Bounds = bounds;
        Renderer = renderer;
    }

    public void SetRenderer(IRenderer newRenderer)
    {
        ArgumentNullException.ThrowIfNull(newRenderer);
        Renderer = newRenderer;
    }

    public void Draw()
    {
        Render();
    }

    protected abstract void Render();
}
