namespace Client.UI.Widgets;

using System;
using global::Client.UI.Rendering;
using Raylib_cs;

public abstract class Widget // Component
{
    public Rectangle Bounds { get; set; }
    public IRenderer Renderer { get; private set; }

    protected Widget(Rectangle bounds, IRenderer renderer)
    {
        ArgumentNullException.ThrowIfNull(renderer);
        Bounds = bounds;
        Renderer = renderer;
    }

    public virtual void SetRenderer(IRenderer newRenderer) // Operation
    {
        ArgumentNullException.ThrowIfNull(newRenderer);
        Renderer = newRenderer;
    }

    public void Draw() // Operation
    {
        Render();
    }

    protected abstract void Render(); // Operation
}
