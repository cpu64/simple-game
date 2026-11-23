namespace Client.UI.Widgets;

using System;
using global::Client.UI.Rendering;
using Raylib_cs;

public abstract class Widget // Component
{
    public Rectangle Bounds { get; set; }
    public IRenderer Renderer { get; private set; }
    public bool IsVisible { get; set; } = true;

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
        if (!IsVisible)
        {
            return;
        }

        Render();
    }

    protected abstract void Render(); // Operation
}
