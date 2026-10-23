namespace Client.UI.Widgets;

using System;
using Raylib_cs;
using global::Client.UI.Rendering;

public class ProgressBar : Widget
{
    public float Value { get; set; }
    public float MaxValue { get; set; }
    public VisualKey BackgroundKey { get; set; }
    public VisualKey FillKey { get; set; }
    public ClipAxis Axis { get; set; }

    public float FillRatio => (MaxValue > 0.0f && !float.IsNaN(Value) && !float.IsInfinity(Value))
        ? Math.Clamp(Value / MaxValue, 0.0f, 1.0f)
        : 0.0f;

    public ProgressBar(
        Rectangle bounds,
        IRenderer renderer,
        float value,
        float maxValue,
        VisualKey backgroundKey,
        VisualKey fillKey,
        ClipAxis axis = ClipAxis.Horizontal)
        : base(bounds, renderer)
    {
        Value = value;
        MaxValue = maxValue;
        BackgroundKey = backgroundKey;
        FillKey = fillKey;
        Axis = axis;
    }

    public ProgressBar(Rectangle bounds, IRenderer renderer)
        : this(
            bounds,
            renderer,
            0.0f,
            100.0f,
            new VisualKey(VisualRole.Surface),
            new VisualKey(VisualRole.FillDanger),
            ClipAxis.Horizontal)
    {
    }

    protected void RenderBar(Rectangle barBounds)
    {
        if (barBounds.Width <= 0 || barBounds.Height <= 0)
        {
            return;
        }

        Renderer.DrawSurface(barBounds, BackgroundKey);
        float ratio = FillRatio;
        if (ratio > 0.0f)
        {
            Renderer.DrawClippedSurface(barBounds, FillKey, ratio, Axis);
        }
    }

    protected override void Render()
    {
        RenderBar(Bounds);
    }
}
