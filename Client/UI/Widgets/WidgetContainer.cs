namespace Client.UI.Widgets;

using System;
using System.Collections.Generic;
using global::Client.UI.Rendering;
using Raylib_cs;

public class WidgetContainer : Widget // Composite
{
    private readonly List<Widget> _children = new(); // Children

    public IReadOnlyList<Widget> Children => _children;

    public WidgetContainer(Rectangle bounds, IRenderer renderer)
        : base(bounds, renderer) { }

    public virtual void Add(Widget child) // Add
    {
        ArgumentNullException.ThrowIfNull(child);
        if (ReferenceEquals(child, this))
        {
            throw new ArgumentException("A WidgetContainer cannot contain itself.", nameof(child));
        }

        if (_children.Contains(child))
        {
            return;
        }

        if (!ReferenceEquals(child.Renderer, Renderer))
        {
            child.SetRenderer(Renderer);
        }

        _children.Add(child);
    }

    public virtual bool Remove(Widget child) // Remove
    {
        if (child is null)
        {
            return false;
        }

        return _children.Remove(child);
    }

    public override void SetRenderer(IRenderer newRenderer) // Operation
    {
        base.SetRenderer(newRenderer);
        foreach (Widget child in _children)
        {
            child.SetRenderer(newRenderer);
        }
    }

    protected override void Render() // Operation
    {
        foreach (Widget child in _children)
        {
            child.Draw();
        }
    }
}
